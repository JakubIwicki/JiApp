import type { SearchResponse, SearchHistoryItem } from '../../types/api';

type Mode = 'success' | 'empty' | 'error' | 'loading';

let _mode: Mode = 'success';
let _delayMs = 0;

export const setSearchMode = (mode: Mode, delayMs = 0) => {
  _mode = mode;
  _delayMs = delayMs;
};

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
  { id: 1, searchText: 'never gonna give you up', searchedAt: new Date(Date.now() - 3600000).toISOString() },
  { id: 2, searchText: 'gangnam style', searchedAt: new Date(Date.now() - 86400000).toISOString() },
];

export const searchVideos = async (
  _query: string,
  _maxResults?: number,
  _signal?: AbortSignal,
): Promise<SearchResponse> => {
  if (_delayMs) await new Promise(r => setTimeout(r, _delayMs));
  if (_mode === 'loading') await new Promise(() => {});
  if (_mode === 'error') throw new Error('Mock search error');
  if (_mode === 'empty') return { results: [] };
  return { results: fakeResults };
};

export const getSearchHistory = async (
  _limit?: number,
): Promise<SearchHistoryItem[]> => {
  if (_mode === 'error') throw new Error('Mock history error');
  return fakeHistory;
};
