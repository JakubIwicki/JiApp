import apiClient from './apiClient';
import { SearchResponse, SearchHistoryItem } from '../types/api';

export const archiveSearchHistory = async (id: number): Promise<void> => {
  await apiClient.patch(`/search/history/${id}/archive`);
};

export const searchVideos = async (
  query: string,
  maxResults?: number,
  signal?: AbortSignal,
): Promise<SearchResponse> => {
  const response = await apiClient.post<SearchResponse>(
    '/search',
    { query, maxResults },
    { signal },
  );
  return response.data;
};

interface SearchHistoryResponse {
  items: SearchHistoryItem[];
}

export const getSearchHistory = async (
  limit?: number,
): Promise<SearchHistoryItem[]> => {
  const response = await apiClient.get<SearchHistoryResponse>(
    '/search/history',
    { params: { limit } },
  );
  return response.data.items;
};
