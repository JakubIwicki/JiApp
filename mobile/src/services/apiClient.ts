import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import {
  getToken,
  clearToken,
  clearRefreshToken,
  clearUserId,
  clearDisplayName,
  clearUsername,
  clearCredentials,
  saveToken,
  getRefreshToken,
  saveRefreshToken,
} from './storageService';
import { RefreshResponseSchema } from '../types/schemas';
import { API_BASE_URL } from '../config';
import type { ServerAugmentedError } from '../types/api';

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

// ── Single-flight refresh guard ───────────────────────────────────────────
// Concurrent 401s must trigger only one /auth/refresh call; other requests
// await the same promise, then retry with the new access token.

let refreshPromise: Promise<string | null> | null = null;

async function refreshAuth(): Promise<string | null> {
  if (refreshPromise) return refreshPromise;

  refreshPromise = (async () => {
    try {
      const storedRefreshToken = await getRefreshToken();
      if (!storedRefreshToken) {
        await Promise.all([
          clearToken(),
          clearRefreshToken(),
          clearUserId(),
          clearDisplayName(),
          clearUsername(),
          clearCredentials(),
        ]);
        return null;
      }

      const response = await axios.post<unknown>(
        `${API_BASE_URL}/auth/refresh`,
        { refreshToken: storedRefreshToken },
        { headers: { 'Content-Type': 'application/json' } },
      );

      const data = RefreshResponseSchema.parse(response.data);
      await Promise.all([
        saveToken(data.accessToken),
        saveRefreshToken(data.refreshToken),
      ]);

      return data.accessToken;
    } catch {
      await Promise.all([
        clearToken(),
        clearRefreshToken(),
        clearUserId(),
        clearDisplayName(),
        clearUsername(),
        clearCredentials(),
      ]);
      return null;
    } finally {
      refreshPromise = null;
    }
  })();

  return refreshPromise;
}

apiClient.interceptors.response.use(
  response => response,
  async (error: AxiosError) => {
    const config = error.config as RetryConfig | undefined;

    if (
      error.response?.status === 401 &&
      config &&
      !config._isRetry &&
      !config.url?.includes('/auth/login') &&
      !config.url?.includes('/auth/register') &&
      !config.url?.includes('/auth/refresh')
    ) {
      const newToken = await refreshAuth();

      if (newToken) {
        config._isRetry = true;
        config.headers.Authorization = `Bearer ${newToken}`;
        return apiClient.request(config);
      }
    }

    // Extract server error message so downstream error utils can read
    // it without importing axios types
    if (error.response?.data && typeof error.response.data === 'object') {
      const data = error.response.data as Record<string, unknown>;
      if (typeof data.error === 'string') {
        (error as ServerAugmentedError)._serverError = data.error;
      }
    }

    return Promise.reject(error);
  },
);

export default apiClient;
