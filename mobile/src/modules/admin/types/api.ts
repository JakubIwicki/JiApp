import { z } from 'zod';

// ── Fixed permission catalog ─────────────────────────────────────────────────

export const ALL_PERMISSIONS = [
  'scheduler.access',
  'ytdownloader.access',
  'lovingboards.access',
  'users.manage',
  'roles.manage',
] as const;

export type Permission = (typeof ALL_PERMISSIONS)[number];

// ── Schemas ──────────────────────────────────────────────────────────────────

export const UserSummarySchema = z.object({
  id: z.number(),
  username: z.string().nullable(),
  email: z.string().nullable(),
  displayName: z.string().nullable(),
  roles: z.array(z.string()),
  isLockedOut: z.boolean(),
});

export const UserDetailSchema = UserSummarySchema.extend({
  lockoutEnd: z.string().nullable(),
});

export const RoleSummarySchema = z.object({
  name: z.string(),
  permissions: z.array(z.string()),
});

export const ListUsersRawSchema = z.object({
  users: z.array(UserSummarySchema),
  totalCount: z.number(),
});

export const ListRolesResponseSchema = z.object({
  roles: z.array(RoleSummarySchema),
});

export const PaginatedUsersSchema = z.object({
  items: z.array(UserSummarySchema),
  total: z.number(),
});

// ── Request payload types ────────────────────────────────────────────────────

export interface CreateUserRequest {
  username: string;
  email: string;
  password: string;
  displayName: string;
  roles: string[];
}

export interface AssignRoleRequest {
  roleName: string;
}

export interface ResetPasswordRequest {
  newPassword: string;
}

export interface CreateRoleRequest {
  name: string;
  permissions: string[];
}

export interface UpdateRolePermissionsRequest {
  permissions: string[];
}

// ── Inferred types ───────────────────────────────────────────────────────────

export type UserSummary = z.infer<typeof UserSummarySchema>;
export type UserDetail = z.infer<typeof UserDetailSchema>;
export type RoleSummary = z.infer<typeof RoleSummarySchema>;
export type ListUsersRaw = z.infer<typeof ListUsersRawSchema>;
export type ListRolesResponse = z.infer<typeof ListRolesResponseSchema>;
export type PaginatedUsers = z.infer<typeof PaginatedUsersSchema>;
