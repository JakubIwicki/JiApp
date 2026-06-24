import apiClient from './apiClient';
import type { HistoryResponse } from '../types/api';
import { HistoryResponseSchema } from '../types/schemas';

export const getHistory = async (
  limit?: number,
  signal?: AbortSignal,
): Promise<HistoryResponse> => {
  const response = await apiClient.get<HistoryResponse>('/yt/history', {
    params: { limit },
    signal,
  });
  return HistoryResponseSchema.parse(response.data);
};
