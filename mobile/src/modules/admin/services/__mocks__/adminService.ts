import type {
  PaginatedUsers,
  UserDetail,
  UserSummary,
  RoleSummary,
} from '../../types/api';

export const listUsers = jest.fn<
  Promise<PaginatedUsers>,
  [string?, number?, number?]
>();
export const getUser = jest.fn<Promise<UserDetail>, [number]>();
export const createUser = jest.fn<
  Promise<UserSummary>,
  [import('../../types/api').CreateUserRequest]
>();
export const disableUser = jest.fn<Promise<void>, [number]>();
export const enableUser = jest.fn<Promise<void>, [number]>();
export const deleteUser = jest.fn<Promise<void>, [number]>();
export const assignRole = jest.fn<
  Promise<void>,
  [number, import('../../types/api').AssignRoleRequest]
>();
export const removeRole = jest.fn<Promise<void>, [number, string]>();
export const resetPassword = jest.fn<
  Promise<void>,
  [number, import('../../types/api').ResetPasswordRequest]
>();
export const listRoles = jest.fn<Promise<RoleSummary[]>, []>();
export const createRole = jest.fn<
  Promise<RoleSummary>,
  [import('../../types/api').CreateRoleRequest]
>();
export const updateRolePermissions = jest.fn<
  Promise<RoleSummary>,
  [string, import('../../types/api').UpdateRolePermissionsRequest]
>();
export const deleteRole = jest.fn<Promise<void>, [string]>();
