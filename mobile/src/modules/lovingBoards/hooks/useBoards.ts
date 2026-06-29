import { useState, useCallback, useRef, useEffect } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import * as boardService from '../services/boardService';
import type { Board } from '../types/api';

interface UseBoardsResult {
  boards: Board[];
  hasMore: boolean;
  isLoading: boolean;
  error: string | null;
  refetch: () => Promise<void>;
  createBoard: (name: string) => Promise<number | undefined>;
}

const useBoards = (): UseBoardsResult => {
  const [boards, setBoards] = useState<Board[]>([]);
  const [hasMore, setHasMore] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    return () => {
      abortRef.current?.abort();
    };
  }, []);

  const loadBoards = useCallback(async () => {
    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    setIsLoading(true);
    setError(null);

    try {
      const data = await boardService.listBoards();
      if (controller.signal.aborted) return;

      setBoards(data.boards);
      setHasMore(data.hasMore);
    } catch (err) {
      if (err instanceof Error && err.name === 'AbortError') return;
      setError('lovingBoards.errors.loadBoards');
      setBoards([]);
    } finally {
      if (!controller.signal.aborted) {
        setIsLoading(false);
      }
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      loadBoards();
    }, [loadBoards]),
  );

  const createBoard = useCallback(
    async (name: string): Promise<number | undefined> => {
      setError(null);
      try {
        const result = await boardService.createBoard(name);
        await loadBoards();
        return result.id;
      } catch (err) {
        setError('lovingBoards.errors.createBoard');
        throw err;
      }
    },
    [loadBoards],
  );

  return {
    boards,
    hasMore,
    isLoading,
    error,
    refetch: loadBoards,
    createBoard,
  };
};

export default useBoards;
