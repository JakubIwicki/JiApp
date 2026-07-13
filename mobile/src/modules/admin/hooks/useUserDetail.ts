import { useState, useCallback, useRef, useEffect } from 'react';
import * as adminService from '../services/adminService';
import { getFriendlyErrorMessage } from '../../../utils/errorUtils';
import type { UserDetail } from '../types/api';

interface UseUserDetailResult {
  user: UserDetail | null;
  isLoading: boolean;
  error: string | null;
  assignRole: (roleName: string) => Promise<void>;
  removeRole: (roleName: string) => Promise<void>;
  resetPassword: (newPassword: string) => Promise<void>;
  disableUser: () => Promise<void>;
  enableUser: () => Promise<void>;
  deleteUser: () => Promise<void>;
  refresh: () => Promise<void>;
}

const useUserDetail = (userId: number): UseUserDetailResult => {
  const [user, setUser] = useState<UserDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    const controller = abortRef.current;
    return () => {
      controller?.abort();
    };
  }, [abortRef]);

  const fetchUser = useCallback(async () => {
    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    setIsLoading(true);
    setError(null);

    try {
      const data = await adminService.getUser(userId);
      if (controller.signal.aborted) return;
      setUser(data);
    } catch (err) {
      if (err instanceof Error && err.name === 'AbortError') return;
      setError(getFriendlyErrorMessage(err, 'Failed to load user'));
      setUser(null);
    } finally {
      if (!controller.signal.aborted) {
        setIsLoading(false);
      }
    }
  }, [userId]);

  useEffect(() => {
    fetchUser();
  }, [fetchUser]);

  const refresh = useCallback(async () => {
    await fetchUser();
  }, [fetchUser]);

  const handleAssignRole = useCallback(
    async (roleName: string) => {
      setError(null);
      try {
        await adminService.assignRole(userId, { roleName });
        await fetchUser();
      } catch (err) {
        setError(getFriendlyErrorMessage(err, 'Failed to assign role'));
        throw err;
      }
    },
    [userId, fetchUser],
  );

  const handleRemoveRole = useCallback(
    async (roleName: string) => {
      setError(null);
      try {
        await adminService.removeRole(userId, roleName);
        await fetchUser();
      } catch (err) {
        setError(getFriendlyErrorMessage(err, 'Failed to remove role'));
        throw err;
      }
    },
    [userId, fetchUser],
  );

  const handleResetPassword = useCallback(
    async (newPassword: string) => {
      setError(null);
      try {
        await adminService.resetPassword(userId, { newPassword });
      } catch (err) {
        setError(getFriendlyErrorMessage(err, 'Failed to reset password'));
        throw err;
      }
    },
    [userId],
  );

  const handleDisableUser = useCallback(async () => {
    setError(null);
    try {
      await adminService.disableUser(userId);
      await fetchUser();
    } catch (err) {
      setError(getFriendlyErrorMessage(err, 'Failed to disable user'));
      throw err;
    }
  }, [userId, fetchUser]);

  const handleEnableUser = useCallback(async () => {
    setError(null);
    try {
      await adminService.enableUser(userId);
      await fetchUser();
    } catch (err) {
      setError(getFriendlyErrorMessage(err, 'Failed to enable user'));
      throw err;
    }
  }, [userId, fetchUser]);

  const handleDeleteUser = useCallback(async () => {
    setError(null);
    try {
      await adminService.deleteUser(userId);
    } catch (err) {
      setError(getFriendlyErrorMessage(err, 'Failed to delete user'));
      throw err;
    }
  }, [userId]);

  return {
    user,
    isLoading,
    error,
    assignRole: handleAssignRole,
    removeRole: handleRemoveRole,
    resetPassword: handleResetPassword,
    disableUser: handleDisableUser,
    enableUser: handleEnableUser,
    deleteUser: handleDeleteUser,
    refresh,
  };
};

export default useUserDetail;
