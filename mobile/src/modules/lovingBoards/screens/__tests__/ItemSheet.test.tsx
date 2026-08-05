import React from 'react';
import { Alert } from 'react-native';
import { fireEvent, waitFor } from '@testing-library/react-native';
import { rtlRender } from '../../../../test/rtlUtils';
import type { Board, Item } from '../../types/api';
import ItemSheet from '../ItemSheet';

const mockNavigate = jest.fn();
const mockGoBack = jest.fn();

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
  createItem,
  updateItem,
  deleteItem,
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

const renderItemSheet = (params: { boardId: number; itemId?: number }) => {
  const props = {
    route: { params },
    navigation: { navigate: mockNavigate, goBack: mockGoBack },
  } as unknown as React.ComponentProps<typeof ItemSheet>;
  return rtlRender(<ItemSheet {...props} />);
};

describe('ItemSheet', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    resetBoardService();
    resetItemService();
  });

  it('creates a new item from the form', async () => {
    withBoards([makeBoard(1, 'Dom')]);

    const { findByText, getByTestId } = renderItemSheet({ boardId: 1 });

    expect(await findByText('lovingBoards.itemSheet.addTitle')).toBeTruthy();
    fireEvent.changeText(getByTestId('item-title-input'), 'Mleko');
    fireEvent.changeText(getByTestId('item-category-input'), 'dairy');
    fireEvent.press(getByTestId('item-save-button'));

    await waitFor(() => {
      expect(createItem).toHaveBeenCalledWith(
        1,
        expect.objectContaining({ title: 'Mleko', category: 'dairy' }),
      );
    });
    expect(mockGoBack).toHaveBeenCalled();
  });

  it('does not save when the title is blank', async () => {
    withBoards([makeBoard(1, 'Dom')]);

    const { findByText, getByTestId } = renderItemSheet({ boardId: 1 });

    await findByText('lovingBoards.itemSheet.addTitle');
    fireEvent.press(getByTestId('item-save-button'));

    expect(
      await findByText('lovingBoards.itemSheet.titleRequired'),
    ).toBeTruthy();
    expect(createItem).not.toHaveBeenCalled();
  });

  it('updates an existing item when editing', async () => {
    withBoards([makeBoard(1, 'Dom', [makeItem(1, { title: 'Mleko' })])]);

    const { findByText, getByTestId } = renderItemSheet({
      boardId: 1,
      itemId: 1,
    });

    expect(await findByText('lovingBoards.itemSheet.editTitle')).toBeTruthy();
    fireEvent.changeText(getByTestId('item-title-input'), 'Mleko 2%');
    fireEvent.press(getByTestId('item-save-button'));

    await waitFor(() => {
      expect(updateItem).toHaveBeenCalledWith(1, 1, { title: 'Mleko 2%' });
    });
    expect(mockGoBack).toHaveBeenCalled();
  });

  it('deletes the item from the edit screen', async () => {
    withBoards([makeBoard(1, 'Dom', [makeItem(1, { title: 'Mleko' })])]);
    const alertSpy = jest
      .spyOn(Alert, 'alert')
      .mockImplementation((_title, _msg, buttons) => {
        const destructive = buttons?.find(b => b.style === 'destructive');
        destructive?.onPress?.();
      });

    const { findByText, getByTestId } = renderItemSheet({
      boardId: 1,
      itemId: 1,
    });

    await findByText('lovingBoards.itemSheet.editTitle');
    fireEvent.press(getByTestId('item-delete-button'));

    await waitFor(() => {
      expect(deleteItem).toHaveBeenCalledWith(1, 1);
    });
    expect(mockGoBack).toHaveBeenCalled();

    alertSpy.mockRestore();
  });
});
