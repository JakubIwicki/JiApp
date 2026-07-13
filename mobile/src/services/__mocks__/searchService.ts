import { createMockFn } from '../../test/createMockFn';
import type { SearchResponse, SearchHistoryItem } from '../../types/api';

// ── Default stub data ──────────────────────────────────────────────────────

const fakeResults = [
  {
    videoId: 'dQw4w9WgXcQ',
    title: 'Rick Astley - Never Gonna Give You Up',
    description: "The official video for Rick Astley's classic hit.",
    imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg',
    videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
    channelTitle: 'Rick Astley',
  },
  {
    videoId: '9bZkp7q19f0',
    title: 'PSY - GANGNAM STYLE',
    description: 'The global hit that took over the world.',
    imageUrl: 'https://i.ytimg.com/vi/9bZkp7q19f0/hqdefault.jpg',
    videoUrl: 'https://www.youtube.com/watch?v=9bZkp7q19f0',
    channelTitle: 'officialpsy',
  },
];

const fakeHistory: SearchHistoryItem[] = [
  {
    id: 1,
    searchText: 'never gonna give you up',
    searchedAt: new Date(Date.now() - 3600000).toISOString(),
  },
  {
    id: 2,
    searchText: 'gangnam style',
    searchedAt: new Date(Date.now() - 86400000).toISOString(),
  },
];

// ── Internal state ─────────────────────────────────────────────────────────

let _searchResults: SearchResponse = { results: fakeResults, hasMore: false };
let _searchError: Error | null = null;
let _historyResults: SearchHistoryItem[] = [...fakeHistory];
let _historyError: Error | null = null;
let _archiveError: Error | null = null;

// ── Mock functions ─────────────────────────────────────────────────────────

export const searchVideos = createMockFn(
  async (
    _query: string,
    _page = 0,
    _signal?: AbortSignal,
  ): Promise<SearchResponse> => {
    if (_searchError) throw _searchError;
    return _searchResults;
  },
);

export const getSearchHistory = createMockFn(
  async (_limit?: number): Promise<SearchHistoryItem[]> => {
    if (_historyError) throw _historyError;
    return _historyResults;
  },
);

export const archiveSearchHistory = createMockFn(
  async (_id: number): Promise<void> => {
    if (_archiveError) throw _archiveError;
  },
);

// ── Fluent scenario builders (.withX()) ────────────────────────────────────

export function withSearchResults(
  results?: Partial<SearchResponse>,
): SearchResponse {
  _searchError = null;
  _searchResults = {
    results: results?.results ?? fakeResults,
    hasMore: results?.hasMore ?? false,
  };
  return _searchResults;
}

export function withEmptySearchResults(): SearchResponse {
  _searchError = null;
  _searchResults = { results: [], hasMore: false };
  return _searchResults;
}

export function withSearchFailure(
  error: Error = new Error('Mock search error'),
): Error {
  _searchError = error;
  return error;
}

export function withSearchHistory(
  items?: SearchHistoryItem[],
): SearchHistoryItem[] {
  _historyError = null;
  _historyResults = items ?? [...fakeHistory];
  return _historyResults;
}

export function withEmptySearchHistory(): SearchHistoryItem[] {
  _historyError = null;
  _historyResults = [];
  return [];
}

export function withHistoryFailure(
  error: Error = new Error('Mock history error'),
): Error {
  _historyError = error;
  return error;
}

export function withArchiveSuccess(): void {
  _archiveError = null;
}

export function withArchiveFailure(
  error: Error = new Error('Mock archive error'),
): Error {
  _archiveError = error;
  return error;
}

// ── Reset ──────────────────────────────────────────────────────────────────

export function reset(): void {
  _searchResults = { results: fakeResults, hasMore: false };
  _searchError = null;
  _historyResults = [...fakeHistory];
  _historyError = null;
  _archiveError = null;

  if (typeof jest !== 'undefined') {
    searchVideos.mockClear();
    getSearchHistory.mockClear();
    archiveSearchHistory.mockClear();
  }
}
