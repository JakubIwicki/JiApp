import axios from 'axios';

const apiClient = axios.create({
  baseURL: 'http://10.0.2.2:5001/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

apiClient.interceptors.request.use(
  async (config) => {
    // Token will be attached here in Phase 1 from encrypted storage
    return config;
  },
  (error) => Promise.reject(error),
);

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      // Clear auth state and redirect to login in Phase 1
    }
    return Promise.reject(error);
  },
);

export default apiClient;
