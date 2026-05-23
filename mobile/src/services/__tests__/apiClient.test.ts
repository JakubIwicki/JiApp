jest.mock('axios', () => {
  const mockInstance = {
    interceptors: {
      request: { use: jest.fn() },
      response: { use: jest.fn() },
    },
  };
  return {
    create: jest.fn(() => mockInstance),
  };
});

jest.mock('../storageService', () => ({
  getToken: jest.fn(),
  clearToken: jest.fn(),
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
const errorHandler = (mockInstance.interceptors.response.use as jest.Mock)
  .mock.calls[0][1];

describe('apiClient 401 response interceptor', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('clears all 5 auth storage keys on 401 response', async () => {
    const error401 = { response: { status: 401 } };

    await expect(errorHandler(error401)).rejects.toEqual(error401);

    expect(storageService.clearToken).toHaveBeenCalledTimes(1);
    expect(storageService.clearUserId).toHaveBeenCalledTimes(1);
    expect(storageService.clearDisplayName).toHaveBeenCalledTimes(1);
    expect(storageService.clearUsername).toHaveBeenCalledTimes(1);
    expect(storageService.clearCredentials).toHaveBeenCalledTimes(1);
  });

  it('does not clear storage on non-401 errors', async () => {
    const error500 = { response: { status: 500 } };

    await expect(errorHandler(error500)).rejects.toEqual(error500);

    expect(storageService.clearToken).not.toHaveBeenCalled();
    expect(storageService.clearUserId).not.toHaveBeenCalled();
    expect(storageService.clearDisplayName).not.toHaveBeenCalled();
    expect(storageService.clearUsername).not.toHaveBeenCalled();
    expect(storageService.clearCredentials).not.toHaveBeenCalled();
  });

  it('does not clear storage on network errors without response', async () => {
    const networkError = { message: 'Network Error', response: undefined };

    await expect(errorHandler(networkError)).rejects.toEqual(networkError);

    expect(storageService.clearToken).not.toHaveBeenCalled();
    expect(storageService.clearUserId).not.toHaveBeenCalled();
    expect(storageService.clearDisplayName).not.toHaveBeenCalled();
    expect(storageService.clearUsername).not.toHaveBeenCalled();
    expect(storageService.clearCredentials).not.toHaveBeenCalled();
  });

  it('attaches _serverError from response body for 5xx errors', async () => {
    const error502 = {
      isAxiosError: true,
      response: { status: 502, data: { error: 'Failed to download video: Video unavailable' } },
    };

    await expect(errorHandler(error502)).rejects.toMatchObject({
      _serverError: 'Failed to download video: Video unavailable',
    });

    expect(storageService.clearToken).not.toHaveBeenCalled();
  });

  it('does not attach _serverError when error response has no data.error', async () => {
    const error500 = {
      isAxiosError: true,
      response: { status: 500, data: { details: 'No error property' } },
    };

    const result = await expect(errorHandler(error500)).rejects.not.toMatchObject({
      _serverError: expect.any(String),
    });

    expect(storageService.clearToken).not.toHaveBeenCalled();
  });

  it('does not attach _serverError on 401 errors', async () => {
    const error401 = {
      isAxiosError: true,
      response: { status: 401, data: { error: 'Unauthorized' } },
    };

    await expect(errorHandler(error401)).rejects.not.toMatchObject({
      _serverError: expect.any(String),
    });
  });
});
