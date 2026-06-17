import React from 'react';
import { render, fireEvent, waitFor, act } from '@testing-library/react-native';

// Mock useAuth
const mockLogin = jest.fn();
jest.mock('../../hooks/useAuth', () => ({
  __esModule: true,
  default: () => ({
    login: mockLogin,
  }),
}));

// Mock react-i18next
jest.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

// Mock @react-navigation/native useNavigation
const mockNavigate = jest.fn();
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

// Mock storageService
jest.mock('../../services/storageService', () => ({
  getCredentials: jest.fn(() => Promise.resolve(null)),
  saveCredentials: jest.fn(() => Promise.resolve()),
  clearCredentials: jest.fn(() => Promise.resolve()),
}));

import LoginScreen from '../LoginScreen';

describe('LoginScreen', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders login form with username, password, remember me, and login button', () => {
    const { getByPlaceholderText, getAllByText, getByText } = render(
      <LoginScreen />,
    );
    expect(getByPlaceholderText('auth.username')).toBeTruthy();
    expect(getByPlaceholderText('auth.password')).toBeTruthy();
    // Title and button both show 'auth.login'
    expect(getAllByText('auth.login').length).toBeGreaterThanOrEqual(2);
    expect(getByText('auth.rememberMe')).toBeTruthy();
    expect(getByText('auth.goToRegister')).toBeTruthy();
  });

  it('calls validation errors when fields empty and login pressed', async () => {
    const { getByTestId, findByText } = render(<LoginScreen />);
    fireEvent.press(getByTestId('button'));

    expect(await findByText('auth.usernameRequired')).toBeTruthy();
    expect(await findByText('auth.passwordRequired')).toBeTruthy();
  });

  it('calls AuthContext.login with trimmed values', async () => {
    mockLogin.mockResolvedValueOnce(undefined);
    const { getByPlaceholderText, getByTestId } = render(<LoginScreen />);

    fireEvent.changeText(getByPlaceholderText('auth.username'), '  testuser  ');
    fireEvent.changeText(getByPlaceholderText('auth.password'), '  pass123  ');

    fireEvent.press(getByTestId('button'));

    await waitFor(() => {
      expect(mockLogin).toHaveBeenCalledWith('testuser', 'pass123');
    });
  });

  it('shows API error on login failure', async () => {
    mockLogin.mockRejectedValueOnce(new Error('Invalid'));
    const { getByPlaceholderText, getByTestId, findByText } = render(
      <LoginScreen />,
    );

    fireEvent.changeText(getByPlaceholderText('auth.username'), 'testuser');
    fireEvent.changeText(getByPlaceholderText('auth.password'), 'wrong');

    fireEvent.press(getByTestId('button'));

    expect(await findByText('auth.invalidCredentials')).toBeTruthy();
  });

  it('navigates to Register on link press', () => {
    const { getByText } = render(<LoginScreen />);
    fireEvent.press(getByText('auth.goToRegister'));
    expect(mockNavigate).toHaveBeenCalledWith('Register');
  });

  it('displays the server error message when login fails with _serverError on 401', async () => {
    mockLogin.mockRejectedValueOnce({
      isAxiosError: true,
      response: {
        status: 401,
        data: { error: 'Account is locked. Try again in 15 minutes.' },
      },
      _serverError: 'Account is locked. Try again in 15 minutes.',
    });
    const { getByPlaceholderText, getByTestId, findByText } = render(
      <LoginScreen />,
    );

    fireEvent.changeText(getByPlaceholderText('auth.username'), 'testuser');
    fireEvent.changeText(getByPlaceholderText('auth.password'), 'wrong');
    fireEvent.press(getByTestId('button'));

    expect(
      await findByText('Account is locked. Try again in 15 minutes.'),
    ).toBeTruthy();
  });

  it('displays server error from response.data.error on non-401 failures', async () => {
    mockLogin.mockRejectedValueOnce({
      isAxiosError: true,
      response: { status: 500, data: { error: 'Internal server error' } },
    });
    const { getByPlaceholderText, getByTestId, findByText } = render(
      <LoginScreen />,
    );

    fireEvent.changeText(getByPlaceholderText('auth.username'), 'testuser');
    fireEvent.changeText(getByPlaceholderText('auth.password'), 'wrong');
    fireEvent.press(getByTestId('button'));

    expect(await findByText('Internal server error')).toBeTruthy();
  });

  it('can be unmounted without errors during async load', async () => {
    // Verify that unmounting before async load completes does not throw
    const { unmount } = render(<LoginScreen />);
    unmount();
    // Allow any pending promises to settle
    await act(async () => {});
  });

  it('can be unmounted without errors during async login', async () => {
    let resolvePromise!: (value: unknown) => void;
    mockLogin.mockReturnValue(
      new Promise(resolve => {
        resolvePromise = resolve;
      }),
    );

    const consoleErrorSpy = jest
      .spyOn(console, 'error')
      .mockImplementation(() => {});

    const { getByPlaceholderText, getByTestId, unmount } = render(
      <LoginScreen />,
    );

    fireEvent.changeText(getByPlaceholderText('auth.username'), 'testuser');
    fireEvent.changeText(getByPlaceholderText('auth.password'), 'pass123');

    // Trigger the async operation
    fireEvent.press(getByTestId('button'));

    // Unmount before the async operation completes
    unmount();

    // Resolve after unmount to trigger catch/finally
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
