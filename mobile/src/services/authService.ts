import axios from 'axios';
import apiClient from './apiClient';
import {
  ChangePasswordRequest,
  LoginApiRaw,
  LoginRequest,
  LoginResponse,
  MeApiRaw,
  RefreshResponse,
  RegisterRequest,
  UpdateProfileApiRaw,
  UpdateProfileRequest,
} from '../types/api';
import {
  LoginApiRawSchema,
  MeApiRawSchema,
  RefreshResponseSchema,
  UpdateProfileApiRawSchema,
} from '../types/schemas';
import { API_BASE_URL } from '../config';

export interface ProfileResponse {
  id: number;
  displayName: string;
  email?: string;
  roles: string[];
  permissions: string[];
}

export const login = async (
  username: string,
  password: string,
): Promise<LoginResponse> => {
  const body: LoginRequest = { username, password };
  const response = await apiClient.post<LoginApiRaw>('/auth/login', body);
  const data = LoginApiRawSchema.parse(response.data);
  // Map API field names (accessToken, userId) to app model (token, id)
  return {
    token: data.accessToken,
    id: data.userId,
    displayName: data.displayName ?? '',
    refreshToken: data.refreshToken,
    roles: data.roles ?? [],
    permissions: data.permissions ?? [],
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
  const data = MeApiRawSchema.parse(response.data);
  return {
    token,
    id: data.id,
    displayName: data.displayName ?? '',
    roles: data.roles ?? [],
    permissions: data.permissions ?? [],
  };
};

export const getProfile = async (): Promise<ProfileResponse> => {
  const response = await apiClient.get<MeApiRaw>('/auth/me');
  const data = MeApiRawSchema.parse(response.data);
  return {
    id: data.id,
    displayName: data.displayName ?? '',
    email: data.email,
    roles: data.roles ?? [],
    permissions: data.permissions ?? [],
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
  const data = UpdateProfileApiRawSchema.parse(response.data);
  return {
    id: data.id,
    displayName: data.displayName ?? '',
    email: data.email,
    roles: [],
    permissions: [],
  };
};

export const changePassword = async (
  currentPassword: string,
  newPassword: string,
): Promise<void> => {
  const body: ChangePasswordRequest = { currentPassword, newPassword };
  await apiClient.post('/auth/change-password', body);
};

/**
 * Exchange a refresh token for a new access + refresh token pair.
 * Uses raw axios to bypass the apiClient 401 interceptor.
 */
export const refreshToken = async (token: string): Promise<RefreshResponse> => {
  const response = await axios.post<unknown>(
    `${API_BASE_URL}/auth/refresh`,
    { refreshToken: token },
    { headers: { 'Content-Type': 'application/json' } },
  );
  return RefreshResponseSchema.parse(response.data);
};
