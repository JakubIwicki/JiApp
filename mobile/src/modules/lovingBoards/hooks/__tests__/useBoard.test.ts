import { renderHook, act } from '@testing-library/react-native';
import useBoard, { DEBOUNCE_MS } from '../useBoard';
import * as boardService from '../../services/boardService';
import * as itemService from '../../services/itemService';
import { openBoardStream } from '../../services/boardStreamService';
import type { BoardStreamParams } from '../../services/boardStreamService';
import type { Board, Item } from '../../types/api';
import type { BoardStreamEvent } from '../../types/events';

// ── Mocks ──────────────────────────────────────────────────────────────────

jest.mock('../../services/boardService', () => ({
  getBoard: jest.fn(),
  updateBoard: jest.fn(),
  addMember: jest.fn(),
  removeMember: jest.fn(),
}));

jest.mock('../../services/itemService', () => ({
  createItem: jest.fn(),
  updateItem: jest.fn(),
  setItemStatus: jest.fn(),
  deleteItem: jest.fn(),
  clearCompleted: jest.fn(),
  resetWeekly: jest.fn(),
}));

jest.mock('../../services/boardStreamService', () => ({
  openBoardStream: jest.fn(),
}));

jest.mock('@react-navigation/native', () => {
  const actual = jest.requireActual('@react-navigation/native');
  return {
    ...actual,
    useFocusEffect: (callback: () => void) => {
      const { useEffect } = jest.requireActual('react');
      useEffect(callback, [callback]);
    },
  };
});

jest.mock('react-i18next', () => {
  const actual = jest.requireActual('react-i18next');
  return {
    ...actual,
    useTranslation: () => ({
      t: (key: string) => key,
    }),
  };
});

const mockGetBoard = boardService.getBoard as jest.Mock;
const mockCreateItem = itemService.createItem as jest.Mock;
const mockSetItemStatus = itemService.setItemStatus as jest.Mock;
const mockOpenBoardStream = openBoardStream as jest.Mock;

let capturedOnEvent: ((event: BoardStreamEvent) => void) | null = null;

// ── Fixtures ───────────────────────────────────────────────────────────────

const makeItem = (id: number, overrides: Partial<Item> = {}): Item => ({
  id,
  boardId: 1,
  title: `Item ${id}`,
  quantity: null,
  category: null,
  note: null,
  assigneeUserId: null,
  expiryDate: null,
  isRecurring: false,
  status: 'Needed',
  addedByUserId: 1,
  completedByUserId: null,
  createdAt: '2026-01-01T00:00:00.000Z',
  updatedAt: '2026-01-01T00:00:00.000Z',
  removedAt: null,
  ...overrides,
});

const makeBoard = (items: Item[] = []): Board => ({
  id: 1,
  name: 'Dom',
  ownerUserId: 1,
  memberUserIds: [1, 2],
  createdAt: '2026-01-01T00:00:00.000Z',
  items,
});

const REFETCH_EVENTS: readonly [string, BoardStreamEvent][] = [
  ['item.added', { type: 'item.added', itemId: 99 }],
  ['item.updated', { type: 'item.updated', itemId: 99 }],
  ['board.updated', { type: 'board.updated', boardId: 1 }],
  ['member.changed', { type: 'member.changed', boardId: 1 }],
  ['recurring.reset', { type: 'recurring.reset', reset: 5 }],
  ['board.deleted', { type: 'board.deleted', boardId: 1 }],
];

// ── Helpers ────────────────────────────────────────────────────────────────

/** Flush pending microtasks so the initial loadBoard settles */
const flushMicrotasks = async (count = 10): Promise<void> => {
  for (let i = 0; i < count; i++) {
    await act(async () => {});
  }
};

beforeEach(() => {
  jest.clearAllMocks();
  capturedOnEvent = null;
  mockOpenBoardStream.mockImplementation((params: BoardStreamParams) => {
    capturedOnEvent = params.onEvent;
    return { close: jest.fn() };
  });
});

afterEach(() => {
  jest.useRealTimers();
});

// ── Tests ──────────────────────────────────────────────────────────────────

describe('useBoard', () => {
  it('loads the board on focus and opens the stream', async () => {
    mockGetBoard.mockResolvedValue(makeBoard([makeItem(1)]));

    const { result } = renderHook(() => useBoard(1));
    await flushMicrotasks();

    expect(mockGetBoard).toHaveBeenCalledWith(1);
    expect(result.current.items).toHaveLength(1);
    expect(mockOpenBoardStream).toHaveBeenCalledTimes(1);
  });

  it('applies item.status locally without calling the service', async () => {
    mockGetBoard.mockResolvedValue(makeBoard([makeItem(1)]));
    const { result } = renderHook(() => useBoard(1));
    await flushMicrotasks();
    mockGetBoard.mockClear();

    act(() => {
      capturedOnEvent?.({
        type: 'item.status',
        itemId: 1,
        status: 'Completed',
      });
    });

    expect(result.current.items[0]?.status).toBe('Completed');
    expect(mockGetBoard).not.toHaveBeenCalled();
  });

  it('removes an item locally when item.removed arrives', async () => {
    mockGetBoard.mockResolvedValue(makeBoard([makeItem(1), makeItem(2)]));
    const { result } = renderHook(() => useBoard(1));
    await flushMicrotasks();
    mockGetBoard.mockClear();

    act(() => {
      capturedOnEvent?.({ type: 'item.removed', itemId: 1 });
    });

    expect(result.current.items.map(i => i.id)).toEqual([2]);
    expect(mockGetBoard).not.toHaveBeenCalled();
  });

  it('clears the listed items locally when items.cleared arrives', async () => {
    mockGetBoard.mockResolvedValue(
      makeBoard([
        makeItem(1, { status: 'Completed' }),
        makeItem(2, { status: 'Completed' }),
        makeItem(3),
      ]),
    );
    const { result } = renderHook(() => useBoard(1));
    await flushMicrotasks();
    mockGetBoard.mockClear();

    act(() => {
      capturedOnEvent?.({ type: 'items.cleared', itemIds: [1, 2] });
    });

    expect(result.current.items.map(i => i.id)).toEqual([3]);
    expect(mockGetBoard).not.toHaveBeenCalled();
  });

  it('toggling an item status never triggers a board refetch', async () => {
    mockGetBoard.mockResolvedValue(makeBoard([makeItem(1)]));
    mockSetItemStatus.mockResolvedValue(undefined);
    const { result } = renderHook(() => useBoard(1));
    await flushMicrotasks();
    mockGetBoard.mockClear();

    await act(async () => {
      await result.current.setItemStatus(1, 'Completed');
    });

    expect(mockSetItemStatus).toHaveBeenCalledWith(1, 1, 'Completed');
    expect(mockGetBoard).not.toHaveBeenCalled();
  });

  it.each(REFETCH_EVENTS)(
    'refetches the board (debounced) when %s arrives',
    async (_name, event) => {
      jest.useFakeTimers();
      mockGetBoard.mockResolvedValue(makeBoard());
      renderHook(() => useBoard(1));
      await flushMicrotasks();
      mockGetBoard.mockClear();

      // Expire the initial-load grace window so the echo is not dropped
      await act(async () => {
        jest.advanceTimersByTime(DEBOUNCE_MS + 10);
      });

      act(() => {
        capturedOnEvent?.(event);
      });
      expect(mockGetBoard).not.toHaveBeenCalled();

      await act(async () => {
        jest.advanceTimersByTime(DEBOUNCE_MS);
      });

      expect(mockGetBoard).toHaveBeenCalledTimes(1);
    },
  );

  it('starts an action refetch immediately without waiting the debounce', async () => {
    jest.useFakeTimers();
    mockGetBoard.mockResolvedValue(makeBoard());
    mockCreateItem.mockResolvedValue({ id: 99 });
    const { result } = renderHook(() => useBoard(1));
    await flushMicrotasks();
    mockGetBoard.mockClear();

    let addPromise!: Promise<number | undefined>;
    await act(async () => {
      addPromise = result.current.addItem({ title: 'Milk' });
    });

    // The GET fired with no debounce elapsing — timers were never advanced
    expect(mockGetBoard).toHaveBeenCalledTimes(1);

    await act(async () => {
      await expect(addPromise).resolves.toBe(99);
    });

    expect(mockGetBoard).toHaveBeenCalledTimes(1);
  });

  it('coalesces an action refetch with its own echo into one getBoard call', async () => {
    jest.useFakeTimers();
    mockGetBoard.mockResolvedValue(makeBoard());
    mockCreateItem.mockResolvedValue({ id: 99 });
    const { result } = renderHook(() => useBoard(1));
    await flushMicrotasks();
    mockGetBoard.mockClear();

    let addPromise!: Promise<number | undefined>;
    await act(async () => {
      addPromise = result.current.addItem({ title: 'Milk' });
    });

    // The write's own echo lands right after the action GET completes and is
    // dropped inside the post-fetch grace window
    await act(async () => {
      capturedOnEvent?.({ type: 'item.added', itemId: 99 });
    });

    await act(async () => {
      await expect(addPromise).resolves.toBe(99);
    });

    expect(mockCreateItem).toHaveBeenCalledTimes(1);
    expect(mockGetBoard).toHaveBeenCalledTimes(1);
  });

  it("joins an in-flight GET that started after the request's own change (no extra GET)", async () => {
    jest.useFakeTimers();
    const getBoardResolvers: Array<(board: Board) => void> = [];
    mockGetBoard.mockImplementation(
      () =>
        new Promise<Board>(resolve => {
          getBoardResolvers.push(resolve);
        }),
    );
    mockCreateItem.mockResolvedValue({ id: 99 });

    const { result } = renderHook(() => useBoard(1));
    // Settle the initial focus load
    await act(async () => {
      getBoardResolvers.shift()?.(makeBoard());
    });
    mockGetBoard.mockClear();

    let addPromise!: Promise<number | undefined>;
    await act(async () => {
      addPromise = result.current.addItem({ title: 'Milk' });
    });
    // The action GET started (after the write) and is still pending
    expect(mockGetBoard).toHaveBeenCalledTimes(1);

    // The write's echo arrives while that GET is in flight. The GET started
    // after the write, so it covers the change — the echo joins it, and no
    // second GET is fired.
    await act(async () => {
      capturedOnEvent?.({ type: 'item.added', itemId: 99 });
    });
    expect(mockGetBoard).toHaveBeenCalledTimes(1);

    // Complete the in-flight GET
    await act(async () => {
      getBoardResolvers.shift()?.(makeBoard());
    });
    await act(async () => {
      await expect(addPromise).resolves.toBe(99);
    });

    expect(mockGetBoard).toHaveBeenCalledTimes(1);
  });

  it('chains a fresh fetch when an action write lands while an older GET is in flight', async () => {
    jest.useFakeTimers();
    const getBoardResolvers: Array<(board: Board) => void> = [];
    mockGetBoard.mockImplementation(
      () =>
        new Promise<Board>(resolve => {
          getBoardResolvers.push(resolve);
        }),
    );
    mockCreateItem.mockResolvedValue({ id: 99 });

    const { result } = renderHook(() => useBoard(1));
    // The initial load GET is still in flight — older than the write below
    mockGetBoard.mockClear();

    // The write completes after the older GET started
    await act(async () => {
      jest.advanceTimersByTime(300);
    });

    let addPromise!: Promise<number | undefined>;
    let addResolved = false;
    await act(async () => {
      addPromise = result.current.addItem({ title: 'Milk' });
      addPromise.then(() => {
        addResolved = true;
      });
    });
    // No parallel GET — the action waits for the older GET, then chains
    expect(mockGetBoard).toHaveBeenCalledTimes(0);

    // The older GET lands with pre-write data — must NOT resolve the action
    await act(async () => {
      getBoardResolvers.shift()?.(makeBoard([makeItem(1)]));
    });
    expect(addResolved).toBe(false);

    // The chained fresh GET runs and lands with post-write data
    await act(async () => {
      getBoardResolvers.shift()?.(makeBoard([makeItem(1), makeItem(2)]));
    });
    await act(async () => {
      await expect(addPromise).resolves.toBe(99);
    });

    expect(result.current.items.map(i => i.id)).toEqual([1, 2]);
    expect(mockGetBoard).toHaveBeenCalledTimes(1);
  });

  it('chains a fresh fetch when an echo arrives while an older GET is in flight', async () => {
    jest.useFakeTimers();
    const getBoardResolvers: Array<(board: Board) => void> = [];
    mockGetBoard.mockImplementation(
      () =>
        new Promise<Board>(resolve => {
          getBoardResolvers.push(resolve);
        }),
    );

    const { result } = renderHook(() => useBoard(1));
    // The initial load GET is still in flight — older than the echo below
    mockGetBoard.mockClear();

    await act(async () => {
      jest.advanceTimersByTime(300);
    });

    // Echo arrives while the older GET is in flight
    await act(async () => {
      capturedOnEvent?.({ type: 'board.updated', boardId: 1 });
    });
    expect(mockGetBoard).toHaveBeenCalledTimes(0);

    // The older GET lands with pre-change data
    await act(async () => {
      getBoardResolvers.shift()?.(makeBoard([makeItem(1)]));
    });
    // The chained fresh GET now runs
    expect(mockGetBoard).toHaveBeenCalledTimes(1);

    // ...and lands with post-change data
    await act(async () => {
      getBoardResolvers.shift()?.(makeBoard([makeItem(1), makeItem(2)]));
    });

    expect(result.current.items.map(i => i.id)).toEqual([1, 2]);
    expect(mockGetBoard).toHaveBeenCalledTimes(1);
  });

  it('drops an echo arriving within the grace window of a just-landed GET', async () => {
    jest.useFakeTimers();
    mockGetBoard.mockResolvedValue(makeBoard());
    renderHook(() => useBoard(1));
    await flushMicrotasks();
    mockGetBoard.mockClear();

    // Echo arrives while the initial GET's landing is still inside the grace
    // window — even after the full window elapses, no refetch fires
    act(() => {
      capturedOnEvent?.({ type: 'board.updated', boardId: 1 });
    });

    await act(async () => {
      jest.advanceTimersByTime(DEBOUNCE_MS + 50);
    });

    expect(mockGetBoard).not.toHaveBeenCalled();
  });

  it('refetches within DEBOUNCE_MS of the first echo despite a trickle of echoes', async () => {
    jest.useFakeTimers();
    mockGetBoard.mockResolvedValue(makeBoard());
    renderHook(() => useBoard(1));
    await flushMicrotasks();
    mockGetBoard.mockClear();

    // Expire the initial-load grace window so echoes are not dropped
    await act(async () => {
      jest.advanceTimersByTime(DEBOUNCE_MS + 10);
    });

    act(() => {
      capturedOnEvent?.({ type: 'board.updated', boardId: 1 });
    });
    expect(mockGetBoard).not.toHaveBeenCalled();

    // A trickle of echoes must not postpone the first refetch
    await act(async () => {
      jest.advanceTimersByTime(100);
      capturedOnEvent?.({ type: 'board.updated', boardId: 1 });
    });
    await act(async () => {
      jest.advanceTimersByTime(100);
      capturedOnEvent?.({ type: 'board.updated', boardId: 1 });
    });
    expect(mockGetBoard).not.toHaveBeenCalled();

    // The refetch fires at DEBOUNCE_MS from the FIRST echo
    await act(async () => {
      jest.advanceTimersByTime(DEBOUNCE_MS - 200);
    });
    expect(mockGetBoard).toHaveBeenCalledTimes(1);
  });

  it('still refreshes the board when the stream is down (no echo)', async () => {
    jest.useFakeTimers();
    mockGetBoard.mockResolvedValue(makeBoard([makeItem(1)]));
    mockCreateItem.mockResolvedValue({ id: 99 });
    const { result } = renderHook(() => useBoard(1));
    await flushMicrotasks();
    mockGetBoard.mockClear();

    let addPromise!: Promise<number | undefined>;
    await act(async () => {
      addPromise = result.current.addItem({ title: 'Milk' });
    });

    await act(async () => {
      await expect(addPromise).resolves.toBe(99);
    });

    // No echo ever arrives — the action's immediate GET still refreshed state
    expect(mockGetBoard).toHaveBeenCalledTimes(1);
  });
});
