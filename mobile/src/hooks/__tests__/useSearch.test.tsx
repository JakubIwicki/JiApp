import { renderHook, act } from '@testing-library/react-native';
import useSearch from '../useSearch';
import * as searchService from '../../services/searchService';
import type { VideoItem } from '../../types/api';

jest.mock('../../services/searchService', () => ({
  searchVideos: jest.fn(),
  getSearchHistory: jest.fn(),
}));

const mockSearchVideos = searchService.searchVideos as jest.Mock;

const createVideoItem = (id: string): VideoItem => ({
  videoId: id,
  title: `Video ${id}`,
  description: `Description for video ${id}`,
  imageUrl: `https://example.com/${id}.jpg`,
  videoUrl: `https://example.com/${id}.mp4`,
  channelTitle: 'TestChannel',
});

describe('useSearch', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('passes AbortSignal to searchVideos', async () => {
    mockSearchVideos.mockResolvedValue({ results: [], hasMore: false });

    const { result } = renderHook(() => useSearch());

    await act(async () => {
      await result.current.search('test');
    });

    expect(mockSearchVideos).toHaveBeenCalledWith(
      'test',
      0,
      expect.any(AbortSignal),
    );
  });

  it('does not set error state when request is aborted', async () => {
    const abortError = new Error('The operation was aborted');
    abortError.name = 'AbortError';
    mockSearchVideos.mockRejectedValue(abortError);

    const { result } = renderHook(() => useSearch());

    await act(async () => {
      await result.current.search('test');
    });

    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
    expect(result.current.results).toEqual([]);
  });

  it('aborts previous request when search is called again', async () => {
    const { result } = renderHook(() => useSearch());

    await act(async () => {
      await result.current.search('first');
    });

    await act(async () => {
      await result.current.search('second');
    });

    const calls = mockSearchVideos.mock.calls;
    expect(calls[0][2]).toBeInstanceOf(AbortSignal);
    expect(calls[1][2]).toBeInstanceOf(AbortSignal);
    expect(calls[0][2]).not.toBe(calls[1][2]);
  });

  it('initialState has empty results, isLoading=false, error=null', () => {
    const { result } = renderHook(() => useSearch());

    expect(result.current.results).toEqual([]);
    expect(result.current.isLoading).toBe(false);
    expect(result.current.isLoadingMore).toBe(false);
    expect(result.current.error).toBeNull();
    expect(result.current.hasMore).toBe(false);
  });

  it('search() sets isLoading=true, then populates results on success', async () => {
    const mockResults = [createVideoItem('1'), createVideoItem('2')];
    mockSearchVideos.mockResolvedValue({ results: mockResults, hasMore: true });

    const { result } = renderHook(() => useSearch());

    let promise: Promise<void>;
    act(() => {
      promise = result.current.search('test query');
    });

    expect(result.current.isLoading).toBe(true);

    await act(async () => {
      await promise;
    });

    expect(result.current.isLoading).toBe(false);
    expect(result.current.results).toEqual(mockResults);
    expect(result.current.error).toBeNull();
    expect(result.current.hasMore).toBe(true);
    expect(mockSearchVideos).toHaveBeenCalledWith(
      'test query',
      0,
      expect.any(AbortSignal),
    );
  });

  it('search() sets error on API failure, isLoading returns to false', async () => {
    const apiError = new Error('Network error');
    mockSearchVideos.mockRejectedValue(apiError);

    const { result } = renderHook(() => useSearch());

    let promise: Promise<void>;
    act(() => {
      promise = result.current.search('test query');
    });

    expect(result.current.isLoading).toBe(true);

    await act(async () => {
      await promise;
    });

    expect(result.current.isLoading).toBe(false);
    expect(result.current.results).toEqual([]);
    expect(result.current.error).toBe('Network error');
    expect(result.current.hasMore).toBe(false);
  });

  it('search() sets generic error message when error has no message', async () => {
    mockSearchVideos.mockRejectedValue('Something went wrong');

    const { result } = renderHook(() => useSearch());

    await act(async () => {
      await result.current.search('test query');
    });

    expect(result.current.error).toBe('Search failed');
  });

  it('clearResults() resets results to empty and clears error', async () => {
    mockSearchVideos.mockResolvedValue({
      results: [createVideoItem('1')],
      hasMore: false,
    });

    const { result } = renderHook(() => useSearch());

    await act(async () => {
      await result.current.search('test');
    });

    expect(result.current.results).toHaveLength(1);

    act(() => {
      result.current.clearResults();
    });

    expect(result.current.results).toEqual([]);
    expect(result.current.error).toBeNull();
    expect(result.current.hasMore).toBe(false);
  });

  // ── loadMore ──

  it('loadMore() appends next page results and tracks hasMore', async () => {
    const page0 = [createVideoItem('1')];
    const page1 = [createVideoItem('2'), createVideoItem('3')];

    mockSearchVideos
      .mockResolvedValueOnce({ results: page0, hasMore: true })
      .mockResolvedValueOnce({ results: page1, hasMore: false });

    const { result } = renderHook(() => useSearch());

    await act(async () => {
      await result.current.search('test');
    });

    expect(result.current.results).toEqual(page0);
    expect(result.current.hasMore).toBe(true);

    await act(async () => {
      await result.current.loadMore();
    });

    expect(result.current.results).toEqual([...page0, ...page1]);
    expect(result.current.hasMore).toBe(false);
    expect(mockSearchVideos).toHaveBeenCalledTimes(2);
    expect(mockSearchVideos).toHaveBeenNthCalledWith(
      2,
      'test',
      1,
      expect.any(AbortSignal),
    );
  });

  it('loadMore() is a no-op when hasMore is false', async () => {
    mockSearchVideos.mockResolvedValueOnce({
      results: [createVideoItem('1')],
      hasMore: false,
    });

    const { result } = renderHook(() => useSearch());

    await act(async () => {
      await result.current.search('test');
    });

    expect(result.current.hasMore).toBe(false);
    expect(mockSearchVideos).toHaveBeenCalledTimes(1);

    await act(async () => {
      await result.current.loadMore();
    });

    expect(mockSearchVideos).toHaveBeenCalledTimes(1);
  });

  it('loadMore() is a no-op while isLoading', async () => {
    mockSearchVideos.mockResolvedValueOnce({
      results: [createVideoItem('1')],
      hasMore: true,
    });

    const { result } = renderHook(() => useSearch());

    let searchPromise: Promise<void>;
    act(() => {
      searchPromise = result.current.search('test');
    });

    // loadMore while initial search is still in flight — should no-op
    await act(async () => {
      await result.current.loadMore();
    });

    await act(async () => {
      await searchPromise;
    });

    expect(mockSearchVideos).toHaveBeenCalledTimes(1);
  });

  it('loadMore() is a no-op while isLoadingMore', async () => {
    mockSearchVideos
      .mockResolvedValueOnce({ results: [createVideoItem('1')], hasMore: true })
      .mockResolvedValueOnce({
        results: [createVideoItem('2')],
        hasMore: false,
      });

    const { result } = renderHook(() => useSearch());

    await act(async () => {
      await result.current.search('test');
    });

    let loadMorePromise: Promise<void>;
    act(() => {
      loadMorePromise = result.current.loadMore();
    });

    // Second loadMore while first is in flight — should no-op
    await act(async () => {
      await result.current.loadMore();
    });

    await act(async () => {
      await loadMorePromise;
    });

    expect(mockSearchVideos).toHaveBeenCalledTimes(2); // search + 1 loadMore
  });

  it('loadMore() preserves existing results on error', async () => {
    const page0 = [createVideoItem('1')];
    mockSearchVideos
      .mockResolvedValueOnce({ results: page0, hasMore: true })
      .mockRejectedValueOnce(new Error('Page 2 failed'));

    const { result } = renderHook(() => useSearch());

    await act(async () => {
      await result.current.search('test');
    });

    expect(result.current.results).toEqual(page0);

    await act(async () => {
      await result.current.loadMore();
    });

    expect(result.current.results).toEqual(page0);
    expect(result.current.hasMore).toBe(false);
    expect(result.current.error).toBe('Page 2 failed');
  });

  it('loadMore() silently returns on AbortError', async () => {
    mockSearchVideos.mockResolvedValueOnce({
      results: [createVideoItem('1')],
      hasMore: true,
    });

    const { result } = renderHook(() => useSearch());

    await act(async () => {
      await result.current.search('test');
    });

    const abortError = new Error('aborted');
    abortError.name = 'AbortError';
    mockSearchVideos.mockRejectedValueOnce(abortError);

    await act(async () => {
      await result.current.loadMore();
    });

    expect(result.current.error).toBeNull();
    expect(result.current.results).toHaveLength(1);
  });
});
