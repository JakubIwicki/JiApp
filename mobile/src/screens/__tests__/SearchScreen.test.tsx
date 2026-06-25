import React from 'react';
import { render, fireEvent, act } from '@testing-library/react-native';

// Mock useSearch
const mockSearch = jest.fn();
const mockClearResults = jest.fn();
const mockLoadMore = jest.fn();
const mockResults: any[] = [];
let mockIsLoading = false;
let mockIsLoadingMore = false;
let mockError: string | null = null;
let mockHasMore = false;

jest.mock('../../hooks/useSearch', () => ({
  __esModule: true,
  default: () => ({
    results: mockResults,
    isLoading: mockIsLoading,
    isLoadingMore: mockIsLoadingMore,
    error: mockError,
    hasMore: mockHasMore,
    search: mockSearch,
    loadMore: mockLoadMore,
    clearResults: mockClearResults,
  }),
}));

// Mock useKeepAwake
jest.mock('../../hooks/useKeepAwake', () => ({
  __esModule: true,
  default: jest.fn(),
}));

// Mock react-i18next
jest.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

// Mock @react-navigation/native useNavigation
const mockNavigate = jest.fn();
jest.mock('@react-navigation/native', () => {
  const actual = jest.requireActual('@react-navigation/native');
  return {
    ...actual,
    useNavigation: () => ({
      navigate: mockNavigate,
      setOptions: jest.fn(),
    }),
  };
});

// Mock searchService for getSearchHistory
jest.mock('../../services/searchService', () => ({
  searchVideos: jest.fn(),
  getSearchHistory: jest.fn(() => Promise.resolve([])),
}));

import SearchScreen from '../SearchScreen';

describe('SearchScreen', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockResults.splice(0, mockResults.length);
    mockIsLoading = false;
    mockIsLoadingMore = false;
    mockError = null;
    mockHasMore = false;
  });

  it('renders search bar', () => {
    const { getByTestId } = render(<SearchScreen />);
    expect(getByTestId('search-input')).toBeTruthy();
  });

  it('shows loading spinner when isLoading', () => {
    mockIsLoading = true;
    const { getByTestId } = render(<SearchScreen />);
    expect(getByTestId('loading-spinner')).toBeTruthy();
  });

  it('shows error message with retry on error', () => {
    mockError = 'Network error';
    const { getByTestId } = render(<SearchScreen />);
    expect(getByTestId('error-message')).toBeTruthy();
    expect(getByTestId('error-retry-button')).toBeTruthy();
  });

  it('can be unmounted without errors during async history load', async () => {
    const { unmount } = render(<SearchScreen />);
    unmount();
    await act(async () => {});
  });

  it('navigates to Download with full video object on VideoCard press', () => {
    const videoItem = {
      videoId: 'abc123',
      title: 'Test Video Title',
      description: 'Test video description',
      imageUrl: 'https://example.com/thumb.jpg',
      videoUrl: 'https://example.com/video.mp4',
      channelTitle: 'Test Channel',
    };
    mockResults.push(videoItem);

    const { getByTestId } = render(<SearchScreen />);
    fireEvent.press(getByTestId('video-card'));

    expect(mockNavigate).toHaveBeenCalledWith('Download', videoItem);
  });

  it('shows load-more spinner in footer when isLoadingMore', () => {
    mockResults.push({
      videoId: 'abc123',
      title: 'Test Video Title',
      description: 'Test video description',
      imageUrl: 'https://example.com/thumb.jpg',
      videoUrl: 'https://example.com/video.mp4',
      channelTitle: 'Test Channel',
    });
    mockHasMore = true;
    mockIsLoadingMore = true;

    const { getByTestId } = render(<SearchScreen />);
    expect(getByTestId('load-more-spinner')).toBeTruthy();
  });

  it('calls loadMore when FlatList onEndReached fires and hasMore is true', () => {
    mockResults.push({
      videoId: 'abc123',
      title: 'Test Video Title',
      description: 'Test video description',
      imageUrl: 'https://example.com/thumb.jpg',
      videoUrl: 'https://example.com/video.mp4',
      channelTitle: 'Test Channel',
    });
    mockHasMore = true;

    const { getByTestId } = render(<SearchScreen />);

    // Fire onEndReached on the FlatList
    const flatList = getByTestId('search-results-flatlist');
    fireEvent(flatList, 'onEndReached');

    expect(mockLoadMore).toHaveBeenCalledTimes(1);
  });

  it('does not call loadMore when hasMore is false', () => {
    mockResults.push({
      videoId: 'abc123',
      title: 'Test Video Title',
      description: 'Test video description',
      imageUrl: 'https://example.com/thumb.jpg',
      videoUrl: 'https://example.com/video.mp4',
      channelTitle: 'Test Channel',
    });
    mockHasMore = false;

    const { getByTestId } = render(<SearchScreen />);

    const flatList = getByTestId('search-results-flatlist');
    fireEvent(flatList, 'onEndReached');

    expect(mockLoadMore).not.toHaveBeenCalled();
  });
});
