import React from 'react';
import { render, fireEvent, waitFor, act } from '@testing-library/react-native';

// Mock useToast
const mockShowSuccess = jest.fn();
jest.mock('../../hooks/useToast', () => ({
  __esModule: true,
  default: () => ({
    showSuccess: mockShowSuccess,
    showError: jest.fn(),
    showInfo: jest.fn(),
    showWarning: jest.fn(),
  }),
}));

// Mock useScreenTitle
jest.mock('../../hooks/useScreenTitle', () => ({
  __esModule: true,
  default: jest.fn(),
}));

// Mock useAuth
const mockRegister = jest.fn();
jest.mock('../../hooks/useAuth', () => ({
  __esModule: true,
  default: () => ({
    register: mockRegister,
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

import RegisterScreen from '../RegisterScreen';

describe('RegisterScreen', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders all 4 inputs + register button', () => {
    const { getByPlaceholderText, getAllByText } = render(<RegisterScreen />);
    expect(getByPlaceholderText('auth.username')).toBeTruthy();
    expect(getByPlaceholderText('auth.email')).toBeTruthy();
    expect(getByPlaceholderText('auth.password')).toBeTruthy();
    expect(getByPlaceholderText('auth.displayName')).toBeTruthy();
    // Title and button both show 'auth.register'
    const registerElements = getAllByText('auth.register');
    expect(registerElements.length).toBeGreaterThanOrEqual(2);
  });

  it('shows validation errors when fields empty and register pressed', async () => {
    const { getByTestId, findByText } = render(<RegisterScreen />);
    fireEvent.press(getByTestId('button'));

    expect(await findByText('auth.usernameRequired')).toBeTruthy();
    expect(await findByText('auth.emailRequired')).toBeTruthy();
    expect(await findByText('auth.passwordRequired')).toBeTruthy();
    expect(await findByText('auth.displayNameRequired')).toBeTruthy();
  });

  it('username too short error (< 3 chars)', async () => {
    const { getByPlaceholderText, getByTestId, findByText } = render(
      <RegisterScreen />,
    );
    const usernameInput = getByPlaceholderText('auth.username');
    fireEvent.changeText(usernameInput, 'ab');

    fireEvent.press(getByTestId('button'));
    expect(await findByText('auth.usernameTooShort')).toBeTruthy();
  });

  it('password too short error (< 4 chars)', async () => {
    const { getByPlaceholderText, getByTestId, findByText } = render(
      <RegisterScreen />,
    );
    const passwordInput = getByPlaceholderText('auth.password');
    fireEvent.changeText(passwordInput, '123');

    fireEvent.press(getByTestId('button'));
    expect(await findByText('auth.passwordTooShort')).toBeTruthy();
  });

  it('calls AuthContext.register with trimmed values', async () => {
    mockRegister.mockResolvedValueOnce(undefined);
    const { getByPlaceholderText, getByTestId } = render(<RegisterScreen />);

    fireEvent.changeText(getByPlaceholderText('auth.username'), '  john  ');
    fireEvent.changeText(
      getByPlaceholderText('auth.email'),
      '  john@test.com  ',
    );
    fireEvent.changeText(getByPlaceholderText('auth.password'), '  Pass1234  ');
    fireEvent.changeText(
      getByPlaceholderText('auth.displayName'),
      '  John Doe  ',
    );

    fireEvent.press(getByTestId('button'));

    await waitFor(() => {
      expect(mockRegister).toHaveBeenCalledWith(
        'john',
        'john@test.com',
        'Pass1234',
        'John Doe',
      );
    });
  });

  it('navigates to LoginScreen on success', async () => {
    mockRegister.mockResolvedValueOnce(undefined);
    const { getByPlaceholderText, getByTestId } = render(<RegisterScreen />);

    fireEvent.changeText(getByPlaceholderText('auth.username'), 'john');
    fireEvent.changeText(getByPlaceholderText('auth.email'), 'john@test.com');
    fireEvent.changeText(getByPlaceholderText('auth.password'), 'Pass1234');
    fireEvent.changeText(getByPlaceholderText('auth.displayName'), 'John Doe');

    fireEvent.press(getByTestId('button'));

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('Login');
    });
  });

  it('shows error on registration failure', async () => {
    mockRegister.mockRejectedValueOnce(new Error('Registration failed'));
    const { getByPlaceholderText, getByTestId, findByText } = render(
      <RegisterScreen />,
    );

    fireEvent.changeText(getByPlaceholderText('auth.username'), 'john');
    fireEvent.changeText(getByPlaceholderText('auth.email'), 'john@test.com');
    fireEvent.changeText(getByPlaceholderText('auth.password'), 'Pass1234');
    fireEvent.changeText(getByPlaceholderText('auth.displayName'), 'John Doe');

    fireEvent.press(getByTestId('button'));

    expect(await findByText('auth.registerFailed')).toBeTruthy();
  });

  it('shows go to login link', () => {
    const { getByText } = render(<RegisterScreen />);
    expect(getByText('auth.goToLogin')).toBeTruthy();
  });

  it('displays the server error message when registration fails with data.error', async () => {
    mockRegister.mockRejectedValueOnce({
      isAxiosError: true,
      response: { status: 400, data: { error: 'Username is already taken.' } },
    });
    const { getByPlaceholderText, getByTestId, findByText } = render(
      <RegisterScreen />,
    );

    fireEvent.changeText(getByPlaceholderText('auth.username'), 'existinguser');
    fireEvent.changeText(getByPlaceholderText('auth.email'), 'john@test.com');
    fireEvent.changeText(getByPlaceholderText('auth.password'), 'Pass1234');
    fireEvent.changeText(getByPlaceholderText('auth.displayName'), 'John Doe');

    fireEvent.press(getByTestId('button'));

    expect(await findByText('Username is already taken.')).toBeTruthy();
  });

  it('displays field-level errors from ValidationProblemDetails response', async () => {
    mockRegister.mockRejectedValueOnce({
      isAxiosError: true,
      response: {
        status: 400,
        data: {
          errors: {
            errors: [
              'Password must have at least one uppercase letter',
              'Username must be between 3 and 50 characters',
            ],
          },
        },
      },
    });
    const { getByPlaceholderText, getByTestId, findByText } = render(
      <RegisterScreen />,
    );

    fireEvent.changeText(getByPlaceholderText('auth.username'), 'existinguser');
    fireEvent.changeText(getByPlaceholderText('auth.email'), 'john@test.com');
    fireEvent.changeText(getByPlaceholderText('auth.password'), 'ValidPass1');
    fireEvent.changeText(getByPlaceholderText('auth.displayName'), 'John Doe');

    fireEvent.press(getByTestId('button'));

    expect(
      await findByText('Password must have at least one uppercase letter'),
    ).toBeTruthy();
    expect(
      await findByText('Username must be between 3 and 50 characters'),
    ).toBeTruthy();
  });

  it('can be unmounted without errors during async register', async () => {
    let resolvePromise!: (value: unknown) => void;
    mockRegister.mockReturnValue(
      new Promise(resolve => {
        resolvePromise = resolve;
      }),
    );

    const consoleErrorSpy = jest
      .spyOn(console, 'error')
      .mockImplementation(() => {});

    const { getByPlaceholderText, getByTestId, unmount } = render(
      <RegisterScreen />,
    );

    fireEvent.changeText(getByPlaceholderText('auth.username'), 'john');
    fireEvent.changeText(getByPlaceholderText('auth.email'), 'john@test.com');
    fireEvent.changeText(getByPlaceholderText('auth.password'), 'Pass1234');
    fireEvent.changeText(getByPlaceholderText('auth.displayName'), 'John Doe');

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
