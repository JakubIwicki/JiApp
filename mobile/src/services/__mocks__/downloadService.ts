import { createMockFn } from '../../test/createMockFn';
import type {
  DownloadRequest,
  DownloadResponse,
  DownloadHistoryItem,
} from '../../types/api';

// ── Default stub data ──────────────────────────────────────────────────────

const fakeHistory: DownloadHistoryItem[] = [
  {
    id: 100,
    videoTitle: 'Rick Astley - Never Gonna Give You Up',
    videoDescription: 'Downloaded from mock service',
    videoId: 'dQw4w9WgXcQ',
    videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
    imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg',
    downloadedAt: new Date(Date.now() - 3600000).toISOString(),
  },
];

// ── Internal state ─────────────────────────────────────────────────────────

let _downloadResponse: DownloadResponse = {
  downloadUrl: 'https://example.com/downloads/mock-file.mp3',
};
let _downloadError: Error | null = null;
let _historyResults: DownloadHistoryItem[] = [...fakeHistory];
let _historyError: Error | null = null;
let _archiveError: Error | null = null;
let _fileDownloadPath: string = '/storage/emulated/0/Download/mock-file.mp3';
let _fileDownloadError: Error | null = null;

// ── Mock functions ─────────────────────────────────────────────────────────

export const requestDownloadLink = createMockFn(
  async (
    _request: DownloadRequest,
    _signal?: AbortSignal,
  ): Promise<DownloadResponse> => {
    if (_downloadError) throw _downloadError;
    return _downloadResponse;
  },
);

export const getDownloadHistory = createMockFn(
  async (_limit?: number): Promise<DownloadHistoryItem[]> => {
    if (_historyError) throw _historyError;
    return _historyResults;
  },
);

export const archiveDownload = createMockFn(
  async (_id: number): Promise<void> => {
    if (_archiveError) throw _archiveError;
  },
);

export const downloadFile = createMockFn(
  async (_downloadUrl: string, fileName: string): Promise<string> => {
    if (_fileDownloadError) throw _fileDownloadError;
    return `/storage/emulated/0/Download/${fileName}.mp3`;
  },
);

// ── Fluent scenario builders (.withX()) ────────────────────────────────────

export function withDownloadLinkSuccess(
  overrides?: Partial<DownloadResponse>,
): DownloadResponse {
  _downloadError = null;
  _downloadResponse = { ..._downloadResponse, ...overrides };
  return _downloadResponse;
}

export function withDownloadLinkFailure(
  error: Error = new Error('Mock download error'),
): Error {
  _downloadError = error;
  return error;
}

export function withDownloadHistory(
  items?: DownloadHistoryItem[],
): DownloadHistoryItem[] {
  _historyError = null;
  _historyResults = items ?? [...fakeHistory];
  return _historyResults;
}

export function withEmptyDownloadHistory(): DownloadHistoryItem[] {
  _historyError = null;
  _historyResults = [];
  return [];
}

export function withDownloadHistoryFailure(
  error: Error = new Error('Mock history error'),
): Error {
  _historyError = error;
  return error;
}

export function withFileDownloadSuccess(path?: string): string {
  _fileDownloadError = null;
  _fileDownloadPath = path ?? '/storage/emulated/0/Download/mock-file.mp3';
  return _fileDownloadPath;
}

export function withFileDownloadFailure(
  error: Error = new Error('Mock file download error'),
): Error {
  _fileDownloadError = error;
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
  _downloadResponse = {
    downloadUrl: 'https://example.com/downloads/mock-file.mp3',
  };
  _downloadError = null;
  _historyResults = [...fakeHistory];
  _historyError = null;
  _archiveError = null;
  _fileDownloadPath = '/storage/emulated/0/Download/mock-file.mp3';
  _fileDownloadError = null;

  if (typeof jest !== 'undefined') {
    requestDownloadLink.mockClear();
    getDownloadHistory.mockClear();
    archiveDownload.mockClear();
    downloadFile.mockClear();
  }
}
