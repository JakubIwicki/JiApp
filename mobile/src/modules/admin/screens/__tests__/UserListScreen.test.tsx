import React from 'react';
import { fireEvent, waitFor } from '@testing-library/react-native';
import { rtlRender } from '../../../../test/rtlUtils';
import type { UserSummary } from '../../types/api';
import UserListScreen from '../UserListScreen';

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

jest.mock('../../services/adminService', () =>
  jest.requireActual('../../services/__mocks__/adminService'),
);

import {
  listUsers,
  withUsers,
  reset,
} from '../../services/__mocks__/adminService';

const makeUser = (
  id: number,
  username: string,
  email: string,
  isLockedOut = false,
): UserSummary => ({
  id,
  username,
  email,
  displayName: username,
  roles: ['User'],
  isLockedOut,
});

describe('UserListScreen', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    reset();
  });

  it('renders the users returned by the service', async () => {
    withUsers([
      makeUser(1, 'alice', 'alice@example.com'),
      makeUser(2, 'bob', 'bob@example.com', true),
    ]);

    const { findByText, getByText } = rtlRender(<UserListScreen />);

    expect(await findByText('alice')).toBeTruthy();
    expect(getByText('bob')).toBeTruthy();
    expect(getByText('alice@example.com')).toBeTruthy();
    expect(getByText('admin.userList.locked')).toBeTruthy();
  });

  it('shows the empty state when there are no users', async () => {
    withUsers([]);

    const { findByText } = rtlRender(<UserListScreen />);

    expect(await findByText('admin.userList.empty')).toBeTruthy();
  });

  it('searches users as the query changes', async () => {
    withUsers([makeUser(1, 'alice', 'alice@example.com')]);

    const { findByText, getByPlaceholderText } = rtlRender(<UserListScreen />);

    await findByText('alice');
    fireEvent.changeText(
      getByPlaceholderText('admin.userList.searchPlaceholder'),
      'bo',
    );

    await waitFor(() => {
      expect(listUsers).toHaveBeenCalledWith('bo', 1, 20);
    });
  });

  it('navigates to the user detail screen on row press', async () => {
    withUsers([makeUser(1, 'alice', 'alice@example.com')]);

    const { findByText, getByTestId } = rtlRender(<UserListScreen />);

    await findByText('alice');
    fireEvent.press(getByTestId('user-row-1'));

    expect(mockNavigate).toHaveBeenCalledWith('UserDetail', { userId: 1 });
  });

  it('navigates to the create-user screen from the FAB', async () => {
    withUsers([]);

    const { findByTestId, getByTestId } = rtlRender(<UserListScreen />);

    await findByTestId('create-user-fab');
    fireEvent.press(getByTestId('create-user-fab'));

    expect(mockNavigate).toHaveBeenCalledWith('CreateUser');
  });

  it('navigates to the role list screen', async () => {
    withUsers([]);

    const { findByTestId, getByTestId } = rtlRender(<UserListScreen />);

    await findByTestId('goto-roles-button');
    fireEvent.press(getByTestId('goto-roles-button'));

    expect(mockNavigate).toHaveBeenCalledWith('RoleList');
  });
});
