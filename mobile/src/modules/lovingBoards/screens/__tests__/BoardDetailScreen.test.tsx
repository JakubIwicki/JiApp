import React from 'react';
import { act, fireEvent } from '@testing-library/react-native';
import { rtlRender } from '../../../../test/rtlUtils';
import type { Board, Item } from '../../types/api';
import BoardDetailScreen from '../BoardDetailScreen';

const mockNavigate = jest.fn();

jest.mock('@react-navigation/native-stack', () => {
  const ReactMock = require('react');
  return {
    createNativeStackNavigator: () => ({
      Navigator: ({ children }: { children: React.ReactNode }) =>
        ReactMock.createElement(ReactMock.Fragment, null, children),
      Screen: ({
        component: Component,
      }: {
        component?: React.ComponentType<unknown>;
      }) => (Component ? ReactMock.createElement(Component) : null),
    }),
  };
});

jest.mock('@react-navigation/native', () => {
  const actual = jest.requireActual('@react-navigation/native');
  return {
    ...actual,
    useNavigation: () => ({
      navigate: mockNavigate,
      setOptions: jest.fn(),
    }),
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

jest.mock('../../services/boardService', () =>
  jest.requireActual('../../services/__mocks__/boardService'),
);
jest.mock('../../services/itemService', () =>
  jest.requireActual('../../services/__mocks__/itemService'),
);
jest.mock('../../services/boardStreamService', () => ({
  openBoardStream: jest.fn(() => ({ close: jest.fn() })),
}));

import {
  setItemStatus,
  clearCompleted,
  resetWeekly,
  reset as resetItemService,
} from '../../services/__mocks__/itemService';
import {
  withBoards,
  reset as resetBoardService,
} from '../../services/__mocks__/boardService';

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

const makeBoard = (id: number, name: string, items: Item[] = []): Board => ({
  id,
  name,
  ownerUserId: 1,
  memberUserIds: [1, 2],
  createdAt: '2026-01-01T00:00:00.000Z',
  items,
});

const renderBoardDetail = (boardId: number) => {
  const props = {
    route: { params: { boardId } },
    navigation: { navigate: mockNavigate, goBack: jest.fn() },
  } as unknown as React.ComponentProps<typeof BoardDetailScreen>;
  return rtlRender(<BoardDetailScreen {...props} />);
};

describe('BoardDetailScreen', () => {
  beforeEach(() => {
    // The undo (5s) and cleared-message (4s) snackbar timers are armed inside
    // async handlers and never cleaned up (the mount-only cleanup reads the refs
    // as null), so real timers would leak live timers into the jest process.
    jest.useFakeTimers();
    jest.clearAllMocks();
    resetBoardService();
    resetItemService();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it('renders the board name and its active items', async () => {
    withBoards([
      makeBoard(1, 'Dom', [
        makeItem(1, { title: 'Mleko', category: 'dairy' }),
        makeItem(2, { title: 'Chleb', status: 'Completed' }),
      ]),
    ]);

    const { findByText, getByText } = renderBoardDetail(1);

    expect(await findByText('Dom')).toBeTruthy();
    expect(getByText('Mleko')).toBeTruthy();
    expect(getByText('lovingBoards.boardDetail.done · 1')).toBeTruthy();
  });

  it('marks an item as completed when its checkbox is pressed', async () => {
    withBoards([
      makeBoard(1, 'Dom', [makeItem(1, { title: 'Mleko', category: 'dairy' })]),
    ]);

    const { findByText, getByTestId } = renderBoardDetail(1);

    await findByText('Mleko');
    fireEvent.press(getByTestId('item-check-1'));

    expect(setItemStatus).toHaveBeenCalledWith(1, 1, 'Completed');
  });

  it('removes an item when its remove action is pressed', async () => {
    withBoards([
      makeBoard(1, 'Dom', [makeItem(1, { title: 'Mleko', category: 'dairy' })]),
    ]);

    const { findByText, getByTestId } = renderBoardDetail(1);

    await findByText('Mleko');
    fireEvent.press(getByTestId('item-remove-1'));

    expect(setItemStatus).toHaveBeenCalledWith(1, 1, 'Removed');
  });

  it('clears completed items when the clear button is pressed', async () => {
    withBoards([
      makeBoard(1, 'Dom', [
        makeItem(1, { title: 'Chleb', status: 'Completed' }),
      ]),
    ]);

    const { findByText, getByLabelText, getByTestId } = renderBoardDetail(1);

    await findByText('Dom');
    fireEvent.press(getByLabelText('lovingBoards.boardDetail.done (1)'));
    fireEvent.press(getByTestId('clear-completed-button'));

    // clearCompleted reloads the board after the service call; flush those
    // microtask state updates inside act so they don't warn after the test.
    await act(async () => {});

    expect(clearCompleted).toHaveBeenCalledWith(1);
  });

  it('resets the weekly items when the reset button is pressed', async () => {
    withBoards([
      makeBoard(1, 'Dom', [makeItem(1, { title: 'Mleko', category: 'dairy' })]),
    ]);

    const { findByText, getByTestId } = renderBoardDetail(1);

    await findByText('Mleko');
    fireEvent.press(getByTestId('board-detail-reset-weekly'));

    // resetWeekly reloads the board after the service call; flush those
    // microtask state updates inside act so they don't warn after the test.
    await act(async () => {});

    expect(resetWeekly).toHaveBeenCalledWith(1);
  });
});
