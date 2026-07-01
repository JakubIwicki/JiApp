import { renderHook, act } from '@testing-library/react-native';
import useUsers from '../useUsers';

jest.mock('../../services/adminService');

import * as adminService from '../../services/adminService';
const mockService = adminService as jest.Mocked<typeof adminService>;

const makeUserSummary = (id: number) => ({
  id,
  username: `user${id}`,
  email: `user${id}@example.com`,
  displayName: `User ${id}`,
  roles: ['User'],
  isLockedOut: false,
});

describe('useUsers', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('initial state has empty users, isLoading=true, error=null', () => {
    mockService.listUsers.mockResolvedValue({
      items: [],
      total: 0,
    });

    const { result } = renderHook(() => useUsers());

    expect(result.current.users).toEqual([]);
    expect(result.current.total).toBe(0);
    expect(result.current.isLoading).toBe(true);
    expect(result.current.error).toBeNull();
  });

  it('loads users on mount', async () => {
    const items = [makeUserSummary(1), makeUserSummary(2)];
    mockService.listUsers.mockResolvedValue({ items, total: 2 });

    const { result } = renderHook(() => useUsers());

    await act(async () => {
      await Promise.resolve();
    });

    expect(result.current.users).toEqual(items);
    expect(result.current.total).toBe(2);
    expect(result.current.isLoading).toBe(false);
  });

  it('sets error on failure', async () => {
    mockService.listUsers.mockRejectedValue(new Error('Network error'));

    const { result } = renderHook(() => useUsers());

    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(result.current.error).toBe('Network error');
    expect(result.current.isLoading).toBe(false);
  });

  it('search updates query and resets page', async () => {
    mockService.listUsers.mockResolvedValue({ items: [], total: 0 });

    const { result } = renderHook(() => useUsers());

    await act(async () => {
      await Promise.resolve();
    });

    mockService.listUsers.mockResolvedValue({
      items: [makeUserSummary(3)],
      total: 1,
    });

    await act(async () => {
      result.current.search('test');
    });

    await act(async () => {
      await Promise.resolve();
    });

    expect(mockService.listUsers).toHaveBeenLastCalledWith('test', 1, 20);
  });

  it('deleteUser removes user from list', async () => {
    const items = [makeUserSummary(1), makeUserSummary(2)];
    mockService.listUsers.mockResolvedValue({ items, total: 2 });
    mockService.deleteUser.mockResolvedValue(undefined);

    const { result } = renderHook(() => useUsers());

    await act(async () => {
      await Promise.resolve();
    });

    await act(async () => {
      await result.current.deleteUser(1);
    });

    expect(result.current.users).toHaveLength(1);
    expect(result.current.users[0].id).toBe(2);
    expect(result.current.total).toBe(1);
  });

  it('disableUser sets isLockedOut to true for that user', async () => {
    const items = [makeUserSummary(1)];
    mockService.listUsers.mockResolvedValue({ items, total: 1 });
    mockService.disableUser.mockResolvedValue(undefined);

    const { result } = renderHook(() => useUsers());

    await act(async () => {
      await Promise.resolve();
    });

    await act(async () => {
      await result.current.disableUser(1);
    });

    expect(result.current.users[0].isLockedOut).toBe(true);
  });

  it('enableUser sets isLockedOut to false for that user', async () => {
    const items = [{ ...makeUserSummary(1), isLockedOut: true }];
    mockService.listUsers.mockResolvedValue({ items, total: 1 });
    mockService.enableUser.mockResolvedValue(undefined);

    const { result } = renderHook(() => useUsers());

    await act(async () => {
      await Promise.resolve();
    });

    await act(async () => {
      await result.current.enableUser(1);
    });

    expect(result.current.users[0].isLockedOut).toBe(false);
  });
});
