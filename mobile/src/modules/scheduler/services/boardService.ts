import { z } from 'zod';
import apiClient from '../../../services/apiClient';
import { BoardSchema } from '../types/api';
import type { Board } from '../types/api';

const IdResponseSchema = z.object({ id: z.number() });
type IdResponse = z.infer<typeof IdResponseSchema>;

const ListBoardsResponseSchema = z.object({ boards: z.array(BoardSchema) });

export const listBoards = async (): Promise<Board[]> => {
  const response = await apiClient.get('/scheduler/boards');
  return ListBoardsResponseSchema.parse(response.data).boards;
};

export const createBoard = async (name: string): Promise<IdResponse> => {
  const response = await apiClient.post('/scheduler/boards', { name });
  return IdResponseSchema.parse(response.data);
};

export const deleteBoard = async (id: number): Promise<void> => {
  await apiClient.delete(`/scheduler/boards/${id}`);
};

export const addBoardMember = async (
  boardId: number,
  userId: number,
): Promise<void> => {
  await apiClient.post(`/scheduler/boards/${boardId}/members`, { userId });
};

export const removeBoardMember = async (
  boardId: number,
  userId: number,
): Promise<void> => {
  await apiClient.delete(`/scheduler/boards/${boardId}/members/${userId}`);
};
