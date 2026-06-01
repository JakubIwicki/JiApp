jest.mock('../apiClient', () => ({
  __esModule: true,
  default: {
    post: jest.fn(),
    get: jest.fn(),
    patch: jest.fn(),
  },
}));

jest.mock('../storageService', () => ({
  getToken: jest.fn(),
}));

// Extend the global ReactNativeBlobUtil mock with MediaCollection and android
jest.mock('react-native-blob-util', () => ({
  __esModule: true,
  default: {
    fs: {
      dirs: {
        DownloadDir: '/storage/emulated/0/Download',
        DocumentDir: '/storage/emulated/0/Documents',
        CacheDir: '/cache',
      },
    },
    config: jest.fn(() => ({
      fetch: jest.fn(() =>
        Promise.resolve({
          path: jest.fn(() => '/cache/temp-download.mp3'),
        }),
      ),
    })),
    fetch: jest.fn(),
    MediaCollection: {
      copyToMediaStore: jest.fn(() =>
        Promise.resolve('content://media/external/audio/100'),
      ),
    },
    android: {
      actionViewIntent: jest.fn(() => Promise.resolve(true)),
    },
  },
  ReactNativeBlobUtil: {
    fs: {
      dirs: {
        DownloadDir: '/storage/emulated/0/Download',
        DocumentDir: '/storage/emulated/0/Documents',
        CacheDir: '/cache',
      },
    },
    config: jest.fn(() => ({
      fetch: jest.fn(() =>
        Promise.resolve({
          path: jest.fn(() => '/cache/temp-download.mp3'),
        }),
      ),
    })),
    fetch: jest.fn(),
    MediaCollection: {
      copyToMediaStore: jest.fn(() =>
        Promise.resolve('content://media/external/audio/100'),
      ),
    },
    android: {
      actionViewIntent: jest.fn(() => Promise.resolve(true)),
    },
  },
}));

import apiClient from '../apiClient';
import { getToken } from '../storageService';
import {
  requestDownloadLink,
  getDownloadHistory,
  archiveDownload,
  downloadFile,
  openAudioFile,
} from '../downloadService';
import ReactNativeBlobUtil from 'react-native-blob-util';
import type { DownloadRequest, DownloadResponse, DownloadHistoryItem } from '../../types/api';

const mockPost = apiClient.post as jest.Mock;
const mockGet = apiClient.get as jest.Mock;
const mockPatch = apiClient.patch as jest.Mock;
const mockGetToken = getToken as jest.Mock;
const mockConfig = ReactNativeBlobUtil.config as jest.Mock;
const mockCopyToMediaStore =
  ReactNativeBlobUtil.MediaCollection.copyToMediaStore as jest.Mock;

beforeEach(() => {
  jest.clearAllMocks();
});

// --- requestDownloadLink ---

describe('requestDownloadLink', () => {
  const downloadRequest: DownloadRequest = {
    videoId: 'dQw4w9WgXcQ',
    videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
    title: 'Never Gonna Give You Up',
  };

  const mockDownloadResponse: DownloadResponse = {
    downloadUrl: 'https://example.com/downloads/song.mp3',
  };

  it('calls /downloads/mp3 with request and returns download URL', async () => {
    mockPost.mockResolvedValueOnce({ data: mockDownloadResponse });

    const result = await requestDownloadLink(downloadRequest);

    expect(mockPost).toHaveBeenCalledWith(
      '/downloads/mp3',
      downloadRequest,
      { signal: undefined },
    );
    expect(result).toEqual(mockDownloadResponse);
  });

  it('passes signal when provided', async () => {
    mockPost.mockResolvedValueOnce({ data: mockDownloadResponse });
    const abortController = new AbortController();

    await requestDownloadLink(downloadRequest, abortController.signal);

    expect(mockPost).toHaveBeenCalledWith(
      '/downloads/mp3',
      downloadRequest,
      { signal: abortController.signal },
    );
  });

  it('throws when request fails', async () => {
    const error = new Error('Download link request failed');
    mockPost.mockRejectedValueOnce(error);

    await expect(requestDownloadLink(downloadRequest)).rejects.toThrow(
      'Download link request failed',
    );
  });
});

// --- getDownloadHistory ---

describe('getDownloadHistory', () => {
  const mockHistoryItems: DownloadHistoryItem[] = [
    {
      id: 100,
      videoTitle: 'Rick Astley - Never Gonna Give You Up',
      videoDescription: 'Classic music video',
      videoId: 'dQw4w9WgXcQ',
      videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
      imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg',
      downloadedAt: '2026-01-01T00:00:00.000Z',
    },
  ];

  it('calls /downloads/history and returns items', async () => {
    mockGet.mockResolvedValueOnce({ data: { items: mockHistoryItems } });

    const result = await getDownloadHistory();

    expect(mockGet).toHaveBeenCalledWith('/downloads/history', {
      params: { limit: undefined },
    });
    expect(result).toEqual(mockHistoryItems);
  });

  it('passes limit when provided', async () => {
    mockGet.mockResolvedValueOnce({ data: { items: mockHistoryItems } });

    await getDownloadHistory(5);

    expect(mockGet).toHaveBeenCalledWith('/downloads/history', {
      params: { limit: 5 },
    });
  });

  it('throws when fetching history fails', async () => {
    const error = new Error('History fetch failed');
    mockGet.mockRejectedValueOnce(error);

    await expect(getDownloadHistory()).rejects.toThrow('History fetch failed');
  });
});

// --- archiveDownload ---

describe('archiveDownload', () => {
  it('calls PATCH /downloads/history/:id/archive', async () => {
    mockPatch.mockResolvedValueOnce({});

    await archiveDownload(42);

    expect(mockPatch).toHaveBeenCalledWith('/downloads/history/42/archive');
  });

  it('throws when archiving fails', async () => {
    const error = new Error('Archive failed');
    mockPatch.mockRejectedValueOnce(error);

    await expect(archiveDownload(99)).rejects.toThrow('Archive failed');
  });
});

// --- downloadFile ---

describe('downloadFile', () => {
  const downloadUrl = 'https://example.com/downloads/song.mp3';
  const fileName = 'Never Gonna Give You Up';

  it('downloads with auth token and copies to MediaStore', async () => {
    mockGetToken.mockResolvedValueOnce('jwt-token-123');

    const result = await downloadFile(downloadUrl, fileName);

    // Should check token
    expect(mockGetToken).toHaveBeenCalledTimes(1);

    // Should configure download with auth header
    expect(mockConfig).toHaveBeenCalledWith({ fileCache: true });
    const configResult = mockConfig.mock.results[0]?.value;
    expect(configResult.fetch).toHaveBeenCalledWith(
      'GET',
      downloadUrl,
      { Authorization: 'Bearer jwt-token-123' },
    );

    // Should copy to MediaStore
    expect(mockCopyToMediaStore).toHaveBeenCalledWith(
      {
        name: `${fileName}.mp3`,
        parentFolder: '',
        mimeType: 'audio/mpeg',
      },
      'Download',
      expect.any(String),
    );

    expect(result).toEqual({
      contentUri: 'content://media/external/audio/100',
      displayPath: 'Download/Never Gonna Give You Up.mp3',
    });
  });

  it('downloads without auth token when no token is available', async () => {
    mockGetToken.mockResolvedValueOnce(null);

    await downloadFile(downloadUrl, fileName);

    expect(mockConfig).toHaveBeenCalledWith({ fileCache: true });
    const configResult = mockConfig.mock.results[0]?.value;
    expect(configResult.fetch).toHaveBeenCalledWith(
      'GET',
      downloadUrl,
      {},
    );
  });

  it('throws when download fails', async () => {
    mockGetToken.mockResolvedValueOnce('jwt-token-123');
    const mockFetch = jest.fn(() => Promise.reject(new Error('Network error')));
    (ReactNativeBlobUtil.config as jest.Mock).mockReturnValueOnce({
      fetch: mockFetch,
    });

    await expect(downloadFile(downloadUrl, fileName)).rejects.toThrow(
      'Network error',
    );
  });
});

// --- openAudioFile ---

describe('openAudioFile', () => {
  it('calls actionViewIntent with audio/mpeg', async () => {
    const result = await openAudioFile('/path/to/file.mp3', 'Open with');

    expect(ReactNativeBlobUtil.android.actionViewIntent).toHaveBeenCalledWith(
      '/path/to/file.mp3',
      'audio/mpeg',
      'Open with',
    );
    expect(result).toBe(true);
  });
});
