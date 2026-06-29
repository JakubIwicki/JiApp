import apiClient from '../../../services/apiClient';
import type { Item } from '../types/api';
import { z } from 'zod';

const IdResponseSchema = z.object({ id: z.number() });
const ClearedResponseSchema = z.object({ cleared: z.number() });
const ResetResponseSchema = z.object({ reset: z.number() });

export interface CreateItemPayload {
  title: string;
  quantity?: string | null;
  category?: string | null;
  note?: string | null;
  assigneeUserId?: number | null;
  expiryDate?: string | null;
  isRecurring?: boolean;
}

export interface UpdateItemPayload {
  title?: string;
  quantity?: string | null;
  category?: string | null;
  note?: string | null;
  assigneeUserId?: number | null;
  expiryDate?: string | null;
  isRecurring?: boolean;
}

export const createItem = async (
  boardId: number,
  payload: CreateItemPayload,
): Promise<{ id: number }> => {
  const response = await apiClient.post(
    `/lovingboards/boards/${boardId}/items`,
    payload,
  );
  return IdResponseSchema.parse(response.data);
};

export const updateItem = async (
  boardId: number,
  itemId: number,
  payload: UpdateItemPayload,
): Promise<void> => {
  await apiClient.put(
    `/lovingboards/boards/${boardId}/items/${itemId}`,
    payload,
  );
};

export const setItemStatus = async (
  boardId: number,
  itemId: number,
  status: string,
): Promise<void> => {
  await apiClient.put(
    `/lovingboards/boards/${boardId}/items/${itemId}/status`,
    { status },
  );
};

export const deleteItem = async (
  boardId: number,
  itemId: number,
): Promise<void> => {
  await apiClient.delete(`/lovingboards/boards/${boardId}/items/${itemId}`);
};

export const clearCompleted = async (
  boardId: number,
): Promise<{ cleared: number }> => {
  const response = await apiClient.post(
    `/lovingboards/boards/${boardId}/items/clear-completed`,
  );
  return ClearedResponseSchema.parse(response.data);
};

export const resetWeekly = async (
  boardId: number,
): Promise<{ reset: number }> => {
  const response = await apiClient.post(
    `/lovingboards/boards/${boardId}/items/reset-weekly`,
  );
  return ResetResponseSchema.parse(response.data);
};
