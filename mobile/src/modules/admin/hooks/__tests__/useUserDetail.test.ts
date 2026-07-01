import { renderHook, act } from '@testing-library/react-native';
import useUserDetail from '../useUserDetail';

jest.mock('../../services/adminService');

import * as adminService from '../../services/adminService';
const mockService = adminService as jest.Mocked<typeof adminService>;

const makeUserDetail = (id: number) => ({
  id,
  username: `user${id}`,
  email: `user${id}@example.com`,
  displayName: `User ${id}`,
  roles: ['User'],
  isLockedOut: false,
  lockoutEnd: null,
});

describe('useUserDetail', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('initial state has user=null, isLoading=true, error=null', () => {
    mockService.getUser.mockResolvedValue(makeUserDetail(1));

    const { result } = renderHook(() => useUserDetail(1));

    expect(result.current.user).toBeNull();
    expect(result.current.isLoading).toBe(true);
    expect(result.current.error).toBeNull();
  });

  it('loads user on mount', async () => {
    const detail = makeUserDetail(1);
    mockService.getUser.mockResolvedValue(detail);

    const { result } = renderHook(() => useUserDetail(1));

    await act(async () => {
      await Promise.resolve();
    });

    expect(result.current.user).toEqual(detail);
    expect(result.current.isLoading).toBe(false);
  });

  it('sets error on failure', async () => {
    mockService.getUser.mockRejectedValue(new Error('Not found'));

    const { result } = renderHook(() => useUserDetail(1));

    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(result.current.error).toBe('Not found');
    expect(result.current.isLoading).toBe(false);
  });

  it('assignRole calls service and refetches', async () => {
    mockService.getUser.mockResolvedValue(makeUserDetail(1));
    mockService.assignRole.mockResolvedValue(undefined);

    const { result } = renderHook(() => useUserDetail(1));

    await act(async () => {
      await Promise.resolve();
    });

    const updated = {
      ...makeUserDetail(1),
      roles: ['User', 'Admin'],
    };
    mockService.getUser.mockResolvedValue(updated);

    await act(async () => {
      await result.current.assignRole('Admin');
    });

    expect(mockService.assignRole).toHaveBeenCalledWith(1, {
      roleName: 'Admin',
    });
    expect(result.current.user?.roles).toEqual(['User', 'Admin']);
  });

  it('removeRole calls service and refetches', async () => {
    const detail = { ...makeUserDetail(1), roles: ['User', 'Admin'] };
    mockService.getUser.mockResolvedValue(detail);
    mockService.removeRole.mockResolvedValue(undefined);

    const { result } = renderHook(() => useUserDetail(1));

    await act(async () => {
      await Promise.resolve();
    });

    const updated = { ...makeUserDetail(1), roles: ['User'] };
    mockService.getUser.mockResolvedValue(updated);

    await act(async () => {
      await result.current.removeRole('Admin');
    });

    expect(mockService.removeRole).toHaveBeenCalledWith(1, 'Admin');
    expect(result.current.user?.roles).toEqual(['User']);
  });

  it('resetPassword calls service', async () => {
    mockService.getUser.mockResolvedValue(makeUserDetail(1));
    mockService.resetPassword.mockResolvedValue(undefined);

    const { result } = renderHook(() => useUserDetail(1));

    await act(async () => {
      await Promise.resolve();
    });

    await act(async () => {
      await result.current.resetPassword('newpass');
    });

    expect(mockService.resetPassword).toHaveBeenCalledWith(1, {
      newPassword: 'newpass',
    });
  });

  it('disableUser calls service and refetches', async () => {
    mockService.getUser.mockResolvedValue(makeUserDetail(1));
    mockService.disableUser.mockResolvedValue(undefined);

    const { result } = renderHook(() => useUserDetail(1));

    await act(async () => {
      await Promise.resolve();
    });

    const updated = { ...makeUserDetail(1), isLockedOut: true };
    mockService.getUser.mockResolvedValue(updated);

    await act(async () => {
      await result.current.disableUser();
    });

    expect(mockService.disableUser).toHaveBeenCalledWith(1);
    expect(result.current.user?.isLockedOut).toBe(true);
  });
});
