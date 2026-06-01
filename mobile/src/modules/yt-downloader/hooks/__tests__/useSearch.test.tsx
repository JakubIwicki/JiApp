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

  it('passes AbortSignal and null pageToken to searchVideos', async () => {
    mockSearchVideos.mockResolvedValue({ results: [], nextPageToken: null });

    const { result } = renderHook(() => useSearch());

    await act(async () => {
      await result.current.search('test');
    });

    expect(mockSearchVideos).toHaveBeenCalledWith(
      'test',
      10,
      expect.any(AbortSignal),
      null,
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
    mockSearchVideos.mockResolvedValue({ results: mockResults, nextPageToken: null });

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
    expect(mockSearchVideos).toHaveBeenCalledWith('test query', 10, expect.any(AbortSignal), null);
  });

  it('search() passes maxResults to service', async () => {
    mockSearchVideos.mockResolvedValue({ results: [], nextPageToken: null });

    const { result } = renderHook(() => useSearch());

    await act(async () => {
      await result.current.search('test', 5);
    });

    expect(mockSearchVideos).toHaveBeenCalledWith('test', 5, expect.any(AbortSignal), null);
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
  });

  it('search() sets generic error message when error has no message', async () => {
    mockSearchVideos.mockRejectedValue('Something went wrong');

    const { result } = renderHook(() => useSearch());

    await act(async () => {
      await result.current.search('test query');
    });

    expect(result.current.error).toBe('Search failed');
  });

  it('clearResults() resets results to empty and clears error and hasMore', async () => {
    mockSearchVideos.mockResolvedValue({
      results: [createVideoItem('1')],
      nextPageToken: 'CAoQAA',
    });

    const { result } = renderHook(() => useSearch());

    await act(async () => {
      await result.current.search('test');
    });

    expect(result.current.results).toHaveLength(1);
    expect(result.current.hasMore).toBe(true);

    act(() => {
      result.current.clearResults();
    });

    expect(result.current.results).toEqual([]);
    expect(result.current.error).toBeNull();
    expect(result.current.hasMore).toBe(false);
  });

  describe('loadMore', () => {
    it('loadMore() appends results and passes stored pageToken', async () => {
      const page1 = [createVideoItem('1'), createVideoItem('2')];
      const page2 = [createVideoItem('3'), createVideoItem('4')];

      mockSearchVideos
        .mockResolvedValueOnce({ results: page1, nextPageToken: 'token2' })
        .mockResolvedValueOnce({ results: page2, nextPageToken: null });

      const { result } = renderHook(() => useSearch());

      await act(async () => {
        await result.current.search('test', 2);
      });

      expect(result.current.results).toEqual(page1);
      expect(result.current.hasMore).toBe(true);

      await act(async () => {
        await result.current.loadMore();
      });

      expect(result.current.results).toEqual([...page1, ...page2]);
      expect(result.current.hasMore).toBe(false);
      expect(mockSearchVideos).toHaveBeenNthCalledWith(
        2,
        'test',
        2,
        expect.any(AbortSignal),
        'token2',
      );
    });

    it('loadMore() is a no-op when hasMore is false', async () => {
      mockSearchVideos.mockResolvedValue({ results: [createVideoItem('1')], nextPageToken: null });

      const { result } = renderHook(() => useSearch());

      await act(async () => {
        await result.current.search('test');
      });

      mockSearchVideos.mockClear();

      await act(async () => {
        await result.current.loadMore();
      });

      expect(mockSearchVideos).not.toHaveBeenCalled();
    });

    it('loadMore() is a no-op when isLoadingMore is true', async () => {
      const page1 = [createVideoItem('1')];
      const page2 = [createVideoItem('2')];

      let resolveFirstLoadMore!: (value: {
        results: VideoItem[];
        nextPageToken: string | null;
      }) => void;
      const deferredPromise = new Promise<{
        results: VideoItem[];
        nextPageToken: string | null;
      }>((resolve) => {
        resolveFirstLoadMore = resolve;
      });

      mockSearchVideos
        .mockResolvedValueOnce({ results: page1, nextPageToken: 'token2' })
        .mockReturnValueOnce(deferredPromise);

      const { result } = renderHook(() => useSearch());

      await act(async () => {
        await result.current.search('test', 2);
      });

      expect(result.current.hasMore).toBe(true);

      // Start first loadMore — it will hang on the deferred promise
      let firstLoadMorePromise: Promise<void>;
      act(() => {
        firstLoadMorePromise = result.current.loadMore();
      });

      // isLoadingMore should now be true (set synchronously before await)
      expect(result.current.isLoadingMore).toBe(true);

      mockSearchVideos.mockClear();

      // Try loadMore again — should be blocked by isLoadingMore guard
      await act(async () => {
        await result.current.loadMore();
      });

      expect(mockSearchVideos).not.toHaveBeenCalled();

      // Resolve the deferred promise to complete the first loadMore
      await act(async () => {
        resolveFirstLoadMore({ results: page2, nextPageToken: null });
        await firstLoadMorePromise!;
      });

      expect(result.current.isLoadingMore).toBe(false);
      expect(result.current.results).toEqual([...page1, ...page2]);
      expect(result.current.hasMore).toBe(false);
    });

    it('loadMore() reuses maxResults from initial search', async () => {
      const page1 = [createVideoItem('1')];
      const page2 = [createVideoItem('2')];

      mockSearchVideos
        .mockResolvedValueOnce({ results: page1, nextPageToken: 'token2' })
        .mockResolvedValueOnce({ results: page2, nextPageToken: null });

      const { result } = renderHook(() => useSearch());

      await act(async () => {
        await result.current.search('test query', 3);
      });

      expect(mockSearchVideos).toHaveBeenCalledWith('test query', 3, expect.any(AbortSignal), null);

      await act(async () => {
        await result.current.loadMore();
      });

      expect(mockSearchVideos).toHaveBeenLastCalledWith('test query', 3, expect.any(AbortSignal), 'token2');
    });

    it('hasMore is true when nextPageToken is non-null', async () => {
      mockSearchVideos.mockResolvedValue({ results: [createVideoItem('1')], nextPageToken: 'abc123' });

      const { result } = renderHook(() => useSearch());

      await act(async () => {
        await result.current.search('test');
      });

      expect(result.current.hasMore).toBe(true);
    });

    it('loadMore() is a no-op when isLoading is true (race condition guard)', async () => {
      const page1 = [createVideoItem('1')];
      mockSearchVideos.mockResolvedValue({ results: page1, nextPageToken: 'token2' });

      const { result } = renderHook(() => useSearch());

      // Complete initial search so hasMore is true
      await act(async () => {
        await result.current.search('test');
      });
      expect(result.current.hasMore).toBe(true);

      // Start a new search; isLoading becomes true, hasMore still true from previous search
      mockSearchVideos.mockClear();
      let searchPromise: Promise<void>;
      act(() => {
        searchPromise = result.current.search('new query');
      });
      expect(result.current.isLoading).toBe(true);

      // loadMore should be a no-op because isLoading is true
      await act(async () => {
        await result.current.loadMore();
      });

      // searchVideos should have been called once by search(), and NOT by loadMore()
      expect(mockSearchVideos).toHaveBeenCalledTimes(1);

      // Complete the in-flight search
      mockSearchVideos.mockResolvedValue({ results: [createVideoItem('2')], nextPageToken: null });
      await act(async () => {
        await searchPromise!;
      });
    });

    it('hasMore is false when nextPageToken is null', async () => {
      mockSearchVideos.mockResolvedValue({ results: [createVideoItem('1')], nextPageToken: null });

      const { result } = renderHook(() => useSearch());

      await act(async () => {
        await result.current.search('test');
      });

      expect(result.current.hasMore).toBe(false);
    });

    it('loadMore() sets error on API failure and preserves existing results', async () => {
      const page1 = [createVideoItem('1')];
      const apiError = new Error('Network error');

      mockSearchVideos
        .mockResolvedValueOnce({ results: page1, nextPageToken: 'token2' })
        .mockRejectedValueOnce(apiError);

      const { result } = renderHook(() => useSearch());

      await act(async () => {
        await result.current.search('test', 2);
      });

      expect(result.current.results).toEqual(page1);
      expect(result.current.error).toBeNull();

      await act(async () => {
        await result.current.loadMore();
      });

      expect(result.current.results).toEqual(page1);
      expect(result.current.error).toBe('Network error');
      expect(result.current.isLoadingMore).toBe(false);
    });
  });

  it('search() clears stale hasMore from previous paginated search', async () => {
    const page1 = [createVideoItem('1')];
    const page2 = [createVideoItem('2')];

    mockSearchVideos
      .mockResolvedValueOnce({ results: page1, nextPageToken: 'token2' })
      .mockResolvedValueOnce({ results: page2, nextPageToken: null });

    const { result } = renderHook(() => useSearch());

    await act(async () => {
      await result.current.search('first query', 2);
    });

    expect(result.current.hasMore).toBe(true);
    expect(result.current.results).toEqual(page1);

    await act(async () => {
      await result.current.search('second query', 2);
    });

    expect(result.current.hasMore).toBe(false);
    expect(result.current.results).toEqual(page2);
  });
});
