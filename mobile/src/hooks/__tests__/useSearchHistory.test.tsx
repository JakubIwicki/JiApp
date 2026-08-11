import { renderHook, act } from '@testing-library/react-native';
import useSearchHistory from '../useSearchHistory';
import * as searchService from '../../services/searchService';
import type { SearchHistoryItem } from '../../types/api';

jest.mock('../../services/searchService', () => ({
  getSearchHistory: jest.fn(),
}));

const mockGetSearchHistory = searchService.getSearchHistory as jest.Mock;

const createSearchItem = (
  overrides: Partial<SearchHistoryItem> = {},
): SearchHistoryItem => ({
  id: 1,
  searchText: 'test query',
  searchedAt: '2026-05-20T10:00:00Z',
  ...overrides,
});

describe('useSearchHistory', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('forwards limit to getSearchHistory exactly once on mount', async () => {
    mockGetSearchHistory.mockResolvedValue([]);

    const { result } = renderHook(() => useSearchHistory(10));

    await act(async () => {});

    expect(mockGetSearchHistory).toHaveBeenCalledTimes(1);
    expect(mockGetSearchHistory).toHaveBeenCalledWith(10);
    expect(result.current.loaded).toBe(true);
  });

  it('is not loaded until the fetch settles', async () => {
    let resolveFetch: (items: SearchHistoryItem[]) => void = () => {};
    mockGetSearchHistory.mockImplementation(
      () =>
        new Promise<SearchHistoryItem[]>(resolve => {
          resolveFetch = resolve;
        }),
    );

    const { result } = renderHook(() => useSearchHistory(5));

    expect(result.current.recentSearches).toEqual([]);
    expect(result.current.loaded).toBe(false);

    await act(async () => {
      resolveFetch([createSearchItem({ id: 1 })]);
    });

    expect(result.current.loaded).toBe(true);
    expect(result.current.recentSearches).toEqual([
      createSearchItem({ id: 1 }),
    ]);
  });

  it('populates recentSearches on a successful fetch', async () => {
    const items = [
      createSearchItem({ id: 1 }),
      createSearchItem({ id: 2, searchText: 'another query' }),
    ];
    mockGetSearchHistory.mockResolvedValue(items);

    const { result } = renderHook(() => useSearchHistory(3));

    await act(async () => {});

    expect(result.current.recentSearches).toEqual(items);
    expect(result.current.loaded).toBe(true);
  });

  it('marks loaded and keeps history empty when the fetch fails', async () => {
    mockGetSearchHistory.mockRejectedValue(new Error('Fetch failed'));

    const { result } = renderHook(() => useSearchHistory(5));

    await act(async () => {});

    expect(result.current.recentSearches).toEqual([]);
    expect(result.current.loaded).toBe(true);
  });

  it('clears recentSearches when a refetch after a limit change fails', async () => {
    mockGetSearchHistory.mockResolvedValue([createSearchItem({ id: 1 })]);

    const { result, rerender } = renderHook(
      ({ limit }: { limit: number }) => useSearchHistory(limit),
      { initialProps: { limit: 5 } },
    );

    await act(async () => {});
    expect(result.current.recentSearches).toEqual([
      createSearchItem({ id: 1 }),
    ]);

    mockGetSearchHistory.mockRejectedValue(new Error('Fetch failed'));

    rerender({ limit: 10 });

    await act(async () => {});

    expect(result.current.recentSearches).toEqual([]);
    expect(result.current.loaded).toBe(true);
  });

  it('refetches with the new limit when limit changes', async () => {
    mockGetSearchHistory.mockResolvedValue([]);

    const { rerender } = renderHook(
      ({ limit }: { limit: number }) => useSearchHistory(limit),
      { initialProps: { limit: 5 } },
    );

    await act(async () => {});
    expect(mockGetSearchHistory).toHaveBeenCalledTimes(1);

    rerender({ limit: 20 });

    await act(async () => {});

    expect(mockGetSearchHistory).toHaveBeenCalledTimes(2);
    expect(mockGetSearchHistory).toHaveBeenLastCalledWith(20);
  });
});
