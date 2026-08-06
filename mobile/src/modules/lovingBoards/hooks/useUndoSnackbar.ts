import { useCallback, useEffect, useRef, useState } from 'react';
import type { BoardItemStatus } from '../types/api';

interface UndoState {
  itemId: number;
  previousStatus: BoardItemStatus;
}

const UNDO_DURATION_MS = 5000;
const CLEARED_DURATION_MS = 4000;

export interface UseUndoSnackbarResult {
  undoState: UndoState | null;
  clearedMessage: string | null;
  showUndo: (itemId: number, previousStatus: BoardItemStatus) => void;
  armUndoTimeout: () => void;
  clearUndo: () => void;
  showCleared: (message: string) => void;
  dismissCleared: () => void;
}

const useUndoSnackbar = (): UseUndoSnackbarResult => {
  const [undoState, setUndoState] = useState<UndoState | null>(null);
  const undoTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [clearedMessage, setClearedMessage] = useState<string | null>(null);
  const clearTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Read the refs at unmount time — a mount-time capture is always null, which
  // leaked timers armed at runtime and caused setState-after-unmount warnings.
  useEffect(() => {
    return () => {
      if (undoTimerRef.current) clearTimeout(undoTimerRef.current);
      if (clearTimerRef.current) clearTimeout(clearTimerRef.current);
    };
  }, []);

  const showUndo = useCallback(
    (itemId: number, previousStatus: BoardItemStatus) => {
      setUndoState({ itemId, previousStatus });
    },
    [],
  );

  const armUndoTimeout = useCallback(() => {
    if (undoTimerRef.current) clearTimeout(undoTimerRef.current);
    undoTimerRef.current = setTimeout(() => {
      setUndoState(null);
      undoTimerRef.current = null;
    }, UNDO_DURATION_MS);
  }, []);

  const clearUndo = useCallback(() => {
    setUndoState(null);
    if (undoTimerRef.current) {
      clearTimeout(undoTimerRef.current);
      undoTimerRef.current = null;
    }
  }, []);

  const showCleared = useCallback((message: string) => {
    setClearedMessage(message);
    if (clearTimerRef.current) clearTimeout(clearTimerRef.current);
    clearTimerRef.current = setTimeout(() => {
      setClearedMessage(null);
      clearTimerRef.current = null;
    }, CLEARED_DURATION_MS);
  }, []);

  const dismissCleared = useCallback(() => {
    setClearedMessage(null);
    if (clearTimerRef.current) {
      clearTimeout(clearTimerRef.current);
      clearTimerRef.current = null;
    }
  }, []);

  return {
    undoState,
    clearedMessage,
    showUndo,
    armUndoTimeout,
    clearUndo,
    showCleared,
    dismissCleared,
  };
};

export default useUndoSnackbar;
export type { UndoState };
