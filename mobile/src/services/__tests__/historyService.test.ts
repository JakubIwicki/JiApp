jest.mock('../apiClient', () => ({
  __esModule: true,
  default: {
    get: jest.fn(),
  },
}));

import apiClient from '../apiClient';
import { getHistory } from '../historyService';
import type { HistoryResponse } from '../../types/api';

const mockGet = apiClient.get as jest.Mock;

const validResponse: HistoryResponse = {
  searches: [
    {
      id: 1,
      searchText: 'never gonna give you up',
      searchedAt: '2026-01-01T00:00:00.000Z',
    },
  ],
  downloads: [
    {
      id: 2,
      videoTitle: 'Rick Astley - Never Gonna Give You Up',
      videoDescription: 'Classic music video',
      videoId: 'dQw4w9WgXcQ',
      videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
      imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg',
      downloadedAt: '2026-01-01T00:00:00.000Z',
    },
  ],
};

beforeEach(() => {
  jest.clearAllMocks();
});

describe('getHistory', () => {
  it('calls GET /yt/history and returns the parsed response', async () => {
    mockGet.mockResolvedValueOnce({ data: validResponse });

    const result = await getHistory();

    expect(mockGet).toHaveBeenCalledWith('/yt/history', {
      params: { limit: undefined },
      signal: undefined,
    });
    expect(result).toEqual(validResponse);
  });

  it('passes limit as a query param when provided', async () => {
    mockGet.mockResolvedValueOnce({ data: validResponse });

    await getHistory(25);

    expect(mockGet).toHaveBeenCalledWith('/yt/history', {
      params: { limit: 25 },
      signal: undefined,
    });
  });

  it('forwards the AbortSignal to axios', async () => {
    mockGet.mockResolvedValueOnce({ data: validResponse });
    const abortController = new AbortController();

    await getHistory(10, abortController.signal);

    expect(mockGet).toHaveBeenCalledWith('/yt/history', {
      params: { limit: 10 },
      signal: abortController.signal,
    });
  });

  it('rejects when the response violates HistoryResponseSchema', async () => {
    mockGet.mockResolvedValueOnce({
      data: {
        searches: [{ id: 'not-a-number', searchText: 42 }],
        downloads: 'nope',
      },
    });

    await expect(getHistory()).rejects.toThrow();
  });

  it('throws when the request fails', async () => {
    const error = new Error('History fetch failed');
    mockGet.mockRejectedValueOnce(error);

    await expect(getHistory()).rejects.toThrow('History fetch failed');
  });
});
