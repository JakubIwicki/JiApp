import React from 'react';
import { render, act } from '@testing-library/react-native';

// Mock useHistory
const mockLoadHistory = jest.fn();
const mockRefresh = jest.fn();
let mockSearches: any[] = [];
let mockDownloads: any[] = [];
let mockIsLoading = false;
let mockError: string | null = null;

jest.mock('../../hooks/useHistory', () => ({
  __esModule: true,
  default: () => ({
    searches: mockSearches,
    downloads: mockDownloads,
    isLoading: mockIsLoading,
    error: mockError,
    loadHistory: mockLoadHistory,
    refresh: mockRefresh,
  }),
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
    useFocusEffect: (callback: () => void) => callback(),
  };
});

import HistoryScreen from '../HistoryScreen';

describe('HistoryScreen', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockSearches = [];
    mockDownloads = [];
    mockIsLoading = false;
    mockError = null;
  });

  it('renders empty state when no searches or downloads', () => {
    const { getByText } = render(<HistoryScreen />);
    expect(getByText('history.searches')).toBeTruthy();
    expect(getByText('history.downloads')).toBeTruthy();
    expect(getByText('history.noSearches')).toBeTruthy();
    expect(getByText('history.noDownloads')).toBeTruthy();
  });

  it('renders search history items', () => {
    mockSearches = [
      { id: 1, searchText: 'test query', searchedAt: '2026-05-20T10:00:00Z' },
    ];
    const { getByTestId } = render(<HistoryScreen />);
    expect(getByTestId('history-item-search')).toBeTruthy();
  });

  it('renders download history items', () => {
    mockDownloads = [
      {
        id: 1,
        videoTitle: 'Test Video',
        videoDescription: 'Desc',
        videoId: 'abc123',
        videoUrl: 'https://youtube.com/watch?v=abc123',
        imageUrl: 'https://i.ytimg.com/vi/abc123/default.jpg',
        downloadedAt: '2026-05-20T10:00:00Z',
      },
    ];
    const { getByTestId } = render(<HistoryScreen />);
    expect(getByTestId('history-item-download')).toBeTruthy();
  });

  it('shows loading spinner when loading with no data', () => {
    mockIsLoading = true;
    const { getByTestId } = render(<HistoryScreen />);
    expect(getByTestId('loading-spinner')).toBeTruthy();
  });

  it('shows error message when error with no data', () => {
    mockError = 'Failed to load';
    const { getByTestId } = render(<HistoryScreen />);
    expect(getByTestId('error-message')).toBeTruthy();
  });

  it('calls loadHistory on mount', () => {
    render(<HistoryScreen />);
    expect(mockLoadHistory).toHaveBeenCalledWith(50);
  });

  it('can be unmounted without errors during async load', async () => {
    const { unmount } = render(<HistoryScreen />);
    unmount();
    await act(async () => {});
  });
});
