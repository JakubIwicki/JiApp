jest.mock('../../config', () => ({
  API_BASE_URL: 'https://example.test/api/v1',
}));

jest.mock('../storageService', () => ({
  getToken: jest.fn(),
}));

import { getPreviewUrl, getPreviewHeaders } from '../previewService';
import { getToken } from '../storageService';

const mockGetToken = getToken as jest.Mock;

beforeEach(() => {
  jest.clearAllMocks();
});

describe('getPreviewUrl', () => {
  it('builds the preview URL from API_BASE_URL and the video id', () => {
    expect(getPreviewUrl('dQw4w9WgXcQ')).toBe(
      'https://example.test/api/v1/yt/preview/dQw4w9WgXcQ',
    );
  });
});

describe('getPreviewHeaders', () => {
  it('returns a Bearer header with the stored token', async () => {
    mockGetToken.mockResolvedValue('jwt-token-123');

    await expect(getPreviewHeaders()).resolves.toEqual({
      Authorization: 'Bearer jwt-token-123',
    });
    expect(mockGetToken).toHaveBeenCalledTimes(1);
  });

  it('returns an empty header object when no token is stored', async () => {
    mockGetToken.mockResolvedValue(null);

    await expect(getPreviewHeaders()).resolves.toEqual({});
    expect(mockGetToken).toHaveBeenCalledTimes(1);
  });
});
