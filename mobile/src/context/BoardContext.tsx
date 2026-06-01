import React, {
  createContext,
  useCallback,
  useEffect,
  useMemo,
  useReducer,
} from 'react';
import * as boardService from '../modules/scheduler/services/boardService';
import * as storageService from '../services/storageService';
import type { Board } from '../modules/scheduler/types/api';

// ─── State ──────────────────────────────────────────────────────────────────────

interface BoardState {
  boards: Board[];
  selectedBoardId: number | null;
  isLoading: boolean;
  error: string | null;
}

type BoardAction =
  | { type: 'SET_LOADING'; isLoading: boolean }
  | { type: 'SET_ERROR'; error: string }
  | { type: 'SET_BOARDS'; boards: Board[]; selectedBoardId: number | null }
  | { type: 'SET_SELECTED_BOARD'; selectedBoardId: number | null }
  | { type: 'ADD_BOARD'; board: Board }
  | { type: 'REMOVE_BOARD'; boardId: number };

// ─── Context Value ──────────────────────────────────────────────────────────────

interface BoardContextValue extends BoardState {
  switchBoard: (id: number) => Promise<void>;
  loadBoards: () => Promise<void>;
  createBoard: (name: string) => Promise<Board>;
  deleteBoard: (id: number) => Promise<void>;
  addMember: (boardId: number, userId: number) => Promise<void>;
  removeMember: (boardId: number, userId: number) => Promise<void>;
}

// ─── Reducer ────────────────────────────────────────────────────────────────────

const initialState: BoardState = {
  boards: [],
  selectedBoardId: null,
  isLoading: true,
  error: null,
};

function boardReducer(state: BoardState, action: BoardAction): BoardState {
  switch (action.type) {
    case 'SET_LOADING':
      return { ...state, isLoading: action.isLoading };
    case 'SET_ERROR':
      return { ...state, error: action.error, isLoading: false };
    case 'SET_BOARDS':
      return {
        ...state,
        boards: action.boards,
        selectedBoardId: action.selectedBoardId,
        isLoading: false,
        error: null,
      };
    case 'SET_SELECTED_BOARD':
      return { ...state, selectedBoardId: action.selectedBoardId };
    case 'ADD_BOARD':
      return { ...state, boards: [...state.boards, action.board] };
    case 'REMOVE_BOARD':
      return {
        ...state,
        boards: state.boards.filter((b) => b.id !== action.boardId),
      };
    default:
      return state;
  }
}

// ─── Context ────────────────────────────────────────────────────────────────────

export const BoardContext = createContext<BoardContextValue>({
  boards: [],
  selectedBoardId: null,
  isLoading: true,
  error: null,
  switchBoard: async () => {},
  loadBoards: async () => {},
  createBoard: async () => ({ id: 0, name: '', memberUserIds: [], createdAt: '' }),
  deleteBoard: async () => {},
  addMember: async () => {},
  removeMember: async () => {},
});

// ─── Provider ──────────────────────────────────────────────────────────────────

const BoardProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [state, dispatch] = useReducer(boardReducer, initialState);

  const loadBoards = useCallback(async () => {
    dispatch({ type: 'SET_LOADING', isLoading: true });
    try {
      const boards = await boardService.listBoards();

      // Restore persisted board selection
      const persistedId = await storageService.getSelectedBoardId();
      let selectedBoardId: number | null = null;

      if (persistedId !== null && boards.some((b) => b.id === persistedId)) {
        selectedBoardId = persistedId;
      } else if (boards.length > 0) {
        selectedBoardId = boards[0].id;
      }

      dispatch({ type: 'SET_BOARDS', boards, selectedBoardId });
    } catch (e) {
      const message =
        e instanceof Error ? e.message : 'Failed to load boards';
      dispatch({ type: 'SET_ERROR', error: message });
    }
  }, []);

  useEffect(() => {
    loadBoards();
  }, [loadBoards]);

  const switchBoard = useCallback(async (id: number) => {
    dispatch({ type: 'SET_SELECTED_BOARD', selectedBoardId: id });
    await storageService.saveSelectedBoardId(id);
  }, []);

  const createBoard = useCallback(
    async (name: string): Promise<Board> => {
      const [result, boards] = await Promise.all([
        boardService.createBoard(name),
        boardService.listBoards(),
      ]);
      const newBoard = boards.find((b) => b.id === result.id);
      if (!newBoard) {
        throw new Error('Board created but not found in list');
      }
      dispatch({ type: 'ADD_BOARD', board: newBoard });
      await switchBoard(newBoard.id);
      return newBoard;
    },
    [switchBoard],
  );

  const deleteBoard = useCallback(
    async (id: number) => {
      await boardService.deleteBoard(id);
      dispatch({ type: 'REMOVE_BOARD', boardId: id });

      // If the deleted board was selected, switch to the first remaining board
      if (state.selectedBoardId === id) {
        const remaining = state.boards.filter((b) => b.id !== id);
        if (remaining.length > 0) {
          await switchBoard(remaining[0].id);
        } else {
          dispatch({ type: 'SET_SELECTED_BOARD', selectedBoardId: null });
          await storageService.clearSelectedBoardId();
        }
      }
    },
    [state.selectedBoardId, state.boards, switchBoard],
  );

  const addMember = useCallback(async (boardId: number, userId: number) => {
    await boardService.addBoardMember(boardId, userId);
    // Reload to get updated member list
    const boards = await boardService.listBoards();
    dispatch({ type: 'SET_BOARDS', boards, selectedBoardId: state.selectedBoardId });
  }, [state.selectedBoardId]);

  const removeMember = useCallback(
    async (boardId: number, userId: number) => {
      await boardService.removeBoardMember(boardId, userId);
      const boards = await boardService.listBoards();
      dispatch({ type: 'SET_BOARDS', boards, selectedBoardId: state.selectedBoardId });
    },
    [state.selectedBoardId],
  );

  const value = useMemo(
    () => ({
      boards: state.boards,
      selectedBoardId: state.selectedBoardId,
      isLoading: state.isLoading,
      error: state.error,
      switchBoard,
      loadBoards,
      createBoard,
      deleteBoard,
      addMember,
      removeMember,
    }),
    [
      state.boards,
      state.selectedBoardId,
      state.isLoading,
      state.error,
      switchBoard,
      loadBoards,
      createBoard,
      deleteBoard,
      addMember,
      removeMember,
    ],
  );

  return (
    <BoardContext.Provider value={value}>{children}</BoardContext.Provider>
  );
};
