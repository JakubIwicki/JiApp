import React from 'react';
import { fireEvent, waitFor } from '@testing-library/react-native';
import { rtlRender } from '../../../../test/rtlUtils';
import type { Board } from '../../types/api';
import BoardListScreen from '../BoardListScreen';

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

import {
  createBoard,
  withBoards,
  withBoardError,
  reset,
} from '../../services/__mocks__/boardService';

const makeBoard = (
  id: number,
  name: string,
  memberUserIds: number[] = [1],
): Board => ({
  id,
  name,
  ownerUserId: 1,
  memberUserIds,
  createdAt: '2026-01-01T00:00:00.000Z',
  items: [],
});

describe('BoardListScreen', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    reset();
  });

  it('lists the boards returned by the service', async () => {
    withBoards([makeBoard(1, 'Wakacje'), makeBoard(2, 'Dom', [1, 2])]);

    const { findByText, getByText } = rtlRender(<BoardListScreen />);

    expect(await findByText('Wakacje')).toBeTruthy();
    expect(getByText('Dom')).toBeTruthy();
  });

  it('shows the empty state when there are no boards', async () => {
    withBoards([]);

    const { findByText } = rtlRender(<BoardListScreen />);

    expect(await findByText('lovingBoards.boardList.empty')).toBeTruthy();
    expect(await findByText('lovingBoards.boardList.emptyHint')).toBeTruthy();
  });

  it('shows the error state with a retry button when loading fails', async () => {
    withBoardError();

    const { findByText, getByTestId } = rtlRender(<BoardListScreen />);

    expect(await findByText('lovingBoards.errors.loadBoards')).toBeTruthy();
    expect(getByTestId('board-list-retry')).toBeTruthy();
  });

  it('creates a board from the create form and opens it', async () => {
    withBoards([]);

    const { findByTestId, getByTestId } = rtlRender(<BoardListScreen />);

    await findByTestId('board-list-screen');
    fireEvent.press(getByTestId('board-list-fab'));
    fireEvent.changeText(getByTestId('board-create-input'), 'Nowa Tablica');
    fireEvent.press(getByTestId('board-create-confirm'));

    await waitFor(() => {
      expect(createBoard).toHaveBeenCalledWith('Nowa Tablica');
    });
    expect(mockNavigate).toHaveBeenCalledWith('BoardDetail', { boardId: 1 });
  });
});
