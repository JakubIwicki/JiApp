import { renderHook, act } from '@testing-library/react-native';
import useUndoSnackbar from '../useUndoSnackbar';

const UNDO_DURATION_MS = 5000;
const CLEARED_DURATION_MS = 4000;

beforeEach(() => {
  jest.useFakeTimers();
});

afterEach(() => {
  jest.useRealTimers();
});

describe('useUndoSnackbar', () => {
  it('shows the undo state on demand', () => {
    const { result } = renderHook(() => useUndoSnackbar());

    act(() => {
      result.current.showUndo(1, 'Removed');
    });

    expect(result.current.undoState).toEqual({
      itemId: 1,
      previousStatus: 'Removed',
    });
  });

  it('auto-dismisses the undo snackbar at exactly its timeout', () => {
    const { result } = renderHook(() => useUndoSnackbar());

    act(() => {
      result.current.showUndo(1, 'Removed');
      result.current.armUndoTimeout();
    });

    act(() => {
      jest.advanceTimersByTime(UNDO_DURATION_MS - 1);
    });
    expect(result.current.undoState).toEqual({
      itemId: 1,
      previousStatus: 'Removed',
    });

    act(() => {
      jest.advanceTimersByTime(1);
    });
    expect(result.current.undoState).toBeNull();
  });

  it('clearUndo before the timeout cancels the auto-dismiss', () => {
    const { result } = renderHook(() => useUndoSnackbar());

    act(() => {
      result.current.showUndo(1, 'Removed');
      result.current.armUndoTimeout();
    });
    expect(jest.getTimerCount()).toBe(1);

    act(() => {
      result.current.clearUndo();
    });
    expect(result.current.undoState).toBeNull();
    expect(jest.getTimerCount()).toBe(0);

    act(() => {
      jest.advanceTimersByTime(UNDO_DURATION_MS);
    });
    expect(result.current.undoState).toBeNull();
    expect(jest.getTimerCount()).toBe(0);
  });

  it('shows the cleared message and auto-dismisses at exactly its timeout', () => {
    const { result } = renderHook(() => useUndoSnackbar());

    act(() => {
      result.current.showCleared('Cleared 3 items');
    });
    expect(result.current.clearedMessage).toBe('Cleared 3 items');

    act(() => {
      jest.advanceTimersByTime(CLEARED_DURATION_MS - 1);
    });
    expect(result.current.clearedMessage).toBe('Cleared 3 items');

    act(() => {
      jest.advanceTimersByTime(1);
    });
    expect(result.current.clearedMessage).toBeNull();
  });

  it('dismissCleared cancels the cleared-message timer', () => {
    const { result } = renderHook(() => useUndoSnackbar());

    act(() => {
      result.current.showCleared('Cleared 3 items');
    });
    expect(jest.getTimerCount()).toBe(1);

    act(() => {
      result.current.dismissCleared();
    });
    expect(result.current.clearedMessage).toBeNull();
    expect(jest.getTimerCount()).toBe(0);
  });

  it('unmounting mid-timer clears the pending timers so nothing fires after unmount', () => {
    const { result, unmount } = renderHook(() => useUndoSnackbar());

    act(() => {
      result.current.showUndo(1, 'Removed');
      result.current.armUndoTimeout();
      result.current.showCleared('Cleared 3 items');
    });
    expect(jest.getTimerCount()).toBe(2);

    unmount();
    expect(jest.getTimerCount()).toBe(0);

    act(() => {
      jest.advanceTimersByTime(UNDO_DURATION_MS + CLEARED_DURATION_MS);
    });
    expect(jest.getTimerCount()).toBe(0);
  });
});
