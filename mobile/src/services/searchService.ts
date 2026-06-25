import apiClient from './apiClient';
import { SearchResponse, SearchHistoryItem } from '../types/api';
import {
  SearchResponseSchema,
  SearchHistoryResponseSchema,
} from '../types/schemas';

export const archiveSearchHistory = async (id: number): Promise<void> => {
  await apiClient.patch(`/yt/search/history/${id}/archive`);
};

export const searchVideos = async (
  query: string,
  page = 0,
  signal?: AbortSignal,
): Promise<SearchResponse> => {
  const response = await apiClient.post<SearchResponse>(
    '/yt/search',
    { query, page },
    { signal },
  );
  return SearchResponseSchema.parse(response.data);
};

export const getSearchHistory = async (
  limit?: number,
): Promise<SearchHistoryItem[]> => {
  const response = await apiClient.get<{ items: SearchHistoryItem[] }>(
    '/yt/search/history',
    { params: { limit } },
  );
  return SearchHistoryResponseSchema.parse(response.data).items;
};
