import { renderHook, act } from '@testing-library/react-native';
import useHistory from '../useHistory';
import * as historyService from '../../services/historyService';
import type {
  SearchHistoryItem,
  DownloadHistoryItem,
} from '../../types/api';

jest.mock('../../services/historyService', () => ({
  getHistory: jest.fn(),
}));

const mockGetHistory = historyService.getHistory as jest.Mock;

const createSearchItem = (
  overrides: Partial<SearchHistoryItem> = {},
): SearchHistoryItem => ({
  id: 1,
  searchText: 'test query',
  searchedAt: '2026-05-20T10:00:00Z',
  ...overrides,
});

const createDownloadItem = (
  overrides: Partial<DownloadHistoryItem> = {},
): DownloadHistoryItem => ({
  id: 1,
  videoTitle: 'Test Video',
  videoDescription: 'A test video description',
  videoId: 'abc123',
  videoUrl: 'https://youtube.com/watch?v=abc123',
  imageUrl: 'https://i.ytimg.com/vi/abc123/default.jpg',
  downloadedAt: '2026-05-20T10:00:00Z',
  ...overrides,
});

describe('useHistory', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('passes AbortSignal to getHistory', async () => {
    mockGetHistory.mockResolvedValue({ searches: [], downloads: [] });

    const { result } = renderHook(() => useHistory());

    await act(async () => {
      await result.current.loadHistory();
    });

    expect(mockGetHistory).toHaveBeenCalledWith(
      undefined,
      expect.any(AbortSignal),
    );
  });

  it('does not set error state when request is aborted', async () => {
    const abortError = new Error('The operation was aborted');
    abortError.name = 'AbortError';
    mockGetHistory.mockRejectedValue(abortError);

    const { result } = renderHook(() => useHistory());

    await act(async () => {
      await result.current.loadHistory();
    });

    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
    expect(result.current.searches).toEqual([]);
    expect(result.current.downloads).toEqual([]);
  });

  it('initialState has empty searches/downloads, isLoading=false, error=null', () => {
    const { result } = renderHook(() => useHistory());

    expect(result.current.searches).toEqual([]);
    expect(result.current.downloads).toEqual([]);
    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it('loadHistory() fetches and splits into searches/downloads', async () => {
    const mockSearches = [createSearchItem({ id: 1 }), createSearchItem({ id: 2, searchText: 'another query' })];
    const mockDownloads = [createDownloadItem({ id: 1 })];
    mockGetHistory.mockResolvedValue({
      searches: mockSearches,
      downloads: mockDownloads,
    });

    const { result } = renderHook(() => useHistory());

    let promise: Promise<void>;
    act(() => {
      promise = result.current.loadHistory();
    });

    expect(result.current.isLoading).toBe(true);

    await act(async () => {
      await promise;
    });

    expect(result.current.isLoading).toBe(false);
    expect(result.current.searches).toEqual(mockSearches);
    expect(result.current.downloads).toEqual(mockDownloads);
    expect(result.current.error).toBeNull();
    expect(mockGetHistory).toHaveBeenCalledWith(undefined, expect.any(AbortSignal));
  });

  it('loadHistory() passes limit to service', async () => {
    mockGetHistory.mockResolvedValue({ searches: [], downloads: [] });

    const { result } = renderHook(() => useHistory());

    await act(async () => {
      await result.current.loadHistory(50);
    });

    expect(mockGetHistory).toHaveBeenCalledWith(50, expect.any(AbortSignal));
  });

  it('loadHistory() sets error on API failure, isLoading returns to false', async () => {
    const apiError = new Error('Network error');
    mockGetHistory.mockRejectedValue(apiError);

    const { result } = renderHook(() => useHistory());

    let promise: Promise<void>;
    act(() => {
      promise = result.current.loadHistory();
    });

    expect(result.current.isLoading).toBe(true);

    await act(async () => {
      await promise;
    });

    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBe('Network error');
    expect(result.current.searches).toEqual([]);
    expect(result.current.downloads).toEqual([]);
  });

  it('loadHistory() sets generic error when thrown value is not an Error instance', async () => {
    mockGetHistory.mockRejectedValue('Something went wrong');

    const { result } = renderHook(() => useHistory());

    await act(async () => {
      await result.current.loadHistory();
    });

    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBe('Failed to load history');
    expect(result.current.searches).toEqual([]);
    expect(result.current.downloads).toEqual([]);
  });

  it('refresh() re-fetches with same limit', async () => {
    mockGetHistory.mockResolvedValue({ searches: [], downloads: [] });

    const { result } = renderHook(() => useHistory());

    await act(async () => {
      await result.current.loadHistory(20);
    });

    expect(mockGetHistory).toHaveBeenCalledWith(20, expect.any(AbortSignal));

    const updatedSearches = [createSearchItem({ id: 3, searchText: 'new query' })];
    const updatedDownloads = [createDownloadItem({ id: 2, videoTitle: 'New Video' })];
    mockGetHistory.mockResolvedValue({
      searches: updatedSearches,
      downloads: updatedDownloads,
    });

    await act(async () => {
      await result.current.refresh();
    });

    expect(mockGetHistory).toHaveBeenCalledWith(20, expect.any(AbortSignal));
    expect(result.current.searches).toEqual(updatedSearches);
    expect(result.current.downloads).toEqual(updatedDownloads);
  });

  it('empty state when both arrays are empty after load', async () => {
    mockGetHistory.mockResolvedValue({ searches: [], downloads: [] });

    const { result } = renderHook(() => useHistory());

    await act(async () => {
      await result.current.loadHistory();
    });

    expect(result.current.searches).toEqual([]);
    expect(result.current.downloads).toEqual([]);
    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
  });
});
