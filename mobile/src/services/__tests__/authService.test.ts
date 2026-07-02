jest.mock('../apiClient', () => ({
  __esModule: true,
  default: {
    post: jest.fn(),
    get: jest.fn(),
  },
}));

jest.mock('axios', () => ({
  __esModule: true,
  default: {
    post: jest.fn(),
  },
}));

import apiClient from '../apiClient';
import axios from 'axios';
import { login, register, checkToken, refreshToken } from '../authService';
import type { LoginResponse } from '../../types/api';

const mockPost = apiClient.post as jest.Mock;
const mockGet = apiClient.get as jest.Mock;
const mockAxiosPost = axios.post as jest.Mock;

beforeEach(() => {
  jest.clearAllMocks();
});

// --- login ---

describe('login', () => {
  const username = 'johndoe';
  const password = 's3cret';
  const mockLoginResponse: LoginResponse = {
    id: 1,
    displayName: 'John Doe',
    token: 'jwt-token-123',
    refreshToken: 'refresh-token-456',
    roles: ['User'],
    permissions: ['ytdownloader.access', 'scheduler.access'],
  };

  const mockApiRaw = {
    accessToken: 'jwt-token-123',
    refreshToken: 'refresh-token-456',
    userId: 1,
    displayName: 'John Doe',
    expiresIn: 3600,
    roles: ['User'],
    permissions: ['ytdownloader.access', 'scheduler.access'],
  };

  it('calls /auth/login with credentials and returns login data including refreshToken', async () => {
    mockPost.mockResolvedValueOnce({ data: mockApiRaw });

    const result = await login(username, password);

    expect(mockPost).toHaveBeenCalledWith('/auth/login', {
      username,
      password,
    });
    expect(result).toEqual(mockLoginResponse);
    expect(result.refreshToken).toBe('refresh-token-456');
  });

  it('defaults roles and permissions to empty arrays when absent from the response', async () => {
    mockPost.mockResolvedValueOnce({
      data: {
        accessToken: 'jwt-token-123',
        refreshToken: 'refresh-token-456',
        userId: 1,
        displayName: 'John Doe',
        expiresIn: 3600,
      },
    });

    const result = await login(username, password);

    expect(result.roles).toEqual([]);
    expect(result.permissions).toEqual([]);
  });

  it('throws when login fails', async () => {
    const error = new Error('Invalid credentials');
    mockPost.mockRejectedValueOnce(error);

    await expect(login(username, password)).rejects.toThrow(
      'Invalid credentials',
    );
    expect(mockPost).toHaveBeenCalledWith('/auth/login', {
      username,
      password,
    });
  });
});

// --- register ---

describe('register', () => {
  const username = 'janedoe';
  const email = 'jane@example.com';
  const password = 'p4ssword';
  const displayName = 'Jane Doe';

  it('calls /auth/register with user details on success', async () => {
    mockPost.mockResolvedValueOnce({});

    await register(username, email, password, displayName);

    expect(mockPost).toHaveBeenCalledWith('/auth/register', {
      username,
      email,
      password,
      displayName,
    });
  });

  it('throws when registration fails', async () => {
    const error = new Error('Username taken');
    mockPost.mockRejectedValueOnce(error);

    await expect(
      register(username, email, password, displayName),
    ).rejects.toThrow('Username taken');
    expect(mockPost).toHaveBeenCalledWith('/auth/register', {
      username,
      email,
      password,
      displayName,
    });
  });
});

// --- checkToken ---

describe('checkToken', () => {
  const token = 'valid-jwt-token';

  it('calls /auth/me with Bearer header and returns user data', async () => {
    const mockApiRaw = {
      id: 1,
      displayName: 'John Doe',
      roles: ['User'],
      permissions: ['ytdownloader.access', 'scheduler.access'],
    };
    mockGet.mockResolvedValueOnce({ data: mockApiRaw });

    const result = await checkToken(token);

    expect(mockGet).toHaveBeenCalledWith('/auth/me', {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(result).toEqual({
      id: 1,
      displayName: 'John Doe',
      token,
      roles: ['User'],
      permissions: ['ytdownloader.access', 'scheduler.access'],
    });
  });

  it('defaults roles and permissions to empty arrays when absent from /auth/me', async () => {
    mockGet.mockResolvedValueOnce({
      data: { id: 1, displayName: 'John Doe' },
    });

    const result = await checkToken(token);

    expect(result.roles).toEqual([]);
    expect(result.permissions).toEqual([]);
  });

  it('throws when token check fails', async () => {
    const error = new Error('Token expired');
    mockGet.mockRejectedValueOnce(error);

    await expect(checkToken(token)).rejects.toThrow('Token expired');
    expect(mockGet).toHaveBeenCalledWith('/auth/me', {
      headers: { Authorization: `Bearer ${token}` },
    });
  });
});

// --- refreshToken ---

describe('refreshToken', () => {
  it('calls /auth/refresh with raw axios and returns validated response', async () => {
    const mockApiRaw = {
      accessToken: 'new-access',
      refreshToken: 'new-refresh',
      expiresIn: 3600,
    };
    mockAxiosPost.mockResolvedValueOnce({ data: mockApiRaw });

    const result = await refreshToken('old-refresh');

    expect(mockAxiosPost).toHaveBeenCalledWith(
      expect.stringContaining('/auth/refresh'),
      { refreshToken: 'old-refresh' },
      { headers: { 'Content-Type': 'application/json' } },
    );
    expect(result).toEqual(mockApiRaw);
  });

  it('throws when refresh response fails Zod validation', async () => {
    mockAxiosPost.mockResolvedValueOnce({
      data: { accessToken: 'ok', expiresIn: 3600 },
    });

    await expect(refreshToken('bad')).rejects.toThrow();
  });

  it('throws when /auth/refresh rejects', async () => {
    mockAxiosPost.mockRejectedValueOnce(new Error('Network error'));

    await expect(refreshToken('stale')).rejects.toThrow('Network error');
  });
});
