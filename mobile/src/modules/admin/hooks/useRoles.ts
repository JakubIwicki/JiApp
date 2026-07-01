import { useState, useCallback, useRef, useEffect } from 'react';
import * as adminService from '../services/adminService';
import type { RoleSummary } from '../types/api';

interface UseRolesResult {
  roles: RoleSummary[];
  isLoading: boolean;
  error: string | null;
  createRole: (data: import('../types/api').CreateRoleRequest) => Promise<void>;
  updatePermissions: (roleName: string, permissions: string[]) => Promise<void>;
  deleteRole: (roleName: string) => Promise<void>;
  refresh: () => Promise<void>;
}

const useRoles = (): UseRolesResult => {
  const [roles, setRoles] = useState<RoleSummary[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    const controller = abortRef.current;
    return () => {
      controller?.abort();
    };
  }, [abortRef]);

  const fetchRoles = useCallback(async () => {
    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    setIsLoading(true);
    setError(null);

    try {
      const data = await adminService.listRoles();
      if (controller.signal.aborted) return;
      setRoles(data);
    } catch (err) {
      if (err instanceof Error && err.name === 'AbortError') return;
      setError(err instanceof Error ? err.message : 'Failed to load roles');
      setRoles([]);
    } finally {
      if (!controller.signal.aborted) {
        setIsLoading(false);
      }
    }
  }, []);

  useEffect(() => {
    fetchRoles();
  }, [fetchRoles]);

  const refresh = useCallback(async () => {
    await fetchRoles();
  }, [fetchRoles]);

  const createRole = useCallback(
    async (data: import('../types/api').CreateRoleRequest): Promise<void> => {
      setError(null);
      try {
        await adminService.createRole(data);
        await fetchRoles();
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to create role');
        throw err;
      }
    },
    [fetchRoles],
  );

  const updatePermissions = useCallback(
    async (roleName: string, permissions: string[]): Promise<void> => {
      setError(null);
      try {
        await adminService.updateRolePermissions(roleName, {
          permissions,
        });
        setRoles(prev =>
          prev.map(r => (r.name === roleName ? { ...r, permissions } : r)),
        );
      } catch (err) {
        setError(
          err instanceof Error ? err.message : 'Failed to update permissions',
        );
        throw err;
      }
    },
    [],
  );

  const handleDeleteRole = useCallback(async (roleName: string) => {
    setError(null);
    try {
      await adminService.deleteRole(roleName);
      setRoles(prev => prev.filter(r => r.name !== roleName));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete role');
      throw err;
    }
  }, []);

  return {
    roles,
    isLoading,
    error,
    createRole,
    updatePermissions,
    deleteRole: handleDeleteRole,
    refresh,
  };
};

export default useRoles;
