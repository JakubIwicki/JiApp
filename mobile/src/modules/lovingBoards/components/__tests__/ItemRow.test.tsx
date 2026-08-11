import React from 'react';
import { fireEvent } from '@testing-library/react-native';
import { rtlRender } from '../../../../test/rtlUtils';
import type { Item } from '../../types/api';
import ItemRow from '../ItemRow';

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

const renderRow = (
  item: Item,
  {
    currentUserId,
    onToggle = jest.fn(),
    onEdit = jest.fn(),
    onRemove = jest.fn(),
  }: {
    currentUserId?: number;
    onToggle?: (item: Item) => void;
    onEdit?: (item: Item) => void;
    onRemove?: (item: Item) => void;
  } = {},
) => {
  const utils = rtlRender(
    <ItemRow
      item={item}
      currentUserId={currentUserId}
      onToggle={onToggle}
      onEdit={onEdit}
      onRemove={onRemove}
    />,
  );
  return { ...utils, onToggle, onEdit, onRemove };
};

describe('ItemRow', () => {
  afterEach(() => {
    jest.clearAllMocks();
    jest.useRealTimers();
  });

  it('renders the title and the added-by caption for a needed item', () => {
    const { getByText } = renderRow(makeItem(1, { title: 'Mleko' }));

    expect(getByText('Mleko')).toBeTruthy();
    expect(getByText('lovingBoards.boardDetail.addedBy')).toBeTruthy();
  });

  it('renders the bought-by caption for a completed item', () => {
    const { getByText, queryByText } = renderRow(
      makeItem(1, {
        title: 'Mleko',
        status: 'Completed',
        completedByUserId: 3,
      }),
    );

    expect(getByText('lovingBoards.boardDetail.boughtBy')).toBeTruthy();
    expect(queryByText('lovingBoards.boardDetail.addedBy')).toBeNull();
  });

  it('renders the checkbox checked for a completed item with a matching current user', () => {
    const { getByTestId, getByText } = renderRow(
      makeItem(1, {
        title: 'Mleko',
        status: 'Completed',
        completedByUserId: 3,
      }),
      { currentUserId: 3 },
    );

    expect(getByTestId('item-check-1').props.accessibilityState.checked).toBe(
      true,
    );
    expect(getByText('✓')).toBeTruthy();
  });

  it('renders the checkbox checked for a completed item with no current user', () => {
    const { getByTestId, getByText } = renderRow(
      makeItem(1, {
        title: 'Mleko',
        status: 'Completed',
        completedByUserId: 3,
      }),
    );

    expect(getByTestId('item-check-1').props.accessibilityState.checked).toBe(
      true,
    );
    expect(getByText('✓')).toBeTruthy();
  });

  it('renders only the title for a removed item and no interactive controls', () => {
    const { getByText, queryByTestId } = renderRow(
      makeItem(1, { title: 'Mleko', status: 'Removed' }),
    );

    expect(getByText('Mleko')).toBeTruthy();
    expect(queryByTestId('item-check-1')).toBeNull();
    expect(queryByTestId('item-body-1')).toBeNull();
    expect(queryByTestId('item-edit-1')).toBeNull();
    expect(queryByTestId('item-remove-1')).toBeNull();
  });

  it('fires onToggle with the item when the checkbox is pressed', () => {
    const item = makeItem(1, { title: 'Mleko' });
    const { getByTestId, onToggle } = renderRow(item);

    fireEvent.press(getByTestId('item-check-1'));

    expect(onToggle).toHaveBeenCalledTimes(1);
    expect(onToggle).toHaveBeenCalledWith(item);
  });

  it('fires onEdit with the item when the body is pressed', () => {
    const item = makeItem(1, { title: 'Mleko' });
    const { getByTestId, onEdit } = renderRow(item);

    fireEvent.press(getByTestId('item-body-1'));

    expect(onEdit).toHaveBeenCalledTimes(1);
    expect(onEdit).toHaveBeenCalledWith(item);
  });

  it('fires onEdit with the item when the edit action is pressed', () => {
    const item = makeItem(1, { title: 'Mleko' });
    const { getByTestId, onEdit } = renderRow(item);

    fireEvent.press(getByTestId('item-edit-1'));

    expect(onEdit).toHaveBeenCalledTimes(1);
    expect(onEdit).toHaveBeenCalledWith(item);
  });

  it('fires onRemove with the item when the remove action is pressed', () => {
    const item = makeItem(1, { title: 'Mleko' });
    const { getByTestId, onRemove } = renderRow(item);

    fireEvent.press(getByTestId('item-remove-1'));

    expect(onRemove).toHaveBeenCalledTimes(1);
    expect(onRemove).toHaveBeenCalledWith(item);
  });

  it('shows a quantity pill when the item has a quantity', () => {
    const { getByText } = renderRow(
      makeItem(1, { title: 'Mleko', quantity: '2' }),
    );

    expect(getByText('lovingBoards.boardDetail.qty')).toBeTruthy();
  });

  it('hides the quantity pill when the item has none', () => {
    const { queryByText } = renderRow(makeItem(1, { title: 'Mleko' }));

    expect(queryByText('lovingBoards.boardDetail.qty')).toBeNull();
  });

  it('shows the recurring pill for a recurring item', () => {
    const { getByText, getByLabelText } = renderRow(
      makeItem(1, { title: 'Mleko', isRecurring: true }),
    );

    expect(getByText('🔁')).toBeTruthy();
    expect(getByLabelText('lovingBoards.boardDetail.recurring')).toBeTruthy();
  });

  it('hides the recurring pill for a non-recurring item', () => {
    const { queryByText } = renderRow(makeItem(1, { title: 'Mleko' }));

    expect(queryByText('🔁')).toBeNull();
  });

  it('shows the assignee avatar when an assignee is set', () => {
    const { getByText } = renderRow(
      makeItem(1, { title: 'Mleko', assigneeUserId: 42 }),
    );

    expect(getByText('42')).toBeTruthy();
  });

  it('shows the assignee avatar beside a completed item', () => {
    const { getByText } = renderRow(
      makeItem(1, {
        title: 'Mleko',
        status: 'Completed',
        assigneeUserId: 42,
        completedByUserId: 3,
      }),
      { currentUserId: 3 },
    );

    expect(getByText('42')).toBeTruthy();
  });

  it('shows the due-tomorrow pill when the expiry date is tomorrow', () => {
    jest.useFakeTimers();
    jest.setSystemTime(new Date('2026-08-11T12:00:00Z'));
    const { getByText, queryByText } = renderRow(
      makeItem(1, { title: 'Mleko', expiryDate: '2026-08-12T12:00:00Z' }),
    );

    expect(getByText('⚠ lovingBoards.boardDetail.dueTomorrow')).toBeTruthy();
    expect(queryByText('lovingBoards.boardDetail.overdue')).toBeNull();
  });

  it('shows the overdue pill when the expiry date is in the past', () => {
    jest.useFakeTimers();
    jest.setSystemTime(new Date('2026-08-11T12:00:00Z'));
    const { getByText, queryByText } = renderRow(
      makeItem(1, { title: 'Mleko', expiryDate: '2026-08-10T12:00:00Z' }),
    );

    expect(getByText('lovingBoards.boardDetail.overdue')).toBeTruthy();
    expect(queryByText('lovingBoards.boardDetail.dueTomorrow')).toBeNull();
  });

  it('shows no urgency pill for an expiry date beyond tomorrow', () => {
    jest.useFakeTimers();
    jest.setSystemTime(new Date('2026-08-11T12:00:00Z'));
    const { queryByText } = renderRow(
      makeItem(1, { title: 'Mleko', expiryDate: '2026-08-20T12:00:00Z' }),
    );

    expect(queryByText('lovingBoards.boardDetail.dueTomorrow')).toBeNull();
    expect(queryByText('lovingBoards.boardDetail.overdue')).toBeNull();
  });
});
