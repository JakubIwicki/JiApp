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
import {
  LoginApiRawSchema,
  MeApiRawSchema,
  UpdateProfileApiRawSchema,
} from '../types/schemas';

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
  const data = LoginApiRawSchema.parse(response.data);
  // Map API field names (accessToken, userId) to app model (token, id)
  return {
    token: data.accessToken,
    id: data.userId,
    displayName: data.displayName ?? '',
    modules: data.modules ?? [],
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
    modules: data.modules ?? [],
  };
};

export const getProfile = async (): Promise<ProfileResponse> => {
  const response = await apiClient.get<MeApiRaw>('/auth/me');
  const data = MeApiRawSchema.parse(response.data);
  return {
    id: data.id,
    displayName: data.displayName ?? '',
    email: data.email,
    modules: data.modules ?? [],
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
