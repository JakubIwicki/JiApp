import { renderHook, act } from '@testing-library/react-native';
import useBoards from '../useBoards';
import {
  listBoards,
  createBoard,
  withBoards,
  withBoardError,
  reset,
} from '../../services/__mocks__/boardService';
import type { Board, ListBoardsResponse } from '../../types/api';

jest.mock('../../services/boardService', () =>
  jest.requireActual('../../services/__mocks__/boardService'),
);

jest.mock('@react-navigation/native', () => {
  const actual = jest.requireActual('@react-navigation/native');
  return {
    ...actual,
    useFocusEffect: (callback: () => void) => {
      const { useEffect } = jest.requireActual('react');
      mockFocusCallback = callback;
      useEffect(callback, [callback]);
    },
  };
});

let mockFocusCallback: (() => void) | null = null;

const mockListBoards = listBoards as jest.Mock;
const defaultListBoardsImpl = mockListBoards.getMockImplementation();

const makeBoard = (id: number, name: string): Board => ({
  id,
  name,
  ownerUserId: 1,
  memberUserIds: [1, 2],
  createdAt: '2026-01-01T00:00:00.000Z',
  items: [],
});

/** Flush pending microtasks so the async focus load settles */
const flushMicrotasks = async (count = 10): Promise<void> => {
  for (let i = 0; i < count; i++) {
    await act(async () => {});
  }
};

beforeEach(() => {
  jest.clearAllMocks();
  reset();
  mockFocusCallback = null;
  // Reset tests that override listBoards restore the state-driven default
  if (defaultListBoardsImpl) {
    mockListBoards.mockImplementation(defaultListBoardsImpl);
  }
});

describe('useBoards', () => {
  it('loads the boards on focus', async () => {
    withBoards([makeBoard(1, 'Dom')]);

    const { result } = renderHook(() => useBoards());
    await flushMicrotasks();

    expect(listBoards).toHaveBeenCalledTimes(1);
    expect(result.current.boards).toEqual([makeBoard(1, 'Dom')]);
    expect(result.current.isLoading).toBe(false);
    expect(result.current.hasMore).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it('propagates hasMore from the response without inventing a page size', async () => {
    mockListBoards.mockResolvedValue({
      boards: [makeBoard(1, 'Dom')],
      hasMore: true,
    });

    const { result } = renderHook(() => useBoards());
    await flushMicrotasks();

    expect(result.current.hasMore).toBe(true);
    expect(result.current.boards).toEqual([makeBoard(1, 'Dom')]);
  });

  it('creates a board then reloads the list', async () => {
    withBoards([]);

    const { result } = renderHook(() => useBoards());
    await flushMicrotasks();
    mockListBoards.mockClear();

    let id: number | undefined;
    await act(async () => {
      id = await result.current.createBoard('Dom');
    });

    expect(id).toBe(1);
    expect(createBoard).toHaveBeenCalledWith('Dom');
    expect(mockListBoards).toHaveBeenCalledTimes(1);
    expect(result.current.boards).toHaveLength(1);
    expect(result.current.boards[0]?.name).toBe('Dom');
  });

  it('sets error and clears loading when the load fails', async () => {
    withBoardError(new Error('Network down'));

    const { result } = renderHook(() => useBoards());
    await flushMicrotasks();

    expect(result.current.error).toBe('lovingBoards.errors.loadBoards');
    expect(result.current.boards).toEqual([]);
    expect(result.current.isLoading).toBe(false);
  });

  it('aborts the in-flight load when the hook unmounts', () => {
    const abortSpy = jest.spyOn(AbortController.prototype, 'abort');
    mockListBoards.mockImplementation(() => new Promise<never>(() => {}));

    const { unmount } = renderHook(() => useBoards());

    expect(abortSpy).not.toHaveBeenCalled();
    unmount();
    expect(abortSpy).toHaveBeenCalledTimes(1);

    abortSpy.mockRestore();
  });

  it('aborts the in-flight request on re-focus so a stale response never lands', async () => {
    const resolvers: Array<(value: ListBoardsResponse) => void> = [];
    mockListBoards.mockImplementation(
      () =>
        new Promise<ListBoardsResponse>(resolve => {
          resolvers.push(resolve);
        }),
    );

    const { result } = renderHook(() => useBoards());

    // Re-focus while the first load is still in flight
    await act(async () => {
      mockFocusCallback?.();
    });
    expect(resolvers).toHaveLength(2);

    // The first (stale) response must be dropped — the controller was aborted
    await act(async () => {
      resolvers[0]?.({ boards: [makeBoard(1, 'stale')], hasMore: false });
    });
    expect(result.current.boards).toEqual([]);

    // The fresh response from the re-focus lands
    await act(async () => {
      resolvers[1]?.({ boards: [makeBoard(2, 'fresh')], hasMore: false });
    });
    expect(result.current.boards).toEqual([makeBoard(2, 'fresh')]);
    expect(result.current.isLoading).toBe(false);
  });
});
