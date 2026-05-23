import axios from 'axios';
import { getToken, clearToken, clearUserId, clearDisplayName, clearUsername, clearCredentials } from './storageService';
import { API_BASE_URL } from '../config';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

apiClient.interceptors.request.use(
  async (config) => {
    const token = await getToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error),
);

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      await Promise.all([
        clearToken(),
        clearUserId(),
        clearDisplayName(),
        clearUsername(),
        clearCredentials(),
      ]);
    }
    return Promise.reject(error);
  },
);

export default apiClient;
