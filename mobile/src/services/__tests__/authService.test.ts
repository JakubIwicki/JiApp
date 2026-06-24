jest.mock('../apiClient', () => ({
  __esModule: true,
  default: {
    post: jest.fn(),
    get: jest.fn(),
  },
}));

import apiClient from '../apiClient';
import { login, register, checkToken } from '../authService';
import type { LoginResponse } from '../../types/api';

const mockPost = apiClient.post as jest.Mock;
const mockGet = apiClient.get as jest.Mock;

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
    modules: ['YtDownloader', 'Scheduler'],
  };

  const mockApiRaw = {
    accessToken: 'jwt-token-123',
    refreshToken: 'refresh-token-456',
    userId: 1,
    displayName: 'John Doe',
    expiresIn: 3600,
    modules: ['YtDownloader', 'Scheduler'],
  };

  it('calls /auth/login with credentials and returns login data', async () => {
    mockPost.mockResolvedValueOnce({ data: mockApiRaw });

    const result = await login(username, password);

    expect(mockPost).toHaveBeenCalledWith('/auth/login', {
      username,
      password,
    });
    expect(result).toEqual(mockLoginResponse);
  });

  it('defaults modules to an empty array when absent from the response', async () => {
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

    expect(result.modules).toEqual([]);
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
    const mockResponse: LoginResponse = {
      id: 1,
      displayName: 'John Doe',
      token,
      modules: ['YtDownloader', 'Scheduler'],
    };
    mockGet.mockResolvedValueOnce({ data: mockResponse });

    const result = await checkToken(token);

    expect(mockGet).toHaveBeenCalledWith('/auth/me', {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(result).toEqual(mockResponse);
  });

  it('defaults modules to an empty array when absent from /auth/me', async () => {
    mockGet.mockResolvedValueOnce({
      data: { id: 1, displayName: 'John Doe', token },
    });

    const result = await checkToken(token);

    expect(result.modules).toEqual([]);
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
