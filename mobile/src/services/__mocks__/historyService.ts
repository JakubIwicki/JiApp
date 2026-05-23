import type { HistoryResponse } from '../../types/api';

type Mode = 'success' | 'empty' | 'error' | 'loading';

let _mode: Mode = 'success';
let _delayMs = 0;

export const setHistoryMode = (mode: Mode, delayMs = 0) => {
  _mode = mode;
  _delayMs = delayMs;
};

const fakeData: HistoryResponse = {
  searches: [
    { id: 1, searchText: 'never gonna give you up', searchedAt: new Date(Date.now() - 3600000).toISOString() },
    { id: 2, searchText: 'gangnam style', searchedAt: new Date(Date.now() - 86400000).toISOString() },
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

export const getHistory = async (
  _limit?: number,
  _signal?: AbortSignal,
): Promise<HistoryResponse> => {
  if (_delayMs) await new Promise(r => setTimeout(r, _delayMs));
  if (_mode === 'loading') await new Promise(() => {});
  if (_mode === 'error') throw new Error('Mock history error');
  if (_mode === 'empty') return { searches: [], downloads: [] };
  return fakeData;
};
