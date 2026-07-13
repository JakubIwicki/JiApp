import { useState, useCallback, useRef, useEffect } from 'react';
import * as adminService from '../services/adminService';
import { getFriendlyErrorMessage } from '../../../utils/errorUtils';
import type { UserSummary } from '../types/api';

interface UseUsersResult {
  users: UserSummary[];
  total: number;
  isLoading: boolean;
  error: string | null;
  search: (query: string) => void;
  loadPage: (page: number) => void;
  createUser: (
    data: import('../types/api').CreateUserRequest,
  ) => Promise<number | undefined>;
  disableUser: (userId: number) => Promise<void>;
  enableUser: (userId: number) => Promise<void>;
  deleteUser: (userId: number) => Promise<void>;
  refresh: () => Promise<void>;
}

const useUsers = (): UseUsersResult => {
  const [users, setUsers] = useState<UserSummary[]>([]);
  const [total, setTotal] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    const controller = abortRef.current;
    return () => {
      controller?.abort();
    };
  }, [abortRef]);

  const fetchUsers = useCallback(async (query: string, page: number) => {
    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    setIsLoading(true);
    setError(null);

    try {
      const data = await adminService.listUsers(query || undefined, page, 20);
      if (controller.signal.aborted) return;
      setUsers(data.items);
      setTotal(data.total);
    } catch (err) {
      if (err instanceof Error && err.name === 'AbortError') return;
      setError(getFriendlyErrorMessage(err, 'Failed to load users'));
      setUsers([]);
      setTotal(0);
    } finally {
      if (!controller.signal.aborted) {
        setIsLoading(false);
      }
    }
  }, []);

  useEffect(() => {
    fetchUsers(searchQuery, currentPage);
  }, [fetchUsers, searchQuery, currentPage]);

  const search = useCallback((query: string) => {
    setSearchQuery(query);
    setCurrentPage(1);
  }, []);

  const loadPage = useCallback((page: number) => {
    setCurrentPage(page);
  }, []);

  const refresh = useCallback(async () => {
    await fetchUsers(searchQuery, currentPage);
  }, [fetchUsers, searchQuery, currentPage]);

  const createUser = useCallback(
    async (
      data: import('../types/api').CreateUserRequest,
    ): Promise<number | undefined> => {
      setError(null);
      try {
        const result = await adminService.createUser(data);
        await refresh();
        return result;
      } catch (err) {
        setError(getFriendlyErrorMessage(err, 'Failed to create user'));
        throw err;
      }
    },
    [refresh],
  );

  const handleDisableUser = useCallback(async (userId: number) => {
    setError(null);
    try {
      await adminService.disableUser(userId);
      setUsers(prev =>
        prev.map(u => (u.id === userId ? { ...u, isLockedOut: true } : u)),
      );
    } catch (err) {
      setError(getFriendlyErrorMessage(err, 'Failed to disable user'));
      throw err;
    }
  }, []);

  const handleEnableUser = useCallback(async (userId: number) => {
    setError(null);
    try {
      await adminService.enableUser(userId);
      setUsers(prev =>
        prev.map(u => (u.id === userId ? { ...u, isLockedOut: false } : u)),
      );
    } catch (err) {
      setError(getFriendlyErrorMessage(err, 'Failed to enable user'));
      throw err;
    }
  }, []);

  const handleDeleteUser = useCallback(async (userId: number) => {
    setError(null);
    try {
      await adminService.deleteUser(userId);
      setUsers(prev => prev.filter(u => u.id !== userId));
      setTotal(prev => prev - 1);
    } catch (err) {
      setError(getFriendlyErrorMessage(err, 'Failed to delete user'));
      throw err;
    }
  }, []);

  return {
    users,
    total,
    isLoading,
    error,
    search,
    loadPage,
    createUser,
    disableUser: handleDisableUser,
    enableUser: handleEnableUser,
    deleteUser: handleDeleteUser,
    refresh,
  };
};

export default useUsers;
