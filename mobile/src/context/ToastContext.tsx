import React, { createContext, useCallback, useEffect, useMemo, useReducer, useRef } from 'react';

export type ToastType = 'success' | 'error' | 'info' | 'warning';

export interface Toast {
  id: string;
  type: ToastType;
  titleKey: string;
  descKey?: string;
  persistent: boolean;
}

interface ToastState {
  queue: Toast[];
}

type ToastAction =
  | { type: 'PUSH'; toast: Toast }
  | { type: 'POP'; id: string }
  | { type: 'CLEAR' };

interface ToastContextValue {
  queue: Toast[];
  pushToast: (toast: Omit<Toast, 'id'>) => void;
  popToast: (id: string) => void;
}

let nextId = 1;
const generateId = (): string => `toast-${nextId++}`;

const MAX_VISIBLE = 3;
const AUTO_DISMISS_MS = 5000;

function toastReducer(state: ToastState, action: ToastAction): ToastState {
  switch (action.type) {
    case 'PUSH': {
      const newQueue = [...state.queue, action.toast];
      if (newQueue.length > MAX_VISIBLE) {
        const removableIndex = newQueue.findIndex((t) => !t.persistent);
        if (removableIndex !== -1) {
          newQueue.splice(removableIndex, 1);
        } else {
          newQueue.splice(0, newQueue.length - MAX_VISIBLE);
        }
      }
      return { queue: newQueue };
    }
    case 'POP':
      return { queue: state.queue.filter((t) => t.id !== action.id) };
    case 'CLEAR':
      return { queue: [] };
    default:
      return state;
  }
}

export const ToastContext = createContext<ToastContextValue>({
  queue: [],
  pushToast: () => {},
  popToast: () => {},
});

export const ToastProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [state, dispatch] = useReducer(toastReducer, { queue: [] });
  const timersRef = useRef<Map<string, ReturnType<typeof setTimeout>>>(new Map());

  useEffect(() => {
    return () => {
      timersRef.current.forEach((timer) => clearTimeout(timer));
    };
  }, []);

  const popToast = useCallback((id: string) => {
    dispatch({ type: 'POP', id });
    const timer = timersRef.current.get(id);
    if (timer) {
      clearTimeout(timer);
      timersRef.current.delete(id);
    }
  }, []);

  const pushToast = useCallback(
    (toast: Omit<Toast, 'id'>) => {
      const fullToast: Toast = { ...toast, id: generateId() };
      dispatch({ type: 'PUSH', toast: fullToast });

      if (!fullToast.persistent) {
        const timer = setTimeout(() => {
          dispatch({ type: 'POP', id: fullToast.id });
          timersRef.current.delete(fullToast.id);
        }, AUTO_DISMISS_MS);
        timersRef.current.set(fullToast.id, timer);
      }

      return fullToast.id;
    },
    [],
  );

  const value = useMemo(
    () => ({ queue: state.queue, pushToast, popToast }),
    [state.queue, pushToast, popToast],
  );

  return <ToastContext.Provider value={value}>{children}</ToastContext.Provider>;
};
