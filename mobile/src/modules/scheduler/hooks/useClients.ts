import { useState, useCallback, useRef, useEffect } from 'react';
import * as clientService from '../services/clientService';
import type { Client } from '../types/api';

interface UseClientsResult {
  clients: Client[];
  isLoading: boolean;
  error: string | null;
  searchClients: (q: string) => Promise<void>;
  loadAll: () => Promise<void>;
  addClient: (data: { name: string; phone?: string; notes?: string }) => Promise<number | undefined>;
  removeClient: (id: number) => Promise<void>;
}

const useClients = (boardId: number): UseClientsResult => {
  const [clients, setClients] = useState<Client[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    const controller = abortRef.current;
    return () => {
      controller?.abort();
    };
  }, [abortRef]);

  const fetchClients = useCallback(async (q?: string) => {
    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    setIsLoading(true);
    setError(null);

    try {
      const data = await clientService.listClients(boardId, q);
      setClients(data);
    } catch (err) {
      if (err instanceof Error && err.name === 'AbortError') return;
      setError(err instanceof Error ? err.message : 'Failed to load clients');
      setClients([]);
    } finally {
      setIsLoading(false);
    }
  }, [boardId]);

  const searchClients = useCallback(
    async (q: string) => fetchClients(q),
    [fetchClients],
  );

  const loadAll = useCallback(
    async () => fetchClients(undefined),
    [fetchClients],
  );

  const addClient = useCallback(
    async (data: {
      name: string;
      phone?: string;
      notes?: string;
    }): Promise<number | undefined> => {
      setError(null);
      try {
        const result = await clientService.createClient(boardId, data);
        return result.id;
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to create client');
        throw err;
      }
    },
    [boardId],
  );

  const removeClient = useCallback(async (id: number) => {
    setError(null);
    try {
      await clientService.deleteClient(id);
      setClients((prev) => prev.filter((c) => c.id !== id));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete client');
      throw err;
    }
  }, []);

  return {
    clients,
    isLoading,
    error,
    searchClients,
    loadAll,
    addClient,
    removeClient,
  };
};

export default useClients;
