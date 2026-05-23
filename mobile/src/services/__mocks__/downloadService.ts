import type { DownloadRequest, DownloadResponse, DownloadHistoryItem } from '../../types/api';

type Mode = 'success' | 'error' | 'loading';

let _mode: Mode = 'success';
let _delayMs = 0;

export const setDownloadMode = (mode: Mode, delayMs = 0) => {
  _mode = mode;
  _delayMs = delayMs;
};

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

export const requestDownloadLink = async (
  _request: DownloadRequest,
  _signal?: AbortSignal,
): Promise<DownloadResponse> => {
  if (_delayMs) await new Promise(r => setTimeout(r, _delayMs));
  if (_mode === 'loading') await new Promise(() => {});
  if (_mode === 'error') throw new Error('Mock download error');
  return { downloadUrl: 'https://example.com/downloads/mock-file.mp3' };
};

export const getDownloadHistory = async (
  _limit?: number,
): Promise<DownloadHistoryItem[]> => fakeHistory;

export const downloadFile = async (
  _downloadUrl: string,
  fileName: string,
): Promise<string> => `/storage/emulated/0/Download/${fileName}.mp3`;
