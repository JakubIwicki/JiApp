import { z } from 'zod';
import apiClient from '../../../services/apiClient';
import {
  UserDetailSchema,
  ListUsersRawSchema,
  ListRolesResponseSchema,
} from '../types/api';
import type {
  PaginatedUsers,
  UserDetail,
  RoleSummary,
  CreateUserRequest,
  AssignRoleRequest,
  ResetPasswordRequest,
  CreateRoleRequest,
  UpdateRolePermissionsRequest,
} from '../types/api';

const BASE = '/auth/admin';

// ── Users ────────────────────────────────────────────────────────────────────

export const listUsers = async (
  search?: string,
  page?: number,
  pageSize?: number,
): Promise<PaginatedUsers> => {
  const params: Record<string, string | number> = {};
  if (search) params.search = search;
  if (page !== undefined) params.page = page;
  if (pageSize !== undefined) params.pageSize = pageSize;
  const response = await apiClient.get(`${BASE}/users`, { params });
  const raw = ListUsersRawSchema.parse(response.data);
  return { items: raw.users, total: raw.totalCount };
};

export const getUser = async (userId: number): Promise<UserDetail> => {
  const response = await apiClient.get(`${BASE}/users/${userId}`);
  return UserDetailSchema.parse(response.data);
};

export const createUser = async (data: CreateUserRequest): Promise<number> => {
  const response = await apiClient.post(`${BASE}/users`, data);
  return z.object({ userId: z.number() }).parse(response.data).userId;
};

export const disableUser = async (userId: number): Promise<void> => {
  await apiClient.post(`${BASE}/users/${userId}/disable`);
};

export const enableUser = async (userId: number): Promise<void> => {
  await apiClient.post(`${BASE}/users/${userId}/enable`);
};

export const deleteUser = async (userId: number): Promise<void> => {
  await apiClient.delete(`${BASE}/users/${userId}`);
};

export const assignRole = async (
  userId: number,
  data: AssignRoleRequest,
): Promise<void> => {
  await apiClient.post(`${BASE}/users/${userId}/roles`, data);
};

export const removeRole = async (
  userId: number,
  roleName: string,
): Promise<void> => {
  await apiClient.delete(`${BASE}/users/${userId}/roles/${roleName}`);
};

export const resetPassword = async (
  userId: number,
  data: ResetPasswordRequest,
): Promise<void> => {
  await apiClient.post(`${BASE}/users/${userId}/reset-password`, data);
};

// ── Roles ────────────────────────────────────────────────────────────────────

export const listRoles = async (): Promise<RoleSummary[]> => {
  const response = await apiClient.get(`${BASE}/roles`);
  return ListRolesResponseSchema.parse(response.data).roles;
};

export const createRole = async (data: CreateRoleRequest): Promise<void> => {
  await apiClient.post(`${BASE}/roles`, data);
};

export const updateRolePermissions = async (
  roleName: string,
  data: UpdateRolePermissionsRequest,
): Promise<void> => {
  await apiClient.put(`${BASE}/roles/${roleName}/permissions`, data);
};

export const deleteRole = async (roleName: string): Promise<void> => {
  await apiClient.delete(`${BASE}/roles/${roleName}`);
};
