import { useState, useCallback, useRef, useEffect, useMemo } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import * as boardService from '../services/boardService';
import * as itemService from '../services/itemService';
import {
  openBoardStream,
  type BoardStreamHandle,
} from '../services/boardStreamService';
import type { Board, Item, BoardItemStatus } from '../types/api';
import type { BoardStreamEvent } from '../types/events';
import type {
  CreateItemPayload,
  UpdateItemPayload,
} from '../services/itemService';
import useItemReminders from './useItemReminders';

interface UseBoardResult {
  board: Board | null;
  items: Item[];
  isLoading: boolean;
  error: string | null;
  presence: number[];
  isLive: boolean;
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

export const DEBOUNCE_MS = 300;

const useBoard = (boardId: number): UseBoardResult => {
  const [board, setBoard] = useState<Board | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [presence, setPresence] = useState<number[]>([]);
  const [isLive, setIsLive] = useState(false);
  const abortRef = useRef<AbortController | null>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const inFlightRef = useRef<Promise<void> | null>(null);
  const inFlightStartedAtRef = useRef(0);
  const lastLandStartedAtRef = useRef(0);
  const lastLandRef = useRef(0);
  const pendingEchoRefetchRef = useRef<{
    readonly promise: Promise<void>;
    readonly resolve: () => void;
    readonly requestedAt: number;
  } | null>(null);
  const streamRef = useRef<BoardStreamHandle | null>(null);
  const boardRef = useRef(board);
  boardRef.current = board;

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
      setError('lovingBoards.errors.loadBoard');
      setBoard(null);
    } finally {
      if (!controller.signal.aborted) {
        setIsLoading(false);
      }
    }
  }, [boardId]);

  // Run a GET and track it: its start time, joinability while in flight, and a
  // landing that opens a post-fetch grace window for redundant echo refetches.
  const runFetch = useCallback((): Promise<void> => {
    const startedAt = Date.now();
    const promise = loadBoard();
    inFlightRef.current = promise;
    inFlightStartedAtRef.current = startedAt;
    promise.then(
      () => {
        if (inFlightRef.current === promise) inFlightRef.current = null;
        lastLandStartedAtRef.current = startedAt;
        lastLandRef.current = Date.now();
      },
      () => {
        if (inFlightRef.current === promise) inFlightRef.current = null;
        lastLandStartedAtRef.current = startedAt;
        lastLandRef.current = Date.now();
      },
    );
    return promise;
  }, [loadBoard]);

  // Cancel a scheduled-but-not-yet-fetched echo refetch, resolving any
  // awaiters so nothing hangs when the screen loses focus before it fires
  const cancelPendingRefetch = useCallback((): void => {
    if (debounceRef.current) {
      clearTimeout(debounceRef.current);
      debounceRef.current = null;
    }
    const pending = pendingEchoRefetchRef.current;
    pendingEchoRefetchRef.current = null;
    pending?.resolve();
  }, []);

  // Core decision for a refetch request made at `requestedAt`: join an
  // in-flight GET only when it started after the request (so it is guaranteed
  // to observe the change); if it started earlier, chain a fresh fetch after
  // it lands instead of resolving with pre-change data; otherwise honor the
  // post-fetch grace window — but only when the landed GET started after the
  // request too. Returns null when a fresh fetch is still required.
  const resolveRefetchNow = useCallback(
    (skipGrace: boolean, requestedAt: number): Promise<void> | null => {
      if (inFlightRef.current) {
        if (inFlightStartedAtRef.current >= requestedAt) {
          return inFlightRef.current;
        }
        return inFlightRef.current.then(() => runFetch());
      }
      if (
        !skipGrace &&
        Date.now() - lastLandRef.current < DEBOUNCE_MS &&
        lastLandStartedAtRef.current >= requestedAt
      ) {
        return Promise.resolve();
      }
      return null;
    },
    [runFetch],
  );

  // Echo path: leading-edge debounce — the first request arms the window and
  // later echoes do NOT extend it, so a trickle of echoes can't starve it.
  const flushEchoDebounce = useCallback((): void => {
    debounceRef.current = null;
    const pending = pendingEchoRefetchRef.current;
    pendingEchoRefetchRef.current = null;
    if (!pending) return;
    const promise = resolveRefetchNow(false, pending.requestedAt) ?? runFetch();
    promise.then(
      () => pending.resolve(),
      () => pending.resolve(),
    );
  }, [resolveRefetchNow, runFetch]);

  const scheduleEchoRefetch = useCallback((): Promise<void> => {
    const requestedAt = Date.now();
    const immediate = resolveRefetchNow(false, requestedAt);
    if (immediate) return immediate;
    if (pendingEchoRefetchRef.current) {
      return pendingEchoRefetchRef.current.promise;
    }

    let resolve!: () => void;
    const promise = new Promise<void>(r => {
      resolve = r;
    });
    pendingEchoRefetchRef.current = { promise, resolve, requestedAt };
    debounceRef.current = setTimeout(flushEchoDebounce, DEBOUNCE_MS);
    return promise;
  }, [resolveRefetchNow, flushEchoDebounce]);

  // Action path: fire the GET immediately (no debounce), coalescing with any
  // in-flight GET that already covers the write, so the write keeps its
  // pre-change latency while its echo still collapses into one request.
  const scheduleRefetch = useCallback((): Promise<void> => {
    return resolveRefetchNow(true, Date.now()) ?? runFetch();
  }, [resolveRefetchNow, runFetch]);

  useEffect(() => {
    return () => {
      abortRef.current?.abort();
      cancelPendingRefetch();
    };
  }, [cancelPendingRefetch]);

  const handleStreamEvent = useCallback(
    (event: BoardStreamEvent): void => {
      switch (event.type) {
        case 'item.status':
          setBoard(prev => {
            if (!prev) return prev;
            return {
              ...prev,
              items: prev.items.map(i =>
                i.id === event.itemId ? { ...i, status: event.status } : i,
              ),
            };
          });
          return;
        case 'item.removed':
          setBoard(prev => {
            if (!prev) return prev;
            return {
              ...prev,
              items: prev.items.filter(i => i.id !== event.itemId),
            };
          });
          return;
        case 'items.cleared':
          setBoard(prev => {
            if (!prev) return prev;
            const removedIds = new Set(event.itemIds);
            return {
              ...prev,
              items: prev.items.filter(i => !removedIds.has(i.id)),
            };
          });
          return;
        default:
          // item.added / item.updated / board.updated / member.changed /
          // recurring.reset / board.deleted need fresh server data
          scheduleEchoRefetch();
      }
    },
    [scheduleEchoRefetch],
  );

  useFocusEffect(
    useCallback(() => {
      runFetch();

      const handle = openBoardStream({
        boardId,
        onEvent: handleStreamEvent,
        onPresence: (userIds: number[]) => {
          setPresence(userIds);
        },
        onOpen: () => {
          runFetch();
          setIsLive(true);
        },
        onError: () => {
          if (!boardRef.current) {
            setError('lovingBoards.errors.stream');
          }
          setIsLive(false);
        },
      });
      streamRef.current = handle;

      return () => {
        handle.close();
        setPresence([]);
        setIsLive(false);
        cancelPendingRefetch();
      };
    }, [boardId, runFetch, handleStreamEvent, cancelPendingRefetch]),
  );

  const addItem = useCallback(
    async (payload: CreateItemPayload): Promise<number | undefined> => {
      setError(null);
      try {
        const result = await itemService.createItem(boardId, payload);
        await scheduleRefetch();
        return result.id;
      } catch (err) {
        setError('lovingBoards.errors.createItem');
        throw err;
      }
    },
    [boardId, scheduleRefetch],
  );

  const updateItem = useCallback(
    async (itemId: number, payload: UpdateItemPayload) => {
      setError(null);
      try {
        await itemService.updateItem(boardId, itemId, payload);
        await scheduleRefetch();
      } catch (err) {
        setError('lovingBoards.errors.updateItem');
        throw err;
      }
    },
    [boardId, scheduleRefetch],
  );

  const setItemStatus = useCallback(
    async (itemId: number, status: BoardItemStatus) => {
      setError(null);
      const priorStatus = board?.items.find(i => i.id === itemId)?.status;
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
        if (priorStatus) {
          setBoard(prev =>
            prev
              ? {
                  ...prev,
                  items: prev.items.map(i =>
                    i.id === itemId ? { ...i, status: priorStatus } : i,
                  ),
                }
              : prev,
          );
        }
        setError('lovingBoards.errors.itemStatus');
        throw err;
      }
    },
    [boardId, board],
  );

  const deleteItem = useCallback(
    async (itemId: number) => {
      setError(null);
      const removedItem = board?.items.find(i => i.id === itemId);
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
        if (removedItem) {
          setBoard(prev =>
            prev ? { ...prev, items: [...prev.items, removedItem] } : prev,
          );
        }
        setError('lovingBoards.errors.deleteItem');
        throw err;
      }
    },
    [boardId, board],
  );

  const clearCompleted = useCallback(async () => {
    setError(null);
    setBoard(prev => {
      if (!prev) return prev;
      return {
        ...prev,
        items: prev.items.filter(i => i.status !== 'Completed'),
      };
    });
    try {
      await itemService.clearCompleted(boardId);
      await scheduleRefetch();
    } catch (err) {
      await scheduleRefetch();
      setError('lovingBoards.errors.clearCompleted');
      throw err;
    }
  }, [boardId, scheduleRefetch]);

  const resetWeekly = useCallback(async () => {
    setError(null);
    try {
      await itemService.resetWeekly(boardId);
      await scheduleRefetch();
    } catch (err) {
      setError('lovingBoards.errors.resetWeekly');
      throw err;
    }
  }, [boardId, scheduleRefetch]);

  const updateBoard = useCallback(
    async (name: string) => {
      setError(null);
      const priorName = board?.name;
      setBoard(prev => {
        if (!prev) return prev;
        return { ...prev, name };
      });
      try {
        await boardService.updateBoard(boardId, name);
      } catch (err) {
        if (priorName) {
          setBoard(prev => (prev ? { ...prev, name: priorName } : prev));
        }
        setError('lovingBoards.errors.updateBoard');
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
        await scheduleRefetch();
      } catch (err) {
        setError('lovingBoards.errors.addMember');
        throw err;
      }
    },
    [boardId, scheduleRefetch],
  );

  const removeMember = useCallback(
    async (userId: number) => {
      setError(null);
      try {
        await boardService.removeMember(boardId, userId);
        await scheduleRefetch();
      } catch (err) {
        setError('lovingBoards.errors.removeMember');
        throw err;
      }
    },
    [boardId, scheduleRefetch],
  );

  const items = useMemo(() => board?.items ?? [], [board]);

  const itemsForReminders = useMemo(
    () => items.filter(i => i.expiryDate !== null),
    [items],
  );
  useItemReminders(itemsForReminders, board?.name);

  return {
    board,
    items,
    isLoading,
    error,
    presence,
    isLive,
    refetch: runFetch,
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
