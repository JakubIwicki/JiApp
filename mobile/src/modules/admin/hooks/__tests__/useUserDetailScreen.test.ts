import { renderHook, act } from '@testing-library/react-native';
import { Alert } from 'react-native';
import useUserDetailScreen from '../useUserDetailScreen';

const mockShowSuccess = jest.fn();
const mockShowError = jest.fn();
const mockGoBack = jest.fn();

jest.mock('../../../../hooks/useToast', () => ({
  __esModule: true,
  default: () => ({
    showSuccess: mockShowSuccess,
    showError: mockShowError,
    showInfo: jest.fn(),
    showWarning: jest.fn(),
  }),
}));

jest.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

jest.mock('@react-navigation/native', () => {
  const actual = jest.requireActual('@react-navigation/native');
  return {
    ...actual,
    useNavigation: () => ({
      navigate: jest.fn(),
      goBack: mockGoBack,
      setOptions: jest.fn(),
    }),
  };
});

jest.mock('../../services/adminService');

import * as adminService from '../../services/adminService';
const mockService = adminService as jest.Mocked<typeof adminService>;

const alertSpy = jest.spyOn(Alert, 'alert').mockImplementation(() => {});

type AlertButton = {
  text?: string;
  style?: 'default' | 'cancel' | 'destructive';
  onPress?: () => void;
};

const getConfirmButton = (): AlertButton | undefined => {
  const buttons = alertSpy.mock.calls[0]?.[2] as AlertButton[] | undefined;
  return (
    buttons?.find(button => button.style === 'destructive') ??
    buttons?.[buttons.length - 1]
  );
};

interface UserDetailFixture {
  id: number;
  username: string;
  email: string;
  displayName: string;
  roles: string[];
  isLockedOut: boolean;
  lockoutEnd: string | null;
}

const makeUserDetail = (
  overrides: Partial<UserDetailFixture> = {},
): UserDetailFixture => ({
  id: 1,
  username: 'user1',
  email: 'user1@example.com',
  displayName: 'User 1',
  roles: ['User'],
  isLockedOut: false,
  lockoutEnd: null,
  ...overrides,
});

const flushAsync = async () => {
  await act(async () => {
    await Promise.resolve();
    await Promise.resolve();
  });
};

describe('useUserDetailScreen', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    alertSpy.mockClear();
    mockService.getUser.mockResolvedValue(makeUserDetail());
    mockService.listRoles.mockResolvedValue([
      { name: 'Admin', permissions: [] },
      { name: 'User', permissions: [] },
    ]);
    mockService.assignRole.mockResolvedValue(undefined);
    mockService.removeRole.mockResolvedValue(undefined);
    mockService.resetPassword.mockResolvedValue(undefined);
    mockService.disableUser.mockResolvedValue(undefined);
    mockService.enableUser.mockResolvedValue(undefined);
    mockService.deleteUser.mockResolvedValue(undefined);
  });

  it('initial state has user=null, isLoading=true, closed toggles and empty password', () => {
    const { result } = renderHook(() => useUserDetailScreen(1));

    expect(result.current.user).toBeNull();
    expect(result.current.isLoading).toBe(true);
    expect(result.current.showAssignRole).toBe(false);
    expect(result.current.showResetPassword).toBe(false);
    expect(result.current.newPassword).toBe('');
    expect(result.current.availableRoles).toEqual([]);
  });

  it('loads user and roles on mount, excluding already-assigned roles', async () => {
    mockService.getUser.mockResolvedValue(
      makeUserDetail({ roles: ['User', 'Admin'] }),
    );
    mockService.listRoles.mockResolvedValue([
      { name: 'Admin', permissions: [] },
      { name: 'User', permissions: [] },
      { name: 'Moderator', permissions: [] },
    ]);

    const { result } = renderHook(() => useUserDetailScreen(1));

    await flushAsync();

    expect(result.current.user).toEqual(
      makeUserDetail({ roles: ['User', 'Admin'] }),
    );
    expect(result.current.isLoading).toBe(false);
    expect(result.current.availableRoles.map(role => role.name)).toEqual([
      'Moderator',
    ]);
  });

  it('sets error on load failure and keeps availableRoles empty', async () => {
    mockService.getUser.mockRejectedValue(new Error('Not found'));

    const { result } = renderHook(() => useUserDetailScreen(1));

    await flushAsync();

    expect(result.current.error).toBe('Failed to load user');
    expect(result.current.user).toBeNull();
    expect(result.current.availableRoles).toEqual([]);
  });

  it('assignRole closes the picker and shows a success toast', async () => {
    const { result } = renderHook(() => useUserDetailScreen(1));

    await flushAsync();

    act(() => {
      result.current.setShowAssignRole(true);
    });

    await act(async () => {
      await result.current.handleAssignRole('Admin');
    });

    expect(mockService.assignRole).toHaveBeenCalledWith(1, {
      roleName: 'Admin',
    });
    expect(result.current.showAssignRole).toBe(false);
    expect(mockShowSuccess).toHaveBeenCalledWith(
      'admin.userDetail.roleAssigned',
    );
  });

  it('assignRole failure shows an error toast and keeps the picker open', async () => {
    mockService.assignRole.mockRejectedValue(new Error('Forbidden'));

    const { result } = renderHook(() => useUserDetailScreen(1));

    await flushAsync();

    act(() => {
      result.current.setShowAssignRole(true);
    });

    await act(async () => {
      await result.current.handleAssignRole('Admin');
    });

    expect(mockShowError).toHaveBeenCalledWith(
      'admin.userDetail.assignRoleError',
    );
    expect(result.current.showAssignRole).toBe(true);
  });

  it('removeRole confirms first, then calls the service on confirm', async () => {
    const { result } = renderHook(() => useUserDetailScreen(1));

    await flushAsync();

    act(() => {
      result.current.handleRemoveRole('Admin');
    });

    expect(alertSpy).toHaveBeenCalled();
    expect(mockService.removeRole).not.toHaveBeenCalled();

    await act(async () => {
      getConfirmButton()?.onPress?.();
    });

    expect(mockService.removeRole).toHaveBeenCalledWith(1, 'Admin');
  });

  it('removeRole failure shows an error toast', async () => {
    mockService.removeRole.mockRejectedValue(new Error('Forbidden'));

    const { result } = renderHook(() => useUserDetailScreen(1));

    await flushAsync();

    act(() => {
      result.current.handleRemoveRole('Admin');
    });

    await act(async () => {
      getConfirmButton()?.onPress?.();
    });

    expect(mockShowError).toHaveBeenCalledWith(
      'admin.userDetail.removeRoleError',
    );
  });

  it('toggleLock on an active user confirms, then disables the user', async () => {
    mockService.getUser.mockResolvedValue(makeUserDetail());

    const { result } = renderHook(() => useUserDetailScreen(1));

    await flushAsync();

    act(() => {
      result.current.handleToggleLock();
    });

    expect(alertSpy).toHaveBeenCalledWith(
      'admin.userDetail.title',
      'admin.userDetail.disableConfirm',
      expect.any(Array),
    );

    await act(async () => {
      getConfirmButton()?.onPress?.();
    });

    expect(mockService.disableUser).toHaveBeenCalledWith(1);
    expect(mockService.enableUser).not.toHaveBeenCalled();
  });

  it('toggleLock on a locked user enables the user', async () => {
    mockService.getUser.mockResolvedValue(
      makeUserDetail({ isLockedOut: true, lockoutEnd: '2026-08-10T00:00:00Z' }),
    );

    const { result } = renderHook(() => useUserDetailScreen(1));

    await flushAsync();

    act(() => {
      result.current.handleToggleLock();
    });

    await act(async () => {
      getConfirmButton()?.onPress?.();
    });

    expect(mockService.enableUser).toHaveBeenCalledWith(1);
    expect(mockService.disableUser).not.toHaveBeenCalled();
  });

  it('toggleLock failure shows an error toast', async () => {
    mockService.disableUser.mockRejectedValue(new Error('Backend down'));

    const { result } = renderHook(() => useUserDetailScreen(1));

    await flushAsync();

    act(() => {
      result.current.handleToggleLock();
    });

    await act(async () => {
      getConfirmButton()?.onPress?.();
    });

    expect(mockShowError).toHaveBeenCalledWith(
      'admin.userDetail.toggleLockError',
    );
  });

  it('deleteUser confirms, then deletes and navigates back', async () => {
    const { result } = renderHook(() => useUserDetailScreen(1));

    await flushAsync();

    act(() => {
      result.current.handleDelete();
    });

    expect(mockService.deleteUser).not.toHaveBeenCalled();

    await act(async () => {
      getConfirmButton()?.onPress?.();
    });

    expect(mockService.deleteUser).toHaveBeenCalledWith(1);
    expect(mockGoBack).toHaveBeenCalled();
  });

  it('deleteUser failure shows an error toast and does not navigate back', async () => {
    mockService.deleteUser.mockRejectedValue(new Error('Backend down'));

    const { result } = renderHook(() => useUserDetailScreen(1));

    await flushAsync();

    act(() => {
      result.current.handleDelete();
    });

    await act(async () => {
      getConfirmButton()?.onPress?.();
    });

    expect(mockShowError).toHaveBeenCalledWith('admin.userDetail.deleteError');
    expect(mockGoBack).not.toHaveBeenCalled();
  });

  it('resetPassword with an empty password does nothing', async () => {
    const { result } = renderHook(() => useUserDetailScreen(1));

    await flushAsync();

    await act(async () => {
      await result.current.handleResetPassword();
    });

    expect(mockService.resetPassword).not.toHaveBeenCalled();
    expect(mockShowSuccess).not.toHaveBeenCalled();
  });

  it('resetPassword resets the password, closes the form and shows a success toast', async () => {
    const { result } = renderHook(() => useUserDetailScreen(1));

    await flushAsync();

    act(() => {
      result.current.setShowResetPassword(true);
      result.current.setNewPassword('  newpass  ');
    });

    await act(async () => {
      await result.current.handleResetPassword();
    });

    expect(mockService.resetPassword).toHaveBeenCalledWith(1, {
      newPassword: 'newpass',
    });
    expect(result.current.showResetPassword).toBe(false);
    expect(result.current.newPassword).toBe('');
    expect(mockShowSuccess).toHaveBeenCalledWith(
      'admin.userDetail.passwordReset',
    );
  });

  it('resetPassword failure shows an error toast', async () => {
    mockService.resetPassword.mockRejectedValue(new Error('Weak password'));

    const { result } = renderHook(() => useUserDetailScreen(1));

    await flushAsync();

    act(() => {
      result.current.setNewPassword('newpass');
    });

    await act(async () => {
      await result.current.handleResetPassword();
    });

    expect(mockShowError).toHaveBeenCalledWith(
      'admin.userDetail.passwordResetError',
    );
  });

  it('cancelResetPassword closes the form and clears the password', async () => {
    const { result } = renderHook(() => useUserDetailScreen(1));

    await flushAsync();

    act(() => {
      result.current.setShowResetPassword(true);
      result.current.setNewPassword('newpass');
      result.current.cancelResetPassword();
    });

    expect(result.current.showResetPassword).toBe(false);
    expect(result.current.newPassword).toBe('');
  });
});
