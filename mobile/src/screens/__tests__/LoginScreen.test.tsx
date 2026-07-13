import React from 'react';
import { fireEvent, waitFor, act } from '@testing-library/react-native';
import { composeStories } from '@storybook/react';

const mockNavigate = jest.fn();

jest.mock('@react-navigation/native-stack', () => {
  const React = require('react');
  return {
    createNativeStackNavigator: () => ({
      Navigator: ({ children }: { children: React.ReactNode }) =>
        React.createElement(React.Fragment, null, children),
      Screen: ({
        component: Component,
      }: {
        component?: React.ComponentType<unknown>;
      }) => (Component ? React.createElement(Component) : null),
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

jest.mock('../../services/authService');
jest.mock('../../services/storageService', () => {
  const actual = jest.requireActual('../../services/storageService');
  return {
    ...actual,
    getUsername: jest.fn(() => Promise.resolve(null)),
  };
});

import * as stories from '../LoginScreen.stories';
import { rtlRender } from '../../test/rtlUtils';
import {
  withLoginSuccess,
  withLoginFailure,
  login as mockAuthLogin,
  reset,
} from '../../services/__mocks__/authService';

const { Default } = composeStories(stories);

describe('LoginScreen', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    reset();
  });

  it('renders login form with username, password, remember me, and login button', () => {
    const { getByPlaceholderText, getAllByText, getByText } = rtlRender(
      <Default />,
    );
    expect(getByPlaceholderText('auth.username')).toBeTruthy();
    expect(getByPlaceholderText('auth.password')).toBeTruthy();
    expect(getAllByText('auth.login').length).toBeGreaterThanOrEqual(2);
    expect(getByText('auth.rememberMe')).toBeTruthy();
    expect(getByText('auth.goToRegister')).toBeTruthy();
  });

  it('calls validation errors when fields empty and login pressed', async () => {
    const { getByTestId, findByText } = rtlRender(<Default />);
    fireEvent.press(getByTestId('button'));

    expect(await findByText('auth.usernameRequired')).toBeTruthy();
    expect(await findByText('auth.passwordRequired')).toBeTruthy();
  });

  it('calls AuthContext.login with trimmed values', async () => {
    withLoginSuccess();
    const { getByPlaceholderText, getByTestId } = rtlRender(<Default />);

    fireEvent.changeText(getByPlaceholderText('auth.username'), '  testuser  ');
    fireEvent.changeText(getByPlaceholderText('auth.password'), '  pass123  ');

    fireEvent.press(getByTestId('button'));

    await waitFor(() => {
      expect(mockAuthLogin).toHaveBeenCalledWith('testuser', 'pass123');
    });
  });

  it('shows API error on login failure', async () => {
    withLoginFailure(new Error('Invalid'));
    const { getByPlaceholderText, getByTestId, findByText } = rtlRender(
      <Default />,
    );

    fireEvent.changeText(getByPlaceholderText('auth.username'), 'testuser');
    fireEvent.changeText(getByPlaceholderText('auth.password'), 'wrong');

    fireEvent.press(getByTestId('button'));

    expect(await findByText('auth.invalidCredentials')).toBeTruthy();
  });

  it('navigates to Register on link press', () => {
    const { getByText } = rtlRender(<Default />);
    fireEvent.press(getByText('auth.goToRegister'));
    expect(mockNavigate).toHaveBeenCalledWith('Register');
  });

  it('displays the server error message when login fails with _serverError on 401', async () => {
    withLoginFailure(
      Object.assign(new Error('Unauthorized'), {
        isAxiosError: true,
        response: {
          status: 401,
          data: { error: 'Account is locked. Try again in 15 minutes.' },
        },
        _serverError: 'Account is locked. Try again in 15 minutes.',
      }),
    );
    const { getByPlaceholderText, getByTestId, findByText } = rtlRender(
      <Default />,
    );

    fireEvent.changeText(getByPlaceholderText('auth.username'), 'testuser');
    fireEvent.changeText(getByPlaceholderText('auth.password'), 'wrong');
    fireEvent.press(getByTestId('button'));

    expect(
      await findByText('Account is locked. Try again in 15 minutes.'),
    ).toBeTruthy();
  });

  it('displays server error from response.data.error on non-401 failures', async () => {
    withLoginFailure(
      Object.assign(new Error('Server Error'), {
        isAxiosError: true,
        response: { status: 500, data: { error: 'Internal server error' } },
      }),
    );
    const { getByPlaceholderText, getByTestId, findByText } = rtlRender(
      <Default />,
    );

    fireEvent.changeText(getByPlaceholderText('auth.username'), 'testuser');
    fireEvent.changeText(getByPlaceholderText('auth.password'), 'wrong');
    fireEvent.press(getByTestId('button'));

    expect(await findByText('Internal server error')).toBeTruthy();
  });

  it('can be unmounted without errors during async load', async () => {
    const { unmount } = rtlRender(<Default />);
    unmount();
    await act(async () => {});
  });

  it('can be unmounted without errors during async login', async () => {
    let resolvePromise!: (value: unknown) => void;
    (mockAuthLogin as jest.Mock).mockImplementation(
      () =>
        new Promise(resolve => {
          resolvePromise = resolve;
        }),
    );

    const consoleErrorSpy = jest
      .spyOn(console, 'error')
      .mockImplementation(() => {});

    const { getByPlaceholderText, getByTestId, unmount } = rtlRender(
      <Default />,
    );

    fireEvent.changeText(getByPlaceholderText('auth.username'), 'testuser');
    fireEvent.changeText(getByPlaceholderText('auth.password'), 'pass123');

    fireEvent.press(getByTestId('button'));

    unmount();

    resolvePromise(undefined);

    await act(async () => {});

    const stateUpdateWarnings = consoleErrorSpy.mock.calls.filter(
      call =>
        typeof call[0] === 'string' &&
        call[0].includes('state update on an unmounted component'),
    );
    expect(stateUpdateWarnings).toHaveLength(0);

    consoleErrorSpy.mockRestore();
  });
});
