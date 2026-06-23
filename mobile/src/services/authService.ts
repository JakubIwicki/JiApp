import apiClient from './apiClient';
import {
  ChangePasswordRequest,
  LoginApiRaw,
  LoginRequest,
  LoginResponse,
  MeApiRaw,
  RegisterRequest,
  UpdateProfileApiRaw,
  UpdateProfileRequest,
} from '../types/api';

export interface ProfileResponse {
  id: number;
  displayName: string;
  email?: string;
  modules: string[];
}

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
    token,
    id: response.data.id,
    displayName: response.data.displayName ?? '',
    modules: response.data.modules ?? [],
  };
};

export const getProfile = async (): Promise<ProfileResponse> => {
  const response = await apiClient.get<MeApiRaw>('/auth/me');
  return {
    id: response.data.id,
    displayName: response.data.displayName ?? '',
    email: response.data.email,
    modules: response.data.modules ?? [],
  };
};

export const updateProfile = async (
  displayName: string,
  email: string,
): Promise<ProfileResponse> => {
  const body: UpdateProfileRequest = { displayName, email };
  const response = await apiClient.patch<UpdateProfileApiRaw>(
    '/auth/profile',
    body,
  );
  return {
    id: response.data.id,
    displayName: response.data.displayName ?? '',
    email: response.data.email,
    modules: [],
  };
};

export const changePassword = async (
  currentPassword: string,
  newPassword: string,
): Promise<void> => {
  const body: ChangePasswordRequest = { currentPassword, newPassword };
  await apiClient.post('/auth/change-password', body);
};
