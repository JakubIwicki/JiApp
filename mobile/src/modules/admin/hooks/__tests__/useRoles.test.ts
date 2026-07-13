import { renderHook, act } from '@testing-library/react-native';
import useRoles from '../useRoles';

jest.mock('../../services/adminService');

import * as adminService from '../../services/adminService';
const mockService = adminService as jest.Mocked<typeof adminService>;

const makeRole = (name: string, permissions: string[] = []) => ({
  name,
  permissions,
});

describe('useRoles', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('initial state has empty roles, isLoading=true, error=null', () => {
    mockService.listRoles.mockResolvedValue([]);

    const { result } = renderHook(() => useRoles());

    expect(result.current.roles).toEqual([]);
    expect(result.current.isLoading).toBe(true);
    expect(result.current.error).toBeNull();
  });

  it('loads roles on mount', async () => {
    const roles = [makeRole('Admin', ['users.manage']), makeRole('User')];
    mockService.listRoles.mockResolvedValue(roles);

    const { result } = renderHook(() => useRoles());

    await act(async () => {
      await Promise.resolve();
    });

    expect(result.current.roles).toEqual(roles);
    expect(result.current.isLoading).toBe(false);
  });

  it('sets error on failure', async () => {
    mockService.listRoles.mockRejectedValue(new Error('Server error'));

    const { result } = renderHook(() => useRoles());

    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(result.current.error).toBe('Failed to load roles');
    expect(result.current.isLoading).toBe(false);
  });

  it('createRole calls service and refetches', async () => {
    mockService.listRoles.mockResolvedValue([]);
    mockService.createRole.mockResolvedValue(undefined);

    const { result } = renderHook(() => useRoles());

    await act(async () => {
      await Promise.resolve();
    });

    const newRoles = [makeRole('Moderator', ['users.manage'])];
    mockService.listRoles.mockResolvedValue(newRoles);

    await act(async () => {
      await result.current.createRole({
        name: 'Moderator',
        permissions: ['users.manage'],
      });
    });

    expect(mockService.createRole).toHaveBeenCalledWith({
      name: 'Moderator',
      permissions: ['users.manage'],
    });
    expect(result.current.roles).toEqual(newRoles);
  });

  it('updatePermissions calls service and updates locally', async () => {
    const roles = [makeRole('User', [])];
    mockService.listRoles.mockResolvedValue(roles);
    mockService.updateRolePermissions.mockResolvedValue(undefined);

    const { result } = renderHook(() => useRoles());

    await act(async () => {
      await Promise.resolve();
    });

    await act(async () => {
      await result.current.updatePermissions('User', ['scheduler.access']);
    });

    expect(mockService.updateRolePermissions).toHaveBeenCalledWith('User', {
      permissions: ['scheduler.access'],
    });
    expect(result.current.roles[0].permissions).toEqual(['scheduler.access']);
  });

  it('deleteRole calls service and removes from list', async () => {
    const roles = [makeRole('Admin'), makeRole('Custom')];
    mockService.listRoles.mockResolvedValue(roles);
    mockService.deleteRole.mockResolvedValue(undefined);

    const { result } = renderHook(() => useRoles());

    await act(async () => {
      await Promise.resolve();
    });

    await act(async () => {
      await result.current.deleteRole('Custom');
    });

    expect(mockService.deleteRole).toHaveBeenCalledWith('Custom');
    expect(result.current.roles).toHaveLength(1);
    expect(result.current.roles[0].name).toBe('Admin');
  });
});
