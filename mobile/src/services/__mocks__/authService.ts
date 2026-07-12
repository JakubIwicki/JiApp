import { createMockFn } from '../../test/createMockFn';
import type { LoginResponse, RefreshResponse } from '../../types/api';

// ── Default stub data ──────────────────────────────────────────────────────

const defaultUser: LoginResponse = {
  id: 1,
  displayName: 'Mock User',
  token: 'mock-jwt-token',
  roles: ['User'],
  permissions: ['ytdownloader.access', 'scheduler.access'],
};

const defaultRefresh: RefreshResponse = {
  accessToken: 'mock-refreshed-jwt',
  refreshToken: 'mock-refreshed-rt',
  expiresIn: 3600,
};

// ── Internal state ─────────────────────────────────────────────────────────

let _loginResponse: LoginResponse = { ...defaultUser };
let _loginError: Error | null = null;
let _registerError: Error | null = null;
let _checkTokenResponse: LoginResponse = { ...defaultUser };
let _checkTokenError: Error | null = null;
let _updateProfileError: Error | null = null;
let _refreshTokenResponse: RefreshResponse = { ...defaultRefresh };
let _refreshTokenError: Error | null = null;

// ── Mock functions ─────────────────────────────────────────────────────────

export const login = createMockFn(
  async (_username: string, _password: string): Promise<LoginResponse> => {
    if (_loginError) throw _loginError;
    return _loginResponse;
  },
);

export const register = createMockFn(
  async (
    _username: string,
    _email: string,
    _password: string,
    _displayName: string,
  ): Promise<void> => {
    if (_registerError) throw _registerError;
  },
);

export const checkToken = createMockFn(
  async (_token: string): Promise<LoginResponse> => {
    if (_checkTokenError) throw _checkTokenError;
    return _checkTokenResponse;
  },
);

export const updateProfile = createMockFn(
  async (_displayName: string, _email: string): Promise<void> => {
    if (_updateProfileError) throw _updateProfileError;
  },
);

export const refreshToken = createMockFn(
  async (_token: string): Promise<RefreshResponse> => {
    if (_refreshTokenError) throw _refreshTokenError;
    return _refreshTokenResponse;
  },
);

// ── Fluent scenario builders (.withX()) ────────────────────────────────────

export function withLoginSuccess(
  overrides?: Partial<LoginResponse>,
): LoginResponse {
  _loginError = null;
  _loginResponse = { ...defaultUser, ...overrides };
  return _loginResponse;
}

export function withLoginFailure(
  error: Error = new Error('Invalid credentials'),
): Error {
  _loginError = error;
  return error;
}

export function withRegisterSuccess(): void {
  _registerError = null;
}

export function withRegisterFailure(
  error: Error = new Error('Registration failed'),
): Error {
  _registerError = error;
  return error;
}

export function withCheckTokenSuccess(
  overrides?: Partial<LoginResponse>,
): LoginResponse {
  _checkTokenError = null;
  _checkTokenResponse = { ...defaultUser, ...overrides };
  return _checkTokenResponse;
}

export function withCheckTokenFailure(
  error: Error = new Error('Token invalid'),
): Error {
  _checkTokenError = error;
  return error;
}

export function withUpdateProfileSuccess(): void {
  _updateProfileError = null;
}

export function withUpdateProfileFailure(
  error: Error = new Error('Failed to update profile'),
): Error {
  _updateProfileError = error;
  return error;
}

export function withRefreshTokenSuccess(
  overrides?: Partial<RefreshResponse>,
): RefreshResponse {
  _refreshTokenError = null;
  _refreshTokenResponse = { ...defaultRefresh, ...overrides };
  return _refreshTokenResponse;
}

export function withRefreshTokenFailure(
  error: Error = new Error('Refresh failed'),
): Error {
  _refreshTokenError = error;
  return error;
}

// ── Reset ──────────────────────────────────────────────────────────────────

export function reset(): void {
  _loginError = null;
  _loginResponse = { ...defaultUser };
  _registerError = null;
  _checkTokenError = null;
  _checkTokenResponse = { ...defaultUser };
  _updateProfileError = null;
  _refreshTokenError = null;
  _refreshTokenResponse = { ...defaultRefresh };

  if (typeof jest !== 'undefined') {
    login.mockClear();
    register.mockClear();
    checkToken.mockClear();
    updateProfile.mockClear();
    refreshToken.mockClear();
  }
}
