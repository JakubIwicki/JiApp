jest.mock('../apiClient', () => ({
  __esModule: true,
  default: {
    post: jest.fn(),
    get: jest.fn(),
    patch: jest.fn(),
  },
}));

import apiClient from '../apiClient';
import {
  searchVideos,
  getSearchHistory,
  archiveSearchHistory,
} from '../searchService';
import type { SearchResponse, SearchHistoryItem } from '../../types/api';

const mockPost = apiClient.post as jest.Mock;
const mockGet = apiClient.get as jest.Mock;
const mockPatch = apiClient.patch as jest.Mock;

beforeEach(() => {
  jest.clearAllMocks();
});

// --- searchVideos ---

describe('searchVideos', () => {
  const query = 'never gonna give you up';
  const mockSearchResponse: SearchResponse = {
    results: [
      {
        videoId: 'dQw4w9WgXcQ',
        title: 'Rick Astley - Never Gonna Give You Up',
        description: 'Classic music video',
        imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg',
        videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
        channelTitle: 'Rick Astley',
      },
    ],
    hasMore: false,
  };

  it('calls /yt/search with query + page 0 by default and returns results with hasMore', async () => {
    mockPost.mockResolvedValueOnce({ data: mockSearchResponse });

    const result = await searchVideos(query);

    expect(mockPost).toHaveBeenCalledWith(
      '/yt/search',
      { query, page: 0 },
      { signal: undefined },
    );
    expect(result).toEqual(mockSearchResponse);
    expect(result.hasMore).toBe(false);
  });

  it('passes page and signal when provided', async () => {
    mockPost.mockResolvedValueOnce({ data: mockSearchResponse });
    const abortController = new AbortController();

    await searchVideos(query, 2, abortController.signal);

    expect(mockPost).toHaveBeenCalledWith(
      '/yt/search',
      { query, page: 2 },
      { signal: abortController.signal },
    );
  });

  it('throws when search fails', async () => {
    const error = new Error('Search failed');
    mockPost.mockRejectedValueOnce(error);

    await expect(searchVideos(query)).rejects.toThrow('Search failed');
  });
});

// --- getSearchHistory ---

describe('getSearchHistory', () => {
  const mockHistoryItems: SearchHistoryItem[] = [
    {
      id: 1,
      searchText: 'never gonna give you up',
      searchedAt: '2026-01-01T00:00:00.000Z',
    },
    {
      id: 2,
      searchText: 'gangnam style',
      searchedAt: '2026-01-02T00:00:00.000Z',
    },
  ];

  it('calls /yt/search/history and returns items', async () => {
    mockGet.mockResolvedValueOnce({ data: { items: mockHistoryItems } });

    const result = await getSearchHistory();

    expect(mockGet).toHaveBeenCalledWith('/yt/search/history', {
      params: { limit: undefined },
    });
    expect(result).toEqual(mockHistoryItems);
  });

  it('passes limit when provided', async () => {
    mockGet.mockResolvedValueOnce({ data: { items: mockHistoryItems } });

    await getSearchHistory(10);

    expect(mockGet).toHaveBeenCalledWith('/yt/search/history', {
      params: { limit: 10 },
    });
  });

  it('throws when fetching history fails', async () => {
    const error = new Error('History fetch failed');
    mockGet.mockRejectedValueOnce(error);

    await expect(getSearchHistory()).rejects.toThrow('History fetch failed');
  });
});

// --- archiveSearchHistory ---

describe('archiveSearchHistory', () => {
  it('calls PATCH /yt/search/history/:id/archive', async () => {
    mockPatch.mockResolvedValueOnce({});

    await archiveSearchHistory(42);

    expect(mockPatch).toHaveBeenCalledWith('/yt/search/history/42/archive');
  });

  it('throws when archiving fails', async () => {
    const error = new Error('Archive failed');
    mockPatch.mockRejectedValueOnce(error);

    await expect(archiveSearchHistory(99)).rejects.toThrow('Archive failed');
  });
});
