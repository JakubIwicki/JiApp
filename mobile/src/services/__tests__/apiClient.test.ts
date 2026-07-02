jest.mock('axios', () => {
  const mockInstance = {
    request: jest.fn(),
    interceptors: {
      request: { use: jest.fn() },
      response: { use: jest.fn() },
    },
  };
  return {
    create: jest.fn(() => mockInstance),
    post: jest.fn(),
  };
});

jest.mock('../storageService', () => ({
  getToken: jest.fn(),
  getRefreshToken: jest.fn(),
  saveToken: jest.fn(),
  saveRefreshToken: jest.fn(),
  clearToken: jest.fn(),
  clearRefreshToken: jest.fn(),
  clearUserId: jest.fn(),
  clearDisplayName: jest.fn(),
  clearUsername: jest.fn(),
  clearCredentials: jest.fn(),
}));

import axios from 'axios';
import * as storageService from '../storageService';

// Importing apiClient triggers module loading which registers interceptors on the mock
import '../apiClient';

// Capture the error handler at module load time (before any clearAllMocks)
const mockInstance = (axios.create as jest.Mock).mock.results[0].value;
const errorHandler = (mockInstance.interceptors.response.use as jest.Mock).mock
  .calls[0][1];

const mockGetRefreshToken = storageService.getRefreshToken as jest.Mock;
const mockSaveToken = storageService.saveToken as jest.Mock;
const mockSaveRefreshToken = storageService.saveRefreshToken as jest.Mock;
const mockClearToken = storageService.clearToken as jest.Mock;
const mockClearRefreshToken = storageService.clearRefreshToken as jest.Mock;
const mockClearUserId = storageService.clearUserId as jest.Mock;
const mockClearDisplayName = storageService.clearDisplayName as jest.Mock;
const mockClearUsername = storageService.clearUsername as jest.Mock;
const mockClearCredentials = storageService.clearCredentials as jest.Mock;

describe('apiClient 401 response interceptor', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockGetRefreshToken.mockResolvedValue(null);
  });

  // ── Basic 401 handling ──────────────────────────────────────────────────

  it('clears all auth storage keys on 401 when no refresh token available', async () => {
    const error401 = { response: { status: 401 }, config: {} };

    await expect(errorHandler(error401)).rejects.toEqual(error401);

    expect(mockClearToken).toHaveBeenCalledTimes(1);
    expect(mockClearRefreshToken).toHaveBeenCalledTimes(1);
    expect(mockClearUserId).toHaveBeenCalledTimes(1);
    expect(mockClearDisplayName).toHaveBeenCalledTimes(1);
    expect(mockClearUsername).toHaveBeenCalledTimes(1);
    expect(mockClearCredentials).toHaveBeenCalledTimes(1);
  });

  it('does not clear storage on non-401 errors', async () => {
    const error500 = { response: { status: 500 } };

    await expect(errorHandler(error500)).rejects.toEqual(error500);

    expect(mockClearToken).not.toHaveBeenCalled();
    expect(mockClearRefreshToken).not.toHaveBeenCalled();
    expect(mockClearUserId).not.toHaveBeenCalled();
    expect(mockClearDisplayName).not.toHaveBeenCalled();
    expect(mockClearUsername).not.toHaveBeenCalled();
    expect(mockClearCredentials).not.toHaveBeenCalled();
  });

  it('does not clear storage on network errors without response', async () => {
    const networkError = { message: 'Network Error', response: undefined };

    await expect(errorHandler(networkError)).rejects.toEqual(networkError);

    expect(mockClearToken).not.toHaveBeenCalled();
    expect(mockClearRefreshToken).not.toHaveBeenCalled();
    expect(mockClearUserId).not.toHaveBeenCalled();
    expect(mockClearDisplayName).not.toHaveBeenCalled();
    expect(mockClearUsername).not.toHaveBeenCalled();
    expect(mockClearCredentials).not.toHaveBeenCalled();
  });

  it('does not intercept 401 for /auth/login', async () => {
    const config = { url: '/auth/login' };
    const error401 = { response: { status: 401 }, config };

    await expect(errorHandler(error401)).rejects.toEqual(error401);

    expect(mockGetRefreshToken).not.toHaveBeenCalled();
  });

  it('does not intercept 401 for /auth/register', async () => {
    const config = { url: '/auth/register' };
    const error401 = { response: { status: 401 }, config };

    await expect(errorHandler(error401)).rejects.toEqual(error401);

    expect(mockGetRefreshToken).not.toHaveBeenCalled();
  });

  it('does not intercept 401 for /auth/refresh', async () => {
    const config = { url: '/auth/refresh' };
    const error401 = { response: { status: 401 }, config };

    await expect(errorHandler(error401)).rejects.toEqual(error401);

    expect(mockGetRefreshToken).not.toHaveBeenCalled();
  });

  it('does not retry a request already marked _isRetry', async () => {
    const config = { _isRetry: true };
    const error401 = { response: { status: 401 }, config };

    await expect(errorHandler(error401)).rejects.toEqual(error401);

    expect(mockGetRefreshToken).not.toHaveBeenCalled();
  });

  // ── Refresh-based re-auth ───────────────────────────────────────────────

  it('calls /auth/refresh with stored refresh token, stores rotated tokens, retries', async () => {
    mockGetRefreshToken.mockResolvedValueOnce('old-refresh-token');
    (axios.post as jest.Mock).mockResolvedValueOnce({
      data: {
        accessToken: 'new-access-token',
        refreshToken: 'new-refresh-token',
        expiresIn: 3600,
      },
    });

    const config = {
      url: '/some/protected/endpoint',
      headers: {} as Record<string, string>,
    };
    const error401 = { response: { status: 401 }, config };

    await errorHandler(error401);

    expect(axios.post).toHaveBeenCalledWith(
      expect.stringContaining('/auth/refresh'),
      { refreshToken: 'old-refresh-token' },
      { headers: { 'Content-Type': 'application/json' } },
    );
    expect(mockSaveToken).toHaveBeenCalledWith('new-access-token');
    expect(mockSaveRefreshToken).toHaveBeenCalledWith('new-refresh-token');
    // Should have retried the original request
    expect(config.headers.Authorization).toBe('Bearer new-access-token');
    expect((config as Record<string, unknown>)._isRetry).toBe(true);
  });

  it('clears all auth state on refresh failure', async () => {
    mockGetRefreshToken.mockResolvedValueOnce('expired-refresh');
    (axios.post as jest.Mock).mockRejectedValueOnce(
      new Error('Refresh rejected'),
    );

    const error401 = { response: { status: 401 }, config: {} };

    await expect(errorHandler(error401)).rejects.toEqual(error401);

    expect(mockClearToken).toHaveBeenCalledTimes(1);
    expect(mockClearRefreshToken).toHaveBeenCalledTimes(1);
    expect(mockClearUserId).toHaveBeenCalledTimes(1);
    expect(mockClearDisplayName).toHaveBeenCalledTimes(1);
    expect(mockClearUsername).toHaveBeenCalledTimes(1);
    expect(mockClearCredentials).toHaveBeenCalledTimes(1);
  });

  // ── Single-flight ───────────────────────────────────────────────────────

  it('triggers only one /auth/refresh for concurrent 401s', async () => {
    // Delay the first refresh call so concurrent requests pile up
    let resolveRefresh!: (value: unknown) => void;
    const refreshPromise = new Promise(resolve => {
      resolveRefresh = resolve;
    });
    (axios.post as jest.Mock).mockReturnValueOnce(refreshPromise);

    mockGetRefreshToken.mockResolvedValue('shared-refresh-token');

    const config1 = {
      url: '/endpoint-a',
      headers: {} as Record<string, string>,
    };
    const config2 = {
      url: '/endpoint-b',
      headers: {} as Record<string, string>,
    };
    const error1 = { response: { status: 401 }, config: config1 };
    const error2 = { response: { status: 401 }, config: config2 };

    // Fire two 401s concurrently
    const result1 = errorHandler(error1);
    const result2 = errorHandler(error2);

    // Resolve the refresh
    resolveRefresh({
      data: {
        accessToken: 'shared-new-access',
        refreshToken: 'shared-new-refresh',
        expiresIn: 3600,
      },
    });

    await Promise.all([result1, result2]);

    // Only one refresh call despite two 401s
    expect(axios.post).toHaveBeenCalledTimes(1);
    expect(mockSaveToken).toHaveBeenCalledTimes(1);
    expect(mockSaveRefreshToken).toHaveBeenCalledTimes(1);
    // Both retries should have the new token
    expect(config1.headers.Authorization).toBe('Bearer shared-new-access');
    expect(config2.headers.Authorization).toBe('Bearer shared-new-access');
  });

  // ── _serverError attachment ─────────────────────────────────────────────

  it('attaches _serverError from response body for 5xx errors', async () => {
    const error502 = {
      isAxiosError: true,
      response: {
        status: 502,
        data: { error: 'Failed to download video: Video unavailable' },
      },
    };

    await expect(errorHandler(error502)).rejects.toMatchObject({
      _serverError: 'Failed to download video: Video unavailable',
    });

    expect(mockClearToken).not.toHaveBeenCalled();
  });

  it('does not attach _serverError when error response has no data.error', async () => {
    const error500 = {
      isAxiosError: true,
      response: { status: 500, data: { details: 'No error property' } },
    };

    const result = await expect(
      errorHandler(error500),
    ).rejects.not.toMatchObject({
      _serverError: expect.any(String),
    });

    expect(mockClearToken).not.toHaveBeenCalled();
  });

  it('attaches _serverError on 401 errors (so login/register can display server messages)', async () => {
    const error401 = {
      isAxiosError: true,
      response: {
        status: 401,
        data: { error: 'Invalid credentials' },
        config: {},
      },
    };

    await expect(errorHandler(error401)).rejects.toMatchObject({
      _serverError: 'Invalid credentials',
    });
  });
});
