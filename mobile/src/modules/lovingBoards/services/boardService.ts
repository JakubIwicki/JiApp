import apiClient from '../../../services/apiClient';
import { ListBoardsResponseSchema, BoardSchema } from '../types/api';
import type { ListBoardsResponse, Board } from '../types/api';

interface IdResponse {
  id: number;
}

export const listBoards = async (): Promise<ListBoardsResponse> => {
  const response = await apiClient.get('/lovingboards/boards');
  return ListBoardsResponseSchema.parse(response.data);
};

export const getBoard = async (id: number): Promise<Board> => {
  const response = await apiClient.get(`/lovingboards/boards/${id}`);
  return BoardSchema.parse(response.data);
};

export const createBoard = async (name: string): Promise<IdResponse> => {
  const response = await apiClient.post<IdResponse>('/lovingboards/boards', {
    name,
  });
  return response.data;
};

export const updateBoard = async (id: number, name: string): Promise<void> => {
  await apiClient.put(`/lovingboards/boards/${id}`, { name });
};

export const deleteBoard = async (id: number): Promise<void> => {
  await apiClient.delete(`/lovingboards/boards/${id}`);
};

export const addMember = async (
  boardId: number,
  userId: number,
): Promise<void> => {
  await apiClient.post(`/lovingboards/boards/${boardId}/members`, { userId });
};

export const removeMember = async (
  boardId: number,
  userId: number,
): Promise<void> => {
  await apiClient.delete(`/lovingboards/boards/${boardId}/members/${userId}`);
};
