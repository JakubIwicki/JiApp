import apiClient from './apiClient';
import {
  LoginApiRaw,
  LoginRequest,
  LoginResponse,
  MeApiRaw,
  RegisterRequest,
} from '../types/api';

export const login = async (
  username: string,
  password: string,
): Promise<LoginResponse> => {
  const body: LoginRequest = { username, password };
  const response = await apiClient.post<LoginApiRaw>('/auth/login', body);
  // Map API field names (accessToken, userId) to app model (token, id)
  return {
    token: response.data.accessToken,
    id: response.data.userId,
    displayName: response.data.displayName,
    modules: response.data.modules ?? [],
  };
};

export const register = async (
  username: string,
  email: string,
  password: string,
  displayName: string,
): Promise<void> => {
  const body: RegisterRequest = {
    username,
    email,
    password,
    displayName,
  };
  await apiClient.post('/auth/register', body);
};

export const checkToken = async (token: string): Promise<LoginResponse> => {
  const response = await apiClient.get<MeApiRaw>('/auth/me', {
    headers: { Authorization: `Bearer ${token}` },
  });
  return {
    token: response.data.token,
    id: response.data.id,
    displayName: response.data.displayName,
    modules: response.data.modules ?? [],
  };
};
