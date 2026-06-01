// NOTE: A simpler version exists at ../../../hooks/useSearch.ts without pagination support.
// These should be consolidated. See [ticket/issue reference].
import { useState, useCallback, useEffect, useRef } from 'react';
import { searchVideos } from '../services/searchService';
import { getErrorMessage } from '../../../utils/errorUtils';
import type { VideoItem } from '../types/api';
import { SEARCH_PAGE_SIZE } from '../../../constants/app';

interface UseSearchResult {
  results: VideoItem[];
  isLoading: boolean;
  isLoadingMore: boolean;
  error: string | null;
  hasMore: boolean;
  search: (query: string, maxResults?: number) => Promise<void>;
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
  const queryRef = useRef<string>('');
  const maxResultsRef = useRef<number>(SEARCH_PAGE_SIZE);
  const pageTokenRef = useRef<string | null>(null);

  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(() => {
    const controller = abortRef.current;
    return () => {
      controller?.abort();
    };
  }, []);

  const search = useCallback(
    async (query: string, maxResults?: number) => {
      abortRef.current?.abort();

      const controller = new AbortController();
      abortRef.current = controller;

      const effectiveMax = maxResults ?? SEARCH_PAGE_SIZE;

      queryRef.current = query;
      maxResultsRef.current = effectiveMax;
      pageTokenRef.current = null;

      setIsLoading(true);
      setIsLoadingMore(false);
      setError(null);

      try {
        const response = await searchVideos(
          query,
          effectiveMax,
          controller.signal,
          null,
        );
        setResults(response.results);
        pageTokenRef.current = response.nextPageToken;
        setHasMore(response.nextPageToken !== null);
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
    },
    [],
  );

  const loadMore = useCallback(async () => {
    if (isLoading || isLoadingMore || !hasMore || !queryRef.current) return;

    abortRef.current?.abort();

    const controller = new AbortController();
    abortRef.current = controller;

    setIsLoadingMore(true);
    setError(null);

    try {
      const response = await searchVideos(
        queryRef.current,
        maxResultsRef.current,
        controller.signal,
        pageTokenRef.current,
      );
      setResults((prev) => [...prev, ...response.results]);
      pageTokenRef.current = response.nextPageToken;
      setHasMore(response.nextPageToken !== null);
    } catch (err) {
      if (err instanceof Error && err.name === 'AbortError') {
        return;
      }
      setError(getErrorMessage(err, 'Search failed'));
    } finally {
      setIsLoadingMore(false);
    }
  }, [isLoading, isLoadingMore, hasMore]);

  const clearResults = useCallback(() => {
    setResults([]);
    setError(null);
    setHasMore(false);
    queryRef.current = '';
    pageTokenRef.current = null;
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
