import type { Board, ListBoardsResponse } from '../../types/api';

type Mode = 'success' | 'empty' | 'error';

let _mode: Mode = 'success';
let _boards: Board[] = [];

export const setBoardMode = (mode: Mode) => {
  _mode = mode;
};

export const setBoards = (boards: Board[]) => {
  _boards = boards;
};

export const createBoard = async (name: string): Promise<{ id: number }> => {
  if (_mode === 'error') throw new Error('Mock error');
  const newBoard: Board = {
    id: _boards.length + 1,
    name,
    ownerUserId: 1,
    memberUserIds: [1],
    createdAt: new Date().toISOString(),
    items: [],
  };
  _boards = [..._boards, newBoard];
  return { id: newBoard.id };
};

export const listBoards = async (): Promise<ListBoardsResponse> => {
  if (_mode === 'error') throw new Error('Mock error');
  return { boards: _mode === 'empty' ? [] : _boards, hasMore: false };
};

export const getBoard = async (id: number): Promise<Board> => {
  if (_mode === 'error') throw new Error('Mock error');
  const board = _boards.find(b => b.id === id);
  if (!board) throw new Error('Board not found');
  return board;
};

export const updateBoard = async (
  _id: number,
  _name: string,
): Promise<void> => {};

export const deleteBoard = async (_id: number): Promise<void> => {};

export const addMember = async (
  _boardId: number,
  _userId: number,
): Promise<void> => {};

export const removeMember = async (
  _boardId: number,
  _userId: number,
): Promise<void> => {};
