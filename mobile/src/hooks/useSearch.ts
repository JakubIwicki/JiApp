import { useState, useCallback, useEffect, useRef } from 'react';
import { searchVideos } from '../services/searchService';
import { getErrorMessage } from '../utils/errorUtils';
import type { VideoItem } from '../types/api';

interface UseSearchResult {
  results: VideoItem[];
  isLoading: boolean;
  isLoadingMore: boolean;
  error: string | null;
  hasMore: boolean;
  search: (query: string) => Promise<void>;
  loadMore: () => Promise<void>;
  clearResults: () => void;
}

const useSearch = (): UseSearchResult => {
  const [results, setResults] = useState<VideoItem[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(false);
  const abortRef = useRef<AbortController | null>(null);
  const currentQueryRef = useRef<string>('');
  const currentPageRef = useRef<number>(0);

  useEffect(() => {
    const controller = abortRef.current;
    return () => {
      controller?.abort();
    };
  }, [abortRef]);

  const search = useCallback(async (query: string) => {
    abortRef.current?.abort();

    const controller = new AbortController();
    abortRef.current = controller;

    currentQueryRef.current = query;
    currentPageRef.current = 0;

    setIsLoading(true);
    setIsLoadingMore(false);
    setError(null);

    try {
      const response = await searchVideos(query, 0, controller.signal);
      setResults(response.results);
      setHasMore(response.hasMore);
    } catch (err) {
      if (err instanceof Error && err.name === 'AbortError') {
        return;
      }
      setError(getErrorMessage(err, 'Search failed'));
      setResults([]);
      setHasMore(false);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const loadMore = useCallback(async () => {
    if (isLoading || isLoadingMore || !hasMore) {
      return;
    }

    const nextPage = currentPageRef.current + 1;

    setIsLoadingMore(true);

    try {
      const response = await searchVideos(
        currentQueryRef.current,
        nextPage,
        abortRef.current?.signal,
      );
      setResults(prev => [...prev, ...response.results]);
      setHasMore(response.hasMore);
      currentPageRef.current = nextPage;
    } catch (err) {
      if (err instanceof Error && err.name === 'AbortError') {
        return;
      }
      setHasMore(false);
      setError(getErrorMessage(err, 'Search failed'));
    } finally {
      setIsLoadingMore(false);
    }
  }, [isLoading, isLoadingMore, hasMore, abortRef]);

  const clearResults = useCallback(() => {
    setResults([]);
    setError(null);
    setHasMore(false);
  }, []);

  return {
    results,
    isLoading,
    isLoadingMore,
    error,
    hasMore,
    search,
    loadMore,
    clearResults,
  };
};

export default useSearch;
