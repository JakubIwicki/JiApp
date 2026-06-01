import { useState, useCallback, useRef, useEffect } from 'react';
import { getHistory } from '../services/historyService';
import { archiveSearchHistory } from '../services/searchService';
import { archiveDownload as archiveDownloadService } from '../services/downloadService';
import { getErrorMessage } from '../utils/errorUtils';
import useToast from './useToast';
import type { SearchHistoryItem, DownloadHistoryItem } from '../types/api';

interface UseHistoryResult {
  searches: SearchHistoryItem[];
  downloads: DownloadHistoryItem[];
  isLoading: boolean;
  error: string | null;
  loadHistory: (limit?: number) => Promise<void>;
  refresh: () => Promise<void>;
  archiveSearch: (id: number) => Promise<void>;
  archiveDownload: (id: number) => Promise<void>;
}

const useHistory = (): UseHistoryResult => {
  const [searches, setSearches] = useState<SearchHistoryItem[]>([]);
  const [downloads, setDownloads] = useState<DownloadHistoryItem[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const currentLimitRef = useRef<number | undefined>(undefined);
  const abortRef = useRef<AbortController | null>(null);
  const { showSuccess, showError } = useToast();

  useEffect(() => {
    const controller = abortRef.current;
    return () => {
      controller?.abort();
    };
  }, [abortRef]);

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

  const archiveSearch = useCallback(async (id: number) => {
    const previous = searches;
    setSearches((prev) => prev.filter((item) => item.id !== id));
    try {
      await archiveSearchHistory(id);
      showSuccess('toast.searchArchived');
    } catch {
      showError('toast.archiveFailed');
      setSearches(previous);
    }
  }, [searches, showSuccess, showError]);

  const archiveDownload = useCallback(async (id: number) => {
    const previous = downloads;
    setDownloads((prev) => prev.filter((item) => item.id !== id));
    try {
      await archiveDownloadService(id);
      showSuccess('toast.downloadArchived');
    } catch {
      showError('toast.archiveFailed');
      setDownloads(previous);
    }
  }, [downloads, showSuccess, showError]);

  return {
    searches,
    downloads,
    isLoading,
    error,
    loadHistory,
    refresh,
    archiveSearch,
    archiveDownload,
  };
};

export default useHistory;
