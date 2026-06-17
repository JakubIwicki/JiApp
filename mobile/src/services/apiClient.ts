import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import {
  getToken,
  clearToken,
  clearUserId,
  clearDisplayName,
  clearUsername,
  clearCredentials,
  saveToken,
  saveUserId,
  saveDisplayName,
  saveUsername,
  getCredentials,
} from './storageService';
import { API_BASE_URL } from '../config';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

apiClient.interceptors.request.use(
  async config => {
    const token = await getToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  error => Promise.reject(error),
);

interface RetryConfig extends InternalAxiosRequestConfig {
  _isRetry?: boolean;
}

apiClient.interceptors.response.use(
  response => response,
  async (error: AxiosError) => {
    const config = error.config as RetryConfig | undefined;

    // Don't intercept 401 for login/register — let the callers handle errors
    if (
      error.response?.status === 401 &&
      config &&
      !config._isRetry &&
      !config.url?.includes('/auth/login') &&
      !config.url?.includes('/auth/register')
    ) {
      const credentials = await getCredentials();

      if (credentials) {
        try {
          // Re-login with saved credentials using raw axios (bypasses this interceptor)
          const loginResponse = await axios.post(
            `${API_BASE_URL}/auth/login`,
            { username: credentials.username, password: credentials.password },
            { headers: { 'Content-Type': 'application/json' } },
          );

          const { accessToken, userId, displayName } = loginResponse.data;
          await Promise.all([
            saveToken(accessToken),
            saveUserId(userId),
            saveDisplayName(displayName),
            saveUsername(credentials.username),
          ]);

          // Retry the original request with the new token
          config._isRetry = true;
          config.headers.Authorization = `Bearer ${accessToken}`;
          return apiClient.request(config);
        } catch {
          // Re-login failed — wipe everything
        }
      }

      // No saved credentials or re-login failed — clear all auth state
      await Promise.all([
        clearToken(),
        clearUserId(),
        clearDisplayName(),
        clearUsername(),
        clearCredentials(),
      ]);
    }

    // Extract server error message so downstream error utils can read
    // it without importing axios types
    if (error.response?.data && typeof error.response.data === 'object') {
      const data = error.response.data as Record<string, unknown>;
      if (typeof data.error === 'string') {
        (error as any)._serverError = data.error;
      }
    }

    return Promise.reject(error);
  },
);

export default apiClient;
