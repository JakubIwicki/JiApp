import { createMockFn } from '../../../../test/createMockFn';
import type { Board, ListBoardsResponse } from '../../types/api';

// ── Default stub data ──────────────────────────────────────────────────────

const defaultBoards: Board[] = [];

// ── Internal state ─────────────────────────────────────────────────────────

let _boards: Board[] = defaultBoards;
let _boardError: Error | null = null;

// ── Mock functions ─────────────────────────────────────────────────────────

export const createBoard = createMockFn(
  async (name: string): Promise<{ id: number }> => {
    if (_boardError) throw _boardError;
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
  },
);

export const listBoards = createMockFn(
  async (): Promise<ListBoardsResponse> => {
    if (_boardError) throw _boardError;
    return { boards: _boards, hasMore: false };
  },
);

export const getBoard = createMockFn(async (id: number): Promise<Board> => {
  if (_boardError) throw _boardError;
  const board = _boards.find(b => b.id === id);
  if (!board) throw new Error('Board not found');
  return board;
});

export const updateBoard = createMockFn(
  async (_id: number, _name: string): Promise<void> => {},
);

export const deleteBoard = createMockFn(
  async (_id: number): Promise<void> => {},
);

export const addMember = createMockFn(
  async (_boardId: number, _userId: number): Promise<void> => {},
);

export const removeMember = createMockFn(
  async (_boardId: number, _userId: number): Promise<void> => {},
);

// ── Fluent scenario builders (.withX()) ────────────────────────────────────

export function withBoards(boards: Board[] = defaultBoards): Board[] {
  _boardError = null;
  _boards = boards;
  return _boards;
}

export function withBoardError(error: Error = new Error('Mock error')): Error {
  _boardError = error;
  return error;
}

// ── Reset ──────────────────────────────────────────────────────────────────

export function reset(): void {
  _boards = defaultBoards;
  _boardError = null;

  if (typeof jest !== 'undefined') {
    createBoard.mockClear();
    listBoards.mockClear();
    getBoard.mockClear();
    updateBoard.mockClear();
    deleteBoard.mockClear();
    addMember.mockClear();
    removeMember.mockClear();
  }
}
