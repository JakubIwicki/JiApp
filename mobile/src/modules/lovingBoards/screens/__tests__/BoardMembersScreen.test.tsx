import React from 'react';
import { fireEvent, waitFor } from '@testing-library/react-native';
import { rtlRender } from '../../../../test/rtlUtils';
import type { Board } from '../../types/api';
import BoardMembersScreen from '../BoardMembersScreen';

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
  addMember,
  removeMember,
  withBoards,
  reset,
} from '../../services/__mocks__/boardService';

const makeBoard = (
  id: number,
  name: string,
  memberUserIds: number[] = [1],
  ownerUserId = 1,
): Board => ({
  id,
  name,
  ownerUserId,
  memberUserIds,
  createdAt: '2026-01-01T00:00:00.000Z',
  items: [],
});

const renderBoardMembers = (boardId: number) => {
  const props = {
    route: { params: { boardId } },
    navigation: { navigate: mockNavigate },
  } as unknown as React.ComponentProps<typeof BoardMembersScreen>;
  return rtlRender(<BoardMembersScreen {...props} />);
};

describe('BoardMembersScreen', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    reset();
  });

  it('renders every member and marks the owner', async () => {
    withBoards([makeBoard(1, 'Dom', [1, 2], 1)]);

    const { findByText, getByText, getByTestId, queryByTestId } =
      renderBoardMembers(1);

    expect(await findByText('#1')).toBeTruthy();
    expect(getByText('#2')).toBeTruthy();
    expect(getByText('lovingBoards.boardMembers.owner')).toBeTruthy();
    expect(queryByTestId('member-remove-1')).toBeNull();
    expect(getByTestId('member-remove-2')).toBeTruthy();
  });

  it('adds a member by user id', async () => {
    withBoards([makeBoard(1, 'Dom', [1], 1)]);

    const { findByText, getByTestId } = renderBoardMembers(1);

    await findByText('#1');
    fireEvent.changeText(getByTestId('board-members-userid-input'), '42');
    fireEvent.press(getByTestId('board-members-add'));

    await waitFor(() => {
      expect(addMember).toHaveBeenCalledWith(1, 42);
    });
  });

  it('removes a non-owner member', async () => {
    withBoards([makeBoard(1, 'Dom', [1, 2], 1)]);

    const { findByText, getByTestId } = renderBoardMembers(1);

    await findByText('#2');
    fireEvent.press(getByTestId('member-remove-2'));

    await waitFor(() => {
      expect(removeMember).toHaveBeenCalledWith(1, 2);
    });
  });
});
