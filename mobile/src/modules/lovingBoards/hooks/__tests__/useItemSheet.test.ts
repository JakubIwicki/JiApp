import { renderHook, act } from '@testing-library/react-native';
import { Alert } from 'react-native';
import useItemSheet, { type UseItemSheetArgs } from '../useItemSheet';
import type { Board, Item } from '../../types/api';
import type { CreateItemPayload } from '../../services/itemService';

jest.mock('react-i18next', () => {
  const actual = jest.requireActual('react-i18next');
  return {
    ...actual,
    useTranslation: () => ({
      t: (key: string) => key,
    }),
  };
});

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

const makeBoard = (memberUserIds: number[] = [1, 2]): Board => ({
  id: 1,
  name: 'Dom',
  ownerUserId: 1,
  memberUserIds,
  createdAt: '2026-01-01T00:00:00.000Z',
  items: [],
});

const renderItemSheet = (overrides: Partial<UseItemSheetArgs> = {}) => {
  const addItem = jest.fn().mockResolvedValue(99);
  const updateItem = jest.fn().mockResolvedValue(undefined);
  const deleteItem = jest.fn().mockResolvedValue(undefined);
  const onDismiss = jest.fn();

  const args: UseItemSheetArgs = {
    existingItem: undefined,
    board: makeBoard(),
    isEditing: false,
    itemId: undefined,
    addItem,
    updateItem,
    deleteItem,
    onDismiss,
    ...overrides,
  };

  const utils = renderHook(() => useItemSheet(args));
  return { ...utils, addItem, updateItem, deleteItem, onDismiss };
};

beforeEach(() => {
  jest.clearAllMocks();
});

describe('useItemSheet', () => {
  it('starts with an empty form in create mode and exposes member ids', () => {
    const { result } = renderItemSheet({ board: makeBoard([1, 2, 3]) });

    expect(result.current.form).toEqual({
      title: '',
      quantity: '',
      category: '',
      note: '',
      assigneeUserId: '',
      dueDate: '',
      isRecurring: false,
      saving: false,
    });
    expect(result.current.memberIds).toEqual([1, 2, 3]);
    expect(result.current.titleError).toBeUndefined();
    expect(result.current.dueDateError).toBeUndefined();
  });

  it('seeds the form from the existing item in edit mode', () => {
    const existingItem = makeItem(1, {
      title: 'Mleko',
      quantity: '1',
      category: 'dairy',
      note: 'from shop',
      assigneeUserId: 5,
      expiryDate: '2026-12-31T00:00:00.000Z',
      isRecurring: true,
    });

    const { result } = renderItemSheet({
      existingItem,
      isEditing: true,
      itemId: 1,
    });

    expect(result.current.form.title).toBe('Mleko');
    expect(result.current.form.quantity).toBe('1');
    expect(result.current.form.category).toBe('dairy');
    expect(result.current.form.note).toBe('from shop');
    expect(result.current.form.assigneeUserId).toBe('5');
    expect(result.current.form.dueDate).toBe('2026-12-31');
    expect(result.current.form.isRecurring).toBe(true);
  });

  it('does not submit when the title is blank', async () => {
    const { result, addItem, onDismiss } = renderItemSheet();

    await act(async () => {
      await result.current.handleSave();
    });

    expect(result.current.titleError).toBe(
      'lovingBoards.itemSheet.titleRequired',
    );
    expect(addItem).not.toHaveBeenCalled();
    expect(onDismiss).not.toHaveBeenCalled();
  });

  it('rejects a due date that is not a YYYY-MM-DD string', async () => {
    const { result, addItem } = renderItemSheet();

    act(() => {
      result.current.setField('title', 'Mleko');
      result.current.setField('dueDate', '30/12/2026');
    });

    await act(async () => {
      await result.current.handleSave();
    });

    expect(result.current.dueDateError).toBe(
      'lovingBoards.itemSheet.dueDateInvalid',
    );
    expect(addItem).not.toHaveBeenCalled();
  });

  it('rejects a due date that matches the format but is not a real date', async () => {
    const { result, addItem } = renderItemSheet();

    act(() => {
      result.current.setField('title', 'Mleko');
      result.current.setField('dueDate', '2026-13-01');
    });

    await act(async () => {
      await result.current.handleSave();
    });

    expect(result.current.dueDateError).toBe(
      'lovingBoards.itemSheet.dueDateInvalid',
    );
    expect(addItem).not.toHaveBeenCalled();
  });

  it('submits the full payload in create mode and dismisses', async () => {
    const { result, addItem, onDismiss } = renderItemSheet();

    act(() => {
      result.current.setField('title', 'Mleko');
      result.current.setField('quantity', '1');
      result.current.setField('category', 'dairy');
      result.current.setField('note', 'from shop');
      result.current.setField('assigneeUserId', '5');
      result.current.setField('dueDate', '2026-12-31');
      result.current.setField('isRecurring', true);
    });

    await act(async () => {
      await result.current.handleSave();
    });

    const expected: CreateItemPayload = {
      title: 'Mleko',
      quantity: '1',
      category: 'dairy',
      note: 'from shop',
      assigneeUserId: 5,
      expiryDate: '2026-12-31',
      isRecurring: true,
    };
    expect(addItem).toHaveBeenCalledWith(expected);
    expect(onDismiss).toHaveBeenCalledTimes(1);
    expect(result.current.form.saving).toBe(false);
  });

  it('submits only the changed field in edit mode', async () => {
    const existingItem = makeItem(1, { title: 'Mleko' });

    const { result, updateItem, onDismiss } = renderItemSheet({
      existingItem,
      isEditing: true,
      itemId: 1,
    });

    act(() => {
      result.current.setField('title', 'Mleko 2%');
    });

    await act(async () => {
      await result.current.handleSave();
    });

    expect(updateItem).toHaveBeenCalledWith(1, { title: 'Mleko 2%' });
    expect(onDismiss).toHaveBeenCalledTimes(1);
  });

  it('skips the update when nothing changed in edit mode but still dismisses', async () => {
    const existingItem = makeItem(1, {
      title: 'Mleko',
      quantity: '1',
      category: 'dairy',
      note: 'from shop',
      assigneeUserId: 5,
      expiryDate: '2026-12-31T00:00:00.000Z',
    });

    const { result, updateItem, onDismiss } = renderItemSheet({
      existingItem,
      isEditing: true,
      itemId: 1,
    });

    await act(async () => {
      await result.current.handleSave();
    });

    expect(updateItem).not.toHaveBeenCalled();
    expect(onDismiss).toHaveBeenCalledTimes(1);
  });

  it('deletes the item through the confirm dialog in edit mode', async () => {
    const alertSpy = jest
      .spyOn(Alert, 'alert')
      .mockImplementation((_title, _msg, buttons) => {
        const destructive = buttons?.find(b => b.style === 'destructive');
        destructive?.onPress?.();
      });

    const { result, deleteItem, onDismiss } = renderItemSheet({
      existingItem: makeItem(1),
      isEditing: true,
      itemId: 1,
    });

    await act(async () => {
      result.current.handleDelete();
    });

    expect(deleteItem).toHaveBeenCalledWith(1);
    expect(onDismiss).toHaveBeenCalledTimes(1);

    alertSpy.mockRestore();
  });

  it('does nothing when deleting in create mode', () => {
    const alertSpy = jest.spyOn(Alert, 'alert');

    const { result, deleteItem, onDismiss } = renderItemSheet();

    act(() => {
      result.current.handleDelete();
    });

    expect(alertSpy).not.toHaveBeenCalled();
    expect(deleteItem).not.toHaveBeenCalled();
    expect(onDismiss).not.toHaveBeenCalled();

    alertSpy.mockRestore();
  });
});
