import { renderHook, act } from '@testing-library/react-native';
import useBoardDetail from '../useBoardDetail';
import useBoard from '../useBoard';
import type { Board, Item } from '../../types/api';

const mockNavigate = jest.fn();

jest.mock('@react-navigation/native', () => ({
  useNavigation: () => ({ navigate: mockNavigate }),
}));

jest.mock('../../../../hooks/useAuth', () => ({
  __esModule: true,
  default: () => ({ userId: 7 }),
}));

jest.mock('react-i18next', () => {
  const actual = jest.requireActual('react-i18next');
  return {
    ...actual,
    useTranslation: () => ({
      t: (key: string) => key,
    }),
  };
});

jest.mock('../useBoard', () => ({
  __esModule: true,
  default: jest.fn(),
}));

const mockUseBoard = useBoard as jest.Mock;
const mockSetItemStatus = jest.fn();
const mockClearCompleted = jest.fn();
const mockResetWeekly = jest.fn();
const mockRefetch = jest.fn();

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

const makeBoard = (items: Item[]): Board => ({
  id: 1,
  name: 'Dom',
  ownerUserId: 1,
  memberUserIds: [1, 2],
  createdAt: '2026-01-01T00:00:00.000Z',
  items,
});

const stubUseBoard = (
  items: Item[],
  overrides: Record<string, unknown> = {},
) => {
  mockUseBoard.mockReturnValue({
    board: makeBoard(items),
    items,
    isLoading: false,
    error: null,
    presence: [],
    isLive: true,
    refetch: mockRefetch,
    addItem: jest.fn(),
    updateItem: jest.fn(),
    setItemStatus: mockSetItemStatus,
    deleteItem: jest.fn(),
    clearCompleted: mockClearCompleted,
    resetWeekly: mockResetWeekly,
    updateBoard: jest.fn(),
    addMember: jest.fn(),
    removeMember: jest.fn(),
    ...overrides,
  });
};

beforeEach(() => {
  jest.clearAllMocks();
});

describe('useBoardDetail', () => {
  it('groups items into category buckets, completed, and uncategorized', () => {
    const item1 = makeItem(1, { category: 'dairy', status: 'Needed' });
    const item2 = makeItem(2, { category: 'dairy', status: 'Needed' });
    const item3 = makeItem(3, { category: 'bakery', status: 'Needed' });
    const item4 = makeItem(4, { category: null, status: 'Needed' });
    const item5 = makeItem(5, { category: 'dairy', status: 'Completed' });

    stubUseBoard([item1, item2, item3, item4, item5]);
    const { result } = renderHook(() => useBoardDetail(1));

    expect(result.current.activeByCategory.get('dairy')).toEqual([
      item1,
      item2,
    ]);
    expect(result.current.activeByCategory.get('bakery')).toEqual([item3]);
    expect(result.current.uncategorizedActive).toEqual([item4]);
    expect(result.current.completedItems).toEqual([item5]);
    expect(result.current.categoryNames).toEqual([
      'bakery',
      'dairy',
      '__uncategorized__',
    ]);
    expect(result.current.allNeeded).toEqual([item1, item2, item3, item4]);
    expect(result.current.completedCount).toBe(1);
    expect(result.current.progressDone).toBe(1);
    expect(result.current.progressTotal).toBe(5);
    expect(result.current.hasItems).toBe(true);
    expect(result.current.hasCompleted).toBe(true);
    expect(result.current.userId).toBe(7);
  });

  it('recomputes the grouping when items change', () => {
    stubUseBoard([makeItem(1, { category: 'dairy', status: 'Needed' })]);
    const { result, rerender } = renderHook(
      (props: { boardId: number }) => useBoardDetail(props.boardId),
      { initialProps: { boardId: 1 } },
    );

    expect(
      result.current.activeByCategory.get('dairy')?.map(i => i.id),
    ).toEqual([1]);

    stubUseBoard([makeItem(2, { category: 'bakery', status: 'Needed' })]);
    rerender({ boardId: 1 });

    expect(result.current.activeByCategory.has('dairy')).toBe(false);
    expect(
      result.current.activeByCategory.get('bakery')?.map(i => i.id),
    ).toEqual([2]);
    expect(result.current.completedItems).toEqual([]);
  });

  it('adds and removes categories from the collapsed set on toggle', () => {
    stubUseBoard([]);
    const { result } = renderHook(() => useBoardDetail(1));

    expect(result.current.collapsedCategories.has('dairy')).toBe(false);

    act(() => {
      result.current.toggleCategory('dairy');
    });
    expect(result.current.collapsedCategories.has('dairy')).toBe(true);

    act(() => {
      result.current.toggleCategory('dairy');
    });
    expect(result.current.collapsedCategories.has('dairy')).toBe(false);
  });

  it('toggles the done section expansion', () => {
    stubUseBoard([]);
    const { result } = renderHook(() => useBoardDetail(1));

    act(() => {
      result.current.toggleDoneExpanded();
    });
    expect(result.current.doneExpanded).toBe(true);

    act(() => {
      result.current.toggleDoneExpanded();
    });
    expect(result.current.doneExpanded).toBe(false);
  });

  it('sets clearing while clearCompleted runs, then resets it and shows the message', async () => {
    let resolveClear!: () => void;
    const clearCompletedMock = jest.fn(
      () =>
        new Promise<void>(resolve => {
          resolveClear = resolve;
        }),
    );
    stubUseBoard([makeItem(1, { status: 'Completed' })], {
      clearCompleted: clearCompletedMock,
    });
    const { result } = renderHook(() => useBoardDetail(1));

    act(() => {
      result.current.toggleDoneExpanded();
    });
    expect(result.current.doneExpanded).toBe(true);

    let promise!: Promise<void>;
    act(() => {
      promise = result.current.handleClearCompleted();
    });
    expect(result.current.clearing).toBe(true);

    await act(async () => {
      resolveClear();
      await promise;
    });

    expect(result.current.clearing).toBe(false);
    expect(result.current.doneExpanded).toBe(false);
    expect(result.current.clearedMessage).toBe(
      'lovingBoards.boardDetail.clearedWithCount',
    );
  });

  it('resets clearing even when clearCompleted rejects', async () => {
    stubUseBoard([makeItem(1)], {
      clearCompleted: jest.fn().mockRejectedValue(new Error('boom')),
    });
    const { result } = renderHook(() => useBoardDetail(1));

    await act(async () => {
      await result.current.handleClearCompleted();
    });

    expect(result.current.clearing).toBe(false);
    expect(result.current.clearedMessage).toBeNull();
  });

  it('forwards resetWeekly and setItemStatus for toggling and removing items', async () => {
    const item = makeItem(1, { status: 'Needed' });
    stubUseBoard([item]);
    const { result } = renderHook(() => useBoardDetail(1));

    await act(async () => {
      await result.current.handleResetWeekly();
    });
    expect(mockResetWeekly).toHaveBeenCalledTimes(1);

    await act(async () => {
      await result.current.handleToggleItem(item);
    });
    expect(mockSetItemStatus).toHaveBeenCalledWith(1, 'Completed');

    await act(async () => {
      await result.current.handleToggleItem({ ...item, status: 'Completed' });
    });
    expect(mockSetItemStatus).toHaveBeenCalledWith(1, 'Needed');

    await act(async () => {
      await result.current.handleRemoveItem(item);
    });
    expect(mockSetItemStatus).toHaveBeenCalledWith(1, 'Removed');
    expect(result.current.undoState).toEqual({
      itemId: 1,
      previousStatus: 'Needed',
    });
  });

  it('navigates to the item sheet and members screen', () => {
    stubUseBoard([]);
    const { result } = renderHook(() => useBoardDetail(1));

    act(() => {
      result.current.handleAddItem();
    });
    expect(mockNavigate).toHaveBeenCalledWith('ItemSheet', { boardId: 1 });

    act(() => {
      result.current.handleEditItem(makeItem(1));
    });
    expect(mockNavigate).toHaveBeenCalledWith('ItemSheet', {
      boardId: 1,
      itemId: 1,
    });

    act(() => {
      result.current.handleMembersPress();
    });
    expect(mockNavigate).toHaveBeenCalledWith('BoardMembers', { boardId: 1 });
  });
});
