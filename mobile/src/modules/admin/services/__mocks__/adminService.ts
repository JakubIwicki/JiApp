import { createMockFn } from '../../../../test/createMockFn';
import type {
  PaginatedUsers,
  UserDetail,
  UserSummary,
  RoleSummary,
  CreateUserRequest,
  AssignRoleRequest,
  ResetPasswordRequest,
  CreateRoleRequest,
  UpdateRolePermissionsRequest,
} from '../../types/api';

// ── Default stub data ──────────────────────────────────────────────────────

const defaultUsers: UserSummary[] = [];
const defaultRoles: RoleSummary[] = [];

// ── Internal state ─────────────────────────────────────────────────────────

let _users: UserSummary[] = defaultUsers;
let _roles: RoleSummary[] = defaultRoles;
let _createUserError: Error | null = null;
let _deleteUserError: Error | null = null;

// ── Mock functions ─────────────────────────────────────────────────────────

export const listUsers = createMockFn(
  async (
    _search?: string,
    _page?: number,
    _pageSize?: number,
  ): Promise<PaginatedUsers> => {
    return { items: _users, total: _users.length };
  },
);

export const getUser = createMockFn(
  async (userId: number): Promise<UserDetail> => {
    return {
      id: userId,
      username: 'mock-user',
      email: 'mock@example.com',
      displayName: 'Mock User',
      roles: ['User'],
      isLockedOut: false,
      lockoutEnd: null,
    };
  },
);

export const createUser = createMockFn(
  async (_data: CreateUserRequest): Promise<number> => {
    if (_createUserError) throw _createUserError;
    return 99;
  },
);

export const disableUser = createMockFn(
  async (_userId: number): Promise<void> => {},
);

export const enableUser = createMockFn(
  async (_userId: number): Promise<void> => {},
);

export const deleteUser = createMockFn(
  async (_userId: number): Promise<void> => {
    if (_deleteUserError) throw _deleteUserError;
  },
);

export const assignRole = createMockFn(
  async (_userId: number, _data: AssignRoleRequest): Promise<void> => {},
);

export const removeRole = createMockFn(
  async (_userId: number, _roleName: string): Promise<void> => {},
);

export const resetPassword = createMockFn(
  async (_userId: number, _data: ResetPasswordRequest): Promise<void> => {},
);

export const listRoles = createMockFn(async (): Promise<RoleSummary[]> => {
  return _roles;
});

export const createRole = createMockFn(
  async (_data: CreateRoleRequest): Promise<void> => {},
);

export const updateRolePermissions = createMockFn(
  async (
    _roleName: string,
    _data: UpdateRolePermissionsRequest,
  ): Promise<void> => {},
);

export const deleteRole = createMockFn(
  async (_roleName: string): Promise<void> => {},
);

// ── Fluent scenario builders (.withX()) ────────────────────────────────────

export function withUsers(users: UserSummary[] = defaultUsers): UserSummary[] {
  _users = users;
  return _users;
}

export function withRoles(roles: RoleSummary[] = defaultRoles): RoleSummary[] {
  _roles = roles;
  return _roles;
}

export function withCreateUserError(
  error: Error = new Error('Mock error'),
): Error {
  _createUserError = error;
  return error;
}

export function withDeleteUserError(
  error: Error = new Error('Mock error'),
): Error {
  _deleteUserError = error;
  return error;
}

// ── Reset ──────────────────────────────────────────────────────────────────

export function reset(): void {
  _users = defaultUsers;
  _roles = defaultRoles;
  _createUserError = null;
  _deleteUserError = null;

  if (typeof jest !== 'undefined') {
    listUsers.mockClear();
    getUser.mockClear();
    createUser.mockClear();
    disableUser.mockClear();
    enableUser.mockClear();
    deleteUser.mockClear();
    assignRole.mockClear();
    removeRole.mockClear();
    resetPassword.mockClear();
    listRoles.mockClear();
    createRole.mockClear();
    updateRolePermissions.mockClear();
    deleteRole.mockClear();
  }
}
