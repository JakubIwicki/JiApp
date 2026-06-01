import apiClient from '../../../services/apiClient';
import type { Board } from '../types/api';

interface IdResponse {
  id: number;
}

interface ListBoardsResponse {
  boards: Board[];
}

export const listBoards = async (): Promise<Board[]> => {
  const response = await apiClient.get<ListBoardsResponse>('/scheduler/boards');
  return response.data.boards;
};

export const createBoard = async (name: string): Promise<IdResponse> => {
  const response = await apiClient.post<IdResponse>('/scheduler/boards', { name });
  return response.data;
};

export const deleteBoard = async (id: number): Promise<void> => {
  await apiClient.delete(`/scheduler/boards/${id}`);
};

export const addBoardMember = async (boardId: number, userId: number): Promise<void> => {
  await apiClient.post(`/scheduler/boards/${boardId}/members`, { userId });
};

export const removeBoardMember = async (boardId: number, userId: number): Promise<void> => {
  await apiClient.delete(`/scheduler/boards/${boardId}/members/${userId}`);
};
