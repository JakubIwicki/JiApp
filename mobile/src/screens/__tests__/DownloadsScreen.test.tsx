import React from 'react';
import { render, act } from '@testing-library/react-native';

// Mock downloadService
const mockGetDownloadHistory = jest.fn();

jest.mock('../../services/downloadService', () => ({
  getDownloadHistory: (...args: unknown[]) => mockGetDownloadHistory(...args),
  archiveDownload: jest.fn(),
}));

// Mock react-i18next
jest.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

// Mock @react-navigation/native useNavigation and useFocusEffect
const mockNavigate = jest.fn();
jest.mock('@react-navigation/native', () => {
  const ReactMock = require('react');
  const actual = jest.requireActual('@react-navigation/native');
  return {
    ...actual,
    useNavigation: () => ({
      navigate: mockNavigate,
      setOptions: jest.fn(),
    }),
    useFocusEffect: (callback: () => void) => {
      // Schedule in useEffect to avoid synchronous state updates during render
      ReactMock.useEffect(() => {
        callback();
      }, [callback]);
    },
  };
});

// Mock hooks
jest.mock('../../hooks/useKeepAwake', () => ({
  __esModule: true,
  default: jest.fn(),
}));

jest.mock('../../hooks/useScreenTitle', () => ({
  __esModule: true,
  default: jest.fn(),
}));

jest.mock('../../hooks/useToast', () => ({
  __esModule: true,
  default: () => ({
    showSuccess: jest.fn(),
    showError: jest.fn(),
  }),
}));

// Mock sub-components
jest.mock('../../components/LoadingSpinner', () => {
  const React = require('react');
  const { View } = require('react-native');
  return {
    __esModule: true,
    default: () => React.createElement(View, { testID: 'loading-spinner' }),
  };
});

jest.mock('../../components/ErrorMessage', () => {
  const React = require('react');
  const { View } = require('react-native');
  return {
    __esModule: true,
    default: ({
      message,
      onRetry,
    }: {
      message: string;
      onRetry?: () => void;
    }) =>
      React.createElement(View, {
        testID: 'error-message',
        message,
        onRetry: onRetry ? 'provided' : undefined,
      }),
  };
});

jest.mock('../../components/RefreshableScrollView', () => {
  const React = require('react');
  const { ScrollView } = require('react-native');
  return {
    __esModule: true,
    default: ({
      children,
      ...props
    }: React.PropsWithChildren<Record<string, unknown>>) =>
      React.createElement(
        ScrollView,
        { ...props, testID: 'refreshable-scroll' },
        children,
      ),
  };
});

jest.mock('../../components/SearchBar', () => {
  const React = require('react');
  const { View } = require('react-native');
  return {
    __esModule: true,
    default: () => React.createElement(View, { testID: 'search-bar' }),
  };
});

jest.mock('../../components/HistorySection', () => {
  const React = require('react');
  const { Text, View } = require('react-native');
  return {
    __esModule: true,
    default: ({
      title,
      items,
      emptyText,
    }: {
      title: string;
      items: unknown[];
      emptyText: string;
    }) =>
      React.createElement(
        View,
        null,
        React.createElement(Text, null, title),
        items.length === 0
          ? React.createElement(Text, { testID: 'empty-state-text' }, emptyText)
          : items.map((_item: unknown, i: number) =>
              React.createElement(
                Text,
                { key: i, testID: `download-item-${i}` },
                'download item',
              ),
            ),
      ),
  };
});

jest.mock('../../components/HistoryItem', () => {
  const React = require('react');
  const { View } = require('react-native');
  return {
    __esModule: true,
    default: ({ type }: { type: string }) =>
      React.createElement(View, { testID: `history-item-${type}` }),
  };
});

import DownloadsScreen from '../DownloadsScreen';

describe('DownloadsScreen', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('shows loading spinner initially', async () => {
    // The initial state has isLoading: true and useFocusEffect calls
    // loadDownloads via useEffect, but we don't resolve the promise yet
    mockGetDownloadHistory.mockImplementation(() => new Promise(() => {}));

    const { findByTestId } = render(<DownloadsScreen />);

    // Wait for effects to run (useFocusEffect schedules via useEffect)
    await act(async () => {});
    expect(await findByTestId('loading-spinner')).toBeTruthy();
  });

  it('shows downloads after loading', async () => {
    const downloadItems = [
      {
        id: 1,
        videoTitle: 'Test Video 1',
        videoDescription: 'Description 1',
        videoId: 'abc123',
        videoUrl: 'https://youtube.com/watch?v=abc123',
        imageUrl: 'https://i.ytimg.com/vi/abc123/default.jpg',
        downloadedAt: '2026-05-20T10:00:00Z',
      },
      {
        id: 2,
        videoTitle: 'Test Video 2',
        videoDescription: 'Description 2',
        videoId: 'def456',
        videoUrl: 'https://youtube.com/watch?v=def456',
        imageUrl: 'https://i.ytimg.com/vi/def456/default.jpg',
        downloadedAt: '2026-05-21T10:00:00Z',
      },
    ];

    mockGetDownloadHistory.mockResolvedValue(downloadItems);

    const { findByTestId } = render(<DownloadsScreen />);

    await act(async () => {});
    expect(await findByTestId('download-item-0')).toBeTruthy();
    expect(await findByTestId('download-item-1')).toBeTruthy();
  });

  it('shows empty state when no downloads', async () => {
    mockGetDownloadHistory.mockResolvedValue([]);

    const { findByTestId, findByText } = render(<DownloadsScreen />);

    await act(async () => {});
    expect(await findByText('history.downloads')).toBeTruthy();
    expect(await findByTestId('empty-state-text')).toBeTruthy();
  });

  it('shows error message on load failure', async () => {
    mockGetDownloadHistory.mockRejectedValue(new Error('Network failure'));

    const { findByTestId } = render(<DownloadsScreen />);

    await act(async () => {});
    expect(await findByTestId('error-message')).toBeTruthy();
  });
});
