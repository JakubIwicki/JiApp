import apiClient from '../../../services/apiClient';
import { SearchResponse, SearchHistoryItem } from '../types/api';

export const archiveSearchHistory = async (id: number): Promise<void> => {
  await apiClient.patch(`/yt/search/history/${id}/archive`);
};

export const searchVideos = async (
  query: string,
  maxResults?: number,
  signal?: AbortSignal,
  pageToken?: string | null,
): Promise<SearchResponse> => {
  const response = await apiClient.post<SearchResponse>(
    '/yt/search',
    { query, maxResults, pageToken: pageToken ?? null },
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
    '/yt/search/history',
    { params: { limit } },
  );
  return response.data.items;
};
