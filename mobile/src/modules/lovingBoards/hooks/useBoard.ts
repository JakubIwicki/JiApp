import { useState, useCallback, useRef, useEffect } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import * as boardService from '../services/boardService';
import * as itemService from '../services/itemService';
import type { Board, Item, BoardItemStatus } from '../types/api';
import type {
  CreateItemPayload,
  UpdateItemPayload,
} from '../services/itemService';

interface UseBoardResult {
  board: Board | null;
  items: Item[];
  isLoading: boolean;
  error: string | null;
  refetch: () => Promise<void>;
  addItem: (payload: CreateItemPayload) => Promise<number | undefined>;
  updateItem: (itemId: number, payload: UpdateItemPayload) => Promise<void>;
  setItemStatus: (itemId: number, status: BoardItemStatus) => Promise<void>;
  deleteItem: (itemId: number) => Promise<void>;
  clearCompleted: () => Promise<void>;
  resetWeekly: () => Promise<void>;
  updateBoard: (name: string) => Promise<void>;
  addMember: (userId: number) => Promise<void>;
  removeMember: (userId: number) => Promise<void>;
}

const useBoard = (boardId: number): UseBoardResult => {
  const [board, setBoard] = useState<Board | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    const controller = abortRef.current;
    return () => {
      controller?.abort();
    };
  }, [abortRef]);

  const loadBoard = useCallback(async () => {
    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    setIsLoading(true);
    setError(null);

    try {
      const data = await boardService.getBoard(boardId);
      if (controller.signal.aborted) return;

      setBoard(data);
    } catch (err) {
      if (err instanceof Error && err.name === 'AbortError') return;
      setError(err instanceof Error ? err.message : 'Failed to load board');
      setBoard(null);
    } finally {
      if (!controller.signal.aborted) {
        setIsLoading(false);
      }
    }
  }, [boardId]);

  useFocusEffect(
    useCallback(() => {
      loadBoard();
    }, [loadBoard]),
  );

  const addItem = useCallback(
    async (payload: CreateItemPayload): Promise<number | undefined> => {
      setError(null);
      try {
        const result = await itemService.createItem(boardId, payload);
        await loadBoard();
        return result.id;
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to create item');
        throw err;
      }
    },
    [boardId, loadBoard],
  );

  const updateItem = useCallback(
    async (itemId: number, payload: UpdateItemPayload) => {
      setError(null);
      try {
        await itemService.updateItem(boardId, itemId, payload);
        await loadBoard();
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to update item');
        throw err;
      }
    },
    [boardId, loadBoard],
  );

  const setItemStatus = useCallback(
    async (itemId: number, status: BoardItemStatus) => {
      setError(null);
      const previous = board;
      setBoard(prev => {
        if (!prev) return prev;
        return {
          ...prev,
          items: prev.items.map(i => (i.id === itemId ? { ...i, status } : i)),
        };
      });
      try {
        await itemService.setItemStatus(boardId, itemId, status);
      } catch (err) {
        setBoard(previous);
        setError(
          err instanceof Error ? err.message : 'Failed to update item status',
        );
        throw err;
      }
    },
    [boardId, board],
  );

  const deleteItem = useCallback(
    async (itemId: number) => {
      setError(null);
      const previous = board;
      setBoard(prev => {
        if (!prev) return prev;
        return {
          ...prev,
          items: prev.items.filter(i => i.id !== itemId),
        };
      });
      try {
        await itemService.deleteItem(boardId, itemId);
      } catch (err) {
        setBoard(previous);
        setError(err instanceof Error ? err.message : 'Failed to delete item');
        throw err;
      }
    },
    [boardId, board],
  );

  const clearCompleted = useCallback(async () => {
    setError(null);
    const previous = board;
    setBoard(prev => {
      if (!prev) return prev;
      return {
        ...prev,
        items: prev.items.filter(i => i.status !== 'Completed'),
      };
    });
    try {
      await itemService.clearCompleted(boardId);
      await loadBoard();
    } catch (err) {
      setBoard(previous);
      setError(
        err instanceof Error ? err.message : 'Failed to clear completed items',
      );
      throw err;
    }
  }, [boardId, board, loadBoard]);

  const resetWeekly = useCallback(async () => {
    setError(null);
    try {
      await itemService.resetWeekly(boardId);
      await loadBoard();
    } catch (err) {
      setError(
        err instanceof Error ? err.message : 'Failed to reset weekly items',
      );
      throw err;
    }
  }, [boardId, loadBoard]);

  const updateBoard = useCallback(
    async (name: string) => {
      setError(null);
      const previous = board;
      setBoard(prev => {
        if (!prev) return prev;
        return { ...prev, name };
      });
      try {
        await boardService.updateBoard(boardId, name);
      } catch (err) {
        setBoard(previous);
        setError(err instanceof Error ? err.message : 'Failed to update board');
        throw err;
      }
    },
    [boardId, board],
  );

  const addMember = useCallback(
    async (userId: number) => {
      setError(null);
      try {
        await boardService.addMember(boardId, userId);
        await loadBoard();
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to add member');
        throw err;
      }
    },
    [boardId, loadBoard],
  );

  const removeMember = useCallback(
    async (userId: number) => {
      setError(null);
      try {
        await boardService.removeMember(boardId, userId);
        await loadBoard();
      } catch (err) {
        setError(
          err instanceof Error ? err.message : 'Failed to remove member',
        );
        throw err;
      }
    },
    [boardId, loadBoard],
  );

  const items = board?.items ?? [];

  return {
    board,
    items,
    isLoading,
    error,
    refetch: loadBoard,
    addItem,
    updateItem,
    setItemStatus,
    deleteItem,
    clearCompleted,
    resetWeekly,
    updateBoard,
    addMember,
    removeMember,
  };
};

export default useBoard;
