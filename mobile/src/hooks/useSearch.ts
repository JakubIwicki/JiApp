import { useState, useCallback, useEffect, useRef } from 'react';
import { searchVideos } from '../services/searchService';
import { getErrorMessage } from '../utils/errorUtils';
import type { VideoItem } from '../types/api';

interface UseSearchResult {
  results: VideoItem[];
  isLoading: boolean;
  error: string | null;
  search: (query: string, maxResults?: number) => Promise<void>;
  clearResults: () => void;
}

const useSearch = (): UseSearchResult => {
  const [results, setResults] = useState<VideoItem[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    const controller = abortRef.current;
    return () => {
      controller?.abort();
    };
  }, [abortRef]);

  const search = useCallback(
    async (query: string, maxResults?: number) => {
      // Cancel any previous in-flight request
      abortRef.current?.abort();

      const controller = new AbortController();
      abortRef.current = controller;

      setIsLoading(true);
      setError(null);

      try {
        const response = await searchVideos(query, maxResults, controller.signal);
        setResults(response.results);
      } catch (err) {
        if (err instanceof Error && err.name === 'AbortError') {
          return;
        }
        setError(getErrorMessage(err, 'Search failed'));
        setResults([]);
      } finally {
        setIsLoading(false);
      }
    },
    [],
  );

  const clearResults = useCallback(() => {
    setResults([]);
    setError(null);
  }, []);

  return {
    results,
    isLoading,
    error,
    search,
    clearResults,
  };
};

export default useSearch;
