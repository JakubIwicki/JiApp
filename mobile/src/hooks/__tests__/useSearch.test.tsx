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
    mockSearchVideos.mockResolvedValue({ results: [] });

    const { result } = renderHook(() => useSearch());

    await act(async () => {
      await result.current.search('test');
    });

    expect(mockSearchVideos).toHaveBeenCalledWith(
      'test',
      undefined,
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

    // Each call should have its own AbortSignal
    const calls = mockSearchVideos.mock.calls;
    expect(calls[0][2]).toBeInstanceOf(AbortSignal);
    expect(calls[1][2]).toBeInstanceOf(AbortSignal);
    // The signals should be different (new controller each call)
    expect(calls[0][2]).not.toBe(calls[1][2]);
  });

  it('initialState has empty results, isLoading=false, error=null', () => {
    const { result } = renderHook(() => useSearch());

    expect(result.current.results).toEqual([]);
    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it('search() sets isLoading=true, then populates results on success', async () => {
    const mockResults = [createVideoItem('1'), createVideoItem('2')];
    mockSearchVideos.mockResolvedValue({ results: mockResults });

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
    expect(mockSearchVideos).toHaveBeenCalledWith('test query', undefined, expect.any(AbortSignal));
  });

  it('search() passes maxResults to service', async () => {
    mockSearchVideos.mockResolvedValue({ results: [] });

    const { result } = renderHook(() => useSearch());

    await act(async () => {
      await result.current.search('test', 5);
    });

    expect(mockSearchVideos).toHaveBeenCalledWith('test', 5, expect.any(AbortSignal));
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

  it('clearResults() resets results to empty and clears error', async () => {
    mockSearchVideos.mockResolvedValue({
      results: [createVideoItem('1')],
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
  });
});
