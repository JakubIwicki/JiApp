import { useState, useCallback, useRef, useEffect } from 'react';
import { getHistory } from '../services/historyService';
import { getErrorMessage } from '../utils/errorUtils';
import type { SearchHistoryItem, DownloadHistoryItem } from '../types/api';

interface UseHistoryResult {
  searches: SearchHistoryItem[];
  downloads: DownloadHistoryItem[];
  isLoading: boolean;
  error: string | null;
  loadHistory: (limit?: number) => Promise<void>;
  refresh: () => Promise<void>;
}

const useHistory = (): UseHistoryResult => {
  const [searches, setSearches] = useState<SearchHistoryItem[]>([]);
  const [downloads, setDownloads] = useState<DownloadHistoryItem[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const currentLimitRef = useRef<number | undefined>(undefined);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    return () => {
      abortRef.current?.abort();
    };
  }, []);

  const loadHistory = useCallback(async (limit?: number) => {
    // Cancel any previous in-flight request
    abortRef.current?.abort();

    const controller = new AbortController();
    abortRef.current = controller;

    setIsLoading(true);
    setError(null);
    currentLimitRef.current = limit;

    try {
      const response = await getHistory(limit, controller.signal);
      setSearches(response.searches);
      setDownloads(response.downloads);
    } catch (err) {
      if (err instanceof Error && err.name === 'AbortError') {
        return;
      }
      setError(getErrorMessage(err, 'Failed to load history'));
      setSearches([]);
      setDownloads([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const refresh = useCallback(async () => {
    await loadHistory(currentLimitRef.current ?? 50);
  }, [loadHistory]);

  return {
    searches,
    downloads,
    isLoading,
    error,
    loadHistory,
    refresh,
  };
};

export default useHistory;
