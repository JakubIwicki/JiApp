import { useCallback, useReducer } from 'react';
import {
  getDownloadHistory,
  archiveDownload as archiveDownloadService,
} from '../services/downloadService';
import useToast from './useToast';
import type { DownloadHistoryItem } from '../types/api';

interface DownloadsState {
  downloads: DownloadHistoryItem[];
  isLoading: boolean;
  isRefreshing: boolean;
  error: string | null;
}

type DownloadsAction =
  | { type: 'FETCH_START'; pull: boolean }
  | { type: 'FETCH_SUCCESS'; downloads: DownloadHistoryItem[] }
  | { type: 'FETCH_ERROR'; error: string }
  | { type: 'REMOVE_DOWNLOAD'; id: number };

function downloadsReducer(
  state: DownloadsState,
  action: DownloadsAction,
): DownloadsState {
  switch (action.type) {
    case 'FETCH_START':
      return {
        ...state,
        error: null,
        isLoading: action.pull ? state.isLoading : true,
        isRefreshing: action.pull ? true : false,
      };
    case 'FETCH_SUCCESS':
      return {
        ...state,
        downloads: action.downloads,
        isLoading: false,
        isRefreshing: false,
        error: null,
      };
    case 'FETCH_ERROR':
      return {
        ...state,
        error: action.error,
        isLoading: false,
        isRefreshing: false,
      };
    case 'REMOVE_DOWNLOAD':
      return {
        ...state,
        downloads: state.downloads.filter(d => d.id !== action.id),
      };
    default:
      return state;
  }
}

const initialDownloadsState: DownloadsState = {
  downloads: [],
  isLoading: true,
  isRefreshing: false,
  error: null,
};

interface UseDownloadsResult {
  downloads: DownloadHistoryItem[];
  isLoading: boolean;
  isRefreshing: boolean;
  error: string | null;
  loadDownloads: (pull: boolean) => Promise<void>;
  archiveDownload: (id: number) => Promise<void>;
}

const useDownloads = (): UseDownloadsResult => {
  const [state, dispatch] = useReducer(downloadsReducer, initialDownloadsState);
  const { showSuccess, showError } = useToast();

  const loadDownloads = useCallback(async (pull: boolean) => {
    dispatch({ type: 'FETCH_START', pull });
    try {
      const items = await getDownloadHistory();
      dispatch({ type: 'FETCH_SUCCESS', downloads: items });
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : String(err);
      dispatch({ type: 'FETCH_ERROR', error: message });
    }
  }, []);

  const archiveDownload = useCallback(
    async (id: number) => {
      dispatch({ type: 'REMOVE_DOWNLOAD', id });
      try {
        await archiveDownloadService(id);
        showSuccess('toast.downloadArchived');
      } catch {
        showError('toast.archiveFailed');
        await loadDownloads(false);
      }
    },
    [loadDownloads, showSuccess, showError],
  );

  return {
    downloads: state.downloads,
    isLoading: state.isLoading,
    isRefreshing: state.isRefreshing,
    error: state.error,
    loadDownloads,
    archiveDownload,
  };
};

export default useDownloads;
