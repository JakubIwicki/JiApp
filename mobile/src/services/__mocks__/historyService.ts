import { createMockFn } from '../../test/createMockFn';
import type { HistoryResponse } from '../../types/api';

// ── Default stub data ──────────────────────────────────────────────────────

const fakeHistory: HistoryResponse = {
  searches: [
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
  ],
  downloads: [
    {
      id: 100,
      videoTitle: 'Rick Astley - Never Gonna Give You Up',
      videoDescription: 'Classic music video',
      videoId: 'dQw4w9WgXcQ',
      videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
      imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg',
      downloadedAt: new Date(Date.now() - 7200000).toISOString(),
    },
  ],
};

// ── Internal state ─────────────────────────────────────────────────────────

let _historyResult: HistoryResponse = { ...fakeHistory };
let _historyError: Error | null = null;

// ── Mock functions ─────────────────────────────────────────────────────────

export const getHistory = createMockFn(
  async (_limit?: number, _signal?: AbortSignal): Promise<HistoryResponse> => {
    if (_historyError) throw _historyError;
    return _historyResult;
  },
);

// ── Fluent scenario builders (.withX()) ────────────────────────────────────

export function withHistory(
  overrides?: Partial<HistoryResponse>,
): HistoryResponse {
  _historyError = null;
  _historyResult = {
    searches: overrides?.searches ?? fakeHistory.searches,
    downloads: overrides?.downloads ?? fakeHistory.downloads,
  };
  return _historyResult;
}

export function withEmptyHistory(): HistoryResponse {
  _historyError = null;
  _historyResult = { searches: [], downloads: [] };
  return _historyResult;
}

export function withHistoryFailure(
  error: Error = new Error('Mock history error'),
): Error {
  _historyError = error;
  return error;
}

// ── Reset ──────────────────────────────────────────────────────────────────

export function reset(): void {
  _historyResult = { ...fakeHistory };
  _historyError = null;

  if (typeof jest !== 'undefined') {
    getHistory.mockClear();
  }
}
