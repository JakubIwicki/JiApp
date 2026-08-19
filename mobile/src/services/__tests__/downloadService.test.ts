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
      exists: jest.fn(() => Promise.resolve(false)),
      unlink: jest.fn(() => Promise.resolve()),
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
      exists: jest.fn(() => Promise.resolve(false)),
      unlink: jest.fn(() => Promise.resolve()),
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
  getDownloadStatus,
  getDownloadHistory,
  archiveDownload,
  downloadFile,
  openAudioFile,
} from '../downloadService';
import ReactNativeBlobUtil from 'react-native-blob-util';
import type {
  DownloadRequest,
  DownloadResponse,
  DownloadStatus,
  DownloadHistoryItem,
} from '../../types/api';

const mockPost = apiClient.post as jest.Mock;
const mockGet = apiClient.get as jest.Mock;
const mockPatch = apiClient.patch as jest.Mock;
const mockGetToken = getToken as jest.Mock;
const mockConfig = ReactNativeBlobUtil.config as jest.Mock;
const mockCopyToMediaStore = ReactNativeBlobUtil.MediaCollection
  .copyToMediaStore as jest.Mock;

beforeEach(() => {
  jest.clearAllMocks();
});

/** Drain the microtask queue (setTimeout fires after all microtasks settle). */
const flushMacrotasks = () =>
  new Promise<void>(resolve => setTimeout(resolve, 0));

// --- requestDownloadLink ---

describe('requestDownloadLink', () => {
  const downloadRequest: DownloadRequest = {
    videoId: 'dQw4w9WgXcQ',
    videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
    title: 'Never Gonna Give You Up',
  };

  const mockDownloadResponse: DownloadResponse = {
    tempId: 'job-123',
    downloadUrl: 'https://example.com/downloads/song.mp3',
  };

  it('calls /yt/downloads/mp3 with request and returns download URL', async () => {
    mockPost.mockResolvedValueOnce({ data: mockDownloadResponse });

    const result = await requestDownloadLink(downloadRequest);

    expect(mockPost).toHaveBeenCalledWith(
      '/yt/downloads/mp3',
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
      '/yt/downloads/mp3',
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

// --- getDownloadStatus ---

describe('getDownloadStatus', () => {
  const mockStatus: DownloadStatus = { status: 'ready' };

  it('calls /yt/downloads/mp3/status/:tempId and returns status', async () => {
    mockGet.mockResolvedValueOnce({ data: mockStatus });

    const result = await getDownloadStatus('job-123');

    expect(mockGet).toHaveBeenCalledWith('/yt/downloads/mp3/status/job-123', {
      signal: undefined,
    });
    expect(result).toEqual(mockStatus);
  });

  it('parses a failed status with an error message', async () => {
    mockGet.mockResolvedValueOnce({
      data: { status: 'failed', error: 'Video unavailable' },
    });

    const result = await getDownloadStatus('job-123');

    expect(result).toEqual({ status: 'failed', error: 'Video unavailable' });
  });

  it('rejects an invalid status payload', async () => {
    mockGet.mockResolvedValueOnce({ data: { status: 'bogus' } });

    await expect(getDownloadStatus('job-123')).rejects.toThrow();
  });

  it('throws when fetching status fails', async () => {
    const error = new Error('Status fetch failed');
    mockGet.mockRejectedValueOnce(error);

    await expect(getDownloadStatus('job-123')).rejects.toThrow(
      'Status fetch failed',
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

  it('calls /yt/downloads/history and returns items', async () => {
    mockGet.mockResolvedValueOnce({ data: { items: mockHistoryItems } });

    const result = await getDownloadHistory();

    expect(mockGet).toHaveBeenCalledWith('/yt/downloads/history', {
      params: { limit: undefined },
    });
    expect(result).toEqual(mockHistoryItems);
  });

  it('passes limit when provided', async () => {
    mockGet.mockResolvedValueOnce({ data: { items: mockHistoryItems } });

    await getDownloadHistory(5);

    expect(mockGet).toHaveBeenCalledWith('/yt/downloads/history', {
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
  it('calls PATCH /yt/downloads/history/:id/archive', async () => {
    mockPatch.mockResolvedValueOnce({});

    await archiveDownload(42);

    expect(mockPatch).toHaveBeenCalledWith('/yt/downloads/history/42/archive');
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

    // Should configure download with auth header (timeout left at library default)
    expect(mockConfig).toHaveBeenCalledWith({
      path: '/cache/Never Gonna Give You Up.mp3',
    });
    const configResult = mockConfig.mock.results[0]?.value;
    expect(configResult.fetch).toHaveBeenCalledWith('GET', downloadUrl, {
      Authorization: 'Bearer jwt-token-123',
    });

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
      filePath: expect.any(String),
    });
  });

  it('downloads without auth token when no token is available', async () => {
    mockGetToken.mockResolvedValueOnce(null);

    await downloadFile(downloadUrl, fileName);

    expect(mockConfig).toHaveBeenCalledWith({
      path: '/cache/Never Gonna Give You Up.mp3',
    });
    const configResult = mockConfig.mock.results[0]?.value;
    expect(configResult.fetch).toHaveBeenCalledWith('GET', downloadUrl, {});
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

  it('cancels the blob task and rejects with AbortError when the signal aborts', async () => {
    mockGetToken.mockResolvedValueOnce('jwt-token-123');
    const abortController = new AbortController();

    // Mirror the library: task.cancel() rejects with a 'canceled' error
    let rejectTask: (error: Error) => void = () => {};
    const task = new Promise<unknown>((_resolve, reject) => {
      rejectTask = reject;
    });
    const taskCancel = jest.fn(() => {
      rejectTask(new Error('canceled'));
    });
    (task as unknown as { cancel: () => void }).cancel = taskCancel;
    const mockFetch = jest.fn(() => task);
    (ReactNativeBlobUtil.config as jest.Mock).mockReturnValueOnce({
      fetch: mockFetch,
    });

    const promise = downloadFile(downloadUrl, fileName, abortController.signal);

    // Drain getToken() and fs.exists() so the blob task exists and is listening
    await flushMacrotasks();

    expect(mockConfig).toHaveBeenCalledWith({
      path: '/cache/Never Gonna Give You Up.mp3',
    });
    expect(mockFetch).toHaveBeenCalledTimes(1);

    abortController.abort();

    await expect(promise).rejects.toMatchObject({ name: 'AbortError' });
    expect(taskCancel).toHaveBeenCalledTimes(1);
  });

  it('does not copy to MediaStore when the signal aborts after the fetch resolves', async () => {
    mockGetToken.mockResolvedValueOnce('jwt-token-123');
    const abortController = new AbortController();

    // Task resolves normally, but the abort lands before copyToMediaStore
    let resolveTask: (value: unknown) => void = () => {};
    const task = new Promise<unknown>(resolve => {
      resolveTask = resolve;
    });
    (task as unknown as { cancel: () => void }).cancel = jest.fn();
    const mockFetch = jest.fn(() => task);
    (ReactNativeBlobUtil.config as jest.Mock).mockReturnValueOnce({
      fetch: mockFetch,
    });

    const promise = downloadFile(downloadUrl, fileName, abortController.signal);

    await flushMacrotasks();

    expect(mockFetch).toHaveBeenCalledTimes(1);

    abortController.abort();
    resolveTask({ path: () => '/cache/temp-download.mp3' });

    await expect(promise).rejects.toMatchObject({ name: 'AbortError' });
    expect(mockCopyToMediaStore).not.toHaveBeenCalled();
    expect(ReactNativeBlobUtil.fs.unlink).toHaveBeenCalledWith(
      '/cache/Never Gonna Give You Up.mp3',
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
    );
    expect(result).toBe(true);
  });
});
