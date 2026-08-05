import { useEffect, useState } from 'react';
import { getSearchHistory } from '../services/searchService';
import type { SearchHistoryItem } from '../types/api';

interface UseSearchHistoryResult {
  recentSearches: SearchHistoryItem[];
  /** True once the history fetch has settled (success or failure). */
  loaded: boolean;
}

const useSearchHistory = (limit: number): UseSearchHistoryResult => {
  const [recentSearches, setRecentSearches] = useState<SearchHistoryItem[]>([]);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    let active = true;
    getSearchHistory(limit)
      .then(items => {
        if (active) {
          setRecentSearches(items);
          setLoaded(true);
        }
      })
      .catch(() => {
        if (active) {
          setRecentSearches([]);
          setLoaded(true);
        }
      });
    return () => {
      active = false;
    };
  }, [limit]);

  return { recentSearches, loaded };
};

export default useSearchHistory;
