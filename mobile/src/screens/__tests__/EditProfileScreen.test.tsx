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
const mockUpdateProfile = jest.fn();
jest.mock('../../hooks/useAuth', () => ({
  __esModule: true,
  default: () => ({
    updateProfile: mockUpdateProfile,
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

// Mock authService
const mockGetProfile = jest.fn();
const mockUpdateProfileService = jest.fn();
const mockChangePasswordService = jest.fn();
jest.mock('../../services/authService', () => ({
  getProfile: (...args: any[]) => mockGetProfile(...args),
  updateProfile: (...args: any[]) => mockUpdateProfileService(...args),
  changePassword: (...args: any[]) => mockChangePasswordService(...args),
}));

import EditProfileScreen from '../EditProfileScreen';

describe('EditProfileScreen', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockGetProfile.mockResolvedValue({
      id: 1,
      displayName: 'Test User',
      email: 'test@example.com',
      roles: [],
      permissions: [],
    });
  });

  it('renders profile section with pre-filled fields', async () => {
    const { findByDisplayValue } = render(<EditProfileScreen />);

    expect(await findByDisplayValue('Test User')).toBeTruthy();
    expect(await findByDisplayValue('test@example.com')).toBeTruthy();
  });

  it('renders all input fields', async () => {
    const { findAllByTestId } = render(<EditProfileScreen />);

    // All FormInput components share testID "form-input"
    const inputs = await findAllByTestId('form-input');
    expect(inputs.length).toBe(5);
  });

  it('renders two save buttons', async () => {
    const { getByTestId } = render(<EditProfileScreen />);

    expect(getByTestId('save-profile-button')).toBeTruthy();
    expect(getByTestId('save-password-button')).toBeTruthy();
  });

  it('shows validation error on empty display name when saving profile', async () => {
    // Return empty profile so fields start empty
    mockGetProfile.mockResolvedValue({
      id: 1,
      displayName: '',
      email: '',
      roles: [],
      permissions: [],
    });

    const { getByTestId, findByText } = render(<EditProfileScreen />);

    fireEvent.press(getByTestId('save-profile-button'));

    expect(await findByText('auth.displayNameRequired')).toBeTruthy();
    expect(await findByText('auth.emailRequired')).toBeTruthy();
  });

  it('shows validation error on invalid email', async () => {
    mockGetProfile.mockResolvedValue({
      id: 1,
      displayName: 'Test',
      email: 'not-an-email',
      roles: [],
      permissions: [],
    });

    const { getByPlaceholderText, getByTestId, findByText } = render(
      <EditProfileScreen />,
    );

    // Wait for prefill, then set an invalid email
    await waitFor(() => {
      expect(mockGetProfile).toHaveBeenCalled();
    });

    fireEvent.changeText(
      getByPlaceholderText('settings.email'),
      'not-an-email',
    );

    fireEvent.press(getByTestId('save-profile-button'));

    expect(await findByText('auth.invalidEmail')).toBeTruthy();
  });

  it('shows validation errors for empty password fields', async () => {
    const { getByTestId, findAllByText } = render(<EditProfileScreen />);

    fireEvent.press(getByTestId('save-password-button'));

    // Should show passwordRequired for all three password fields
    const errors = await findAllByText('auth.passwordRequired');
    expect(errors.length).toBe(3);
  });

  it('shows validation error when passwords do not match', async () => {
    const { getByPlaceholderText, getByTestId, findByText } = render(
      <EditProfileScreen />,
    );

    fireEvent.changeText(
      getByPlaceholderText('settings.currentPassword'),
      'oldPassword1',
    );
    fireEvent.changeText(
      getByPlaceholderText('settings.newPassword'),
      'newPassword1',
    );
    fireEvent.changeText(
      getByPlaceholderText('settings.confirmPassword'),
      'differentNewPassword1',
    );

    fireEvent.press(getByTestId('save-password-button'));

    expect(await findByText('settings.passwordsMustMatch')).toBeTruthy();
  });

  it('shows validation error when new password too short', async () => {
    const { getByPlaceholderText, getByTestId, findByText } = render(
      <EditProfileScreen />,
    );

    fireEvent.changeText(
      getByPlaceholderText('settings.currentPassword'),
      'oldPassword1',
    );
    fireEvent.changeText(getByPlaceholderText('settings.newPassword'), 'short');
    fireEvent.changeText(
      getByPlaceholderText('settings.confirmPassword'),
      'short',
    );

    fireEvent.press(getByTestId('save-password-button'));

    expect(await findByText('auth.passwordTooShort')).toBeTruthy();
  });

  it('calls updateProfile service on successful profile save', async () => {
    mockUpdateProfile.mockResolvedValueOnce(undefined);

    const { getByPlaceholderText, getByTestId } = render(<EditProfileScreen />);

    // Wait for prefill
    await waitFor(() => {
      expect(mockGetProfile).toHaveBeenCalled();
    });

    fireEvent.changeText(getByPlaceholderText('auth.displayName'), 'New Name');
    fireEvent.changeText(
      getByPlaceholderText('settings.email'),
      'new@example.com',
    );

    fireEvent.press(getByTestId('save-profile-button'));

    await waitFor(() => {
      expect(mockUpdateProfile).toHaveBeenCalledWith(
        'New Name',
        'new@example.com',
      );
    });
  });

  it('shows success toast on profile update', async () => {
    mockUpdateProfile.mockResolvedValueOnce(undefined);

    const { getByTestId } = render(<EditProfileScreen />);

    await waitFor(() => {
      expect(mockGetProfile).toHaveBeenCalled();
    });

    fireEvent.press(getByTestId('save-profile-button'));

    await waitFor(() => {
      expect(mockShowSuccess).toHaveBeenCalledWith('settings.profileUpdated');
    });
  });

  it('calls changePassword service on successful password save', async () => {
    mockChangePasswordService.mockResolvedValueOnce(undefined);

    const { getByPlaceholderText, getByTestId } = render(<EditProfileScreen />);

    fireEvent.changeText(
      getByPlaceholderText('settings.currentPassword'),
      'oldPassword1',
    );
    fireEvent.changeText(
      getByPlaceholderText('settings.newPassword'),
      'newPassword1',
    );
    fireEvent.changeText(
      getByPlaceholderText('settings.confirmPassword'),
      'newPassword1',
    );

    fireEvent.press(getByTestId('save-password-button'));

    await waitFor(() => {
      expect(mockChangePasswordService).toHaveBeenCalledWith(
        'oldPassword1',
        'newPassword1',
      );
    });
  });

  it('shows success toast on password change', async () => {
    mockChangePasswordService.mockResolvedValueOnce(undefined);

    const { getByPlaceholderText, getByTestId } = render(<EditProfileScreen />);

    fireEvent.changeText(
      getByPlaceholderText('settings.currentPassword'),
      'oldPassword1',
    );
    fireEvent.changeText(
      getByPlaceholderText('settings.newPassword'),
      'newPassword1',
    );
    fireEvent.changeText(
      getByPlaceholderText('settings.confirmPassword'),
      'newPassword1',
    );

    fireEvent.press(getByTestId('save-password-button'));

    await waitFor(() => {
      expect(mockShowSuccess).toHaveBeenCalledWith('settings.passwordChanged');
    });
  });

  it('clears password fields after successful password change', async () => {
    mockChangePasswordService.mockResolvedValueOnce(undefined);

    const { getByPlaceholderText, getByTestId } = render(<EditProfileScreen />);

    fireEvent.changeText(
      getByPlaceholderText('settings.currentPassword'),
      'oldPassword1',
    );
    fireEvent.changeText(
      getByPlaceholderText('settings.newPassword'),
      'newPassword1',
    );
    fireEvent.changeText(
      getByPlaceholderText('settings.confirmPassword'),
      'newPassword1',
    );

    fireEvent.press(getByTestId('save-password-button'));

    await waitFor(() => {
      expect(mockChangePasswordService).toHaveBeenCalled();
    });

    // Password fields should be cleared
    const inputs = [
      getByPlaceholderText('settings.currentPassword'),
      getByPlaceholderText('settings.newPassword'),
      getByPlaceholderText('settings.confirmPassword'),
    ];
    for (const input of inputs) {
      expect(input.props.value).toBe('');
    }
  });

  it('displays server error message on profile save failure', async () => {
    mockUpdateProfile.mockRejectedValueOnce({
      isAxiosError: true,
      response: { status: 400, data: { error: 'Email is already taken.' } },
    });

    const { getByTestId, findByText } = render(<EditProfileScreen />);

    await waitFor(() => {
      expect(mockGetProfile).toHaveBeenCalled();
    });

    fireEvent.press(getByTestId('save-profile-button'));

    expect(await findByText('Email is already taken.')).toBeTruthy();
  });

  it('shows email-taken message on 409 conflict from profile save', async () => {
    mockUpdateProfile.mockRejectedValueOnce({
      isAxiosError: true,
      response: { status: 409, data: { error: 'Email is already taken.' } },
    });

    const { getByTestId, findByText, queryByText } = render(
      <EditProfileScreen />,
    );

    await waitFor(() => {
      expect(mockGetProfile).toHaveBeenCalled();
    });

    fireEvent.press(getByTestId('save-profile-button'));

    // Should show the email-taken field error
    expect(await findByText('settings.emailTaken')).toBeTruthy();
    // Should NOT show the generic error fallback
    expect(queryByText('common.error')).toBeNull();
  });

  it('handles prefill fetch failure gracefully', async () => {
    mockGetProfile.mockRejectedValueOnce(new Error('Network error'));

    const { getByTestId } = render(<EditProfileScreen />);

    // Should still render without crashing
    await waitFor(() => {
      expect(mockGetProfile).toHaveBeenCalled();
    });

    expect(getByTestId('save-profile-button')).toBeTruthy();
    expect(getByTestId('save-password-button')).toBeTruthy();
  });

  it('can be unmounted without errors during async profile save', async () => {
    let resolvePromise!: (value: unknown) => void;
    mockUpdateProfile.mockReturnValue(
      new Promise(resolve => {
        resolvePromise = resolve;
      }),
    );

    const consoleErrorSpy = jest
      .spyOn(console, 'error')
      .mockImplementation(() => {});

    const { getByTestId, unmount } = render(<EditProfileScreen />);

    await waitFor(() => {
      expect(mockGetProfile).toHaveBeenCalled();
    });

    fireEvent.press(getByTestId('save-profile-button'));

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

  it('displays field-level errors from ValidationProblemDetails on profile save', async () => {
    mockUpdateProfile.mockRejectedValueOnce({
      isAxiosError: true,
      response: {
        status: 400,
        data: {
          errors: {
            errors: [
              'DisplayName must be between 1 and 50 characters',
              'Email is not in a valid format',
            ],
          },
        },
      },
    });

    const { getByTestId, findByText } = render(<EditProfileScreen />);

    await waitFor(() => {
      expect(mockGetProfile).toHaveBeenCalled();
    });

    fireEvent.press(getByTestId('save-profile-button'));

    expect(
      await findByText('DisplayName must be between 1 and 50 characters'),
    ).toBeTruthy();
    expect(await findByText('Email is not in a valid format')).toBeTruthy();
  });
});
