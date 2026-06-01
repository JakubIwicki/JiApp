import React from 'react';
import { render, fireEvent, waitFor } from '@testing-library/react-native';

// Mock useDownload
const mockDownload = jest.fn();
const mockReset = jest.fn();
let mockIsDownloading = false;
let mockError: string | null = null;
let mockLocalFilePath: string | null = null;

jest.mock('../../hooks/useDownload', () => ({
  __esModule: true,
  default: () => ({
    isDownloading: mockIsDownloading,
    error: mockError,
    localFilePath: mockLocalFilePath,
    download: mockDownload,
    reset: mockReset,
  }),
}));

// Mock react-i18next
jest.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

// Mock @react-navigation/native
const mockNavigate = jest.fn();
jest.mock('@react-navigation/native', () => {
  const actual = jest.requireActual('@react-navigation/native');
  return {
    ...actual,
    useNavigation: () => ({
      navigate: mockNavigate,
      setOptions: jest.fn(),
    }),
    useRoute: () => ({
      params: {
        videoId: 'test-video-123',
        title: 'Test Video Title',
        description: 'Test video description for unit testing',
        imageUrl: 'https://example.com/thumb.jpg',
        videoUrl: 'https://example.com/video.mp4',
      },
    }),
  };
});

import DownloadScreen from '../DownloadScreen';
import { act } from '@testing-library/react-native';

describe('DownloadScreen', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockIsDownloading = false;
    mockError = null;
    mockLocalFilePath = null;
    mockDownload.mockResolvedValue(undefined);
  });

  it('renders video info (thumbnail, title, description) from navigation params', () => {
    const { getByText, getByTestId } = render(<DownloadScreen />);

    expect(getByText('Test Video Title')).toBeTruthy();
    expect(getByText('Test video description for unit testing')).toBeTruthy();
    expect(getByTestId('download-thumbnail')).toBeTruthy();
  });

  it('shows "Download MP3" button in initial state', () => {
    const { getByText } = render(<DownloadScreen />);

    expect(getByText('download.downloadMp3')).toBeTruthy();
  });

  it('shows loading spinner during download', () => {
    mockIsDownloading = true;

    const { getByTestId, queryByText } = render(<DownloadScreen />);

    expect(getByTestId('loading-spinner')).toBeTruthy();
    expect(queryByText('download.downloadMp3')).toBeNull();
  });

  it('shows success message with file path after successful download', () => {
    mockLocalFilePath = '/storage/emulated/0/Download/TestVideo.mp3';

    const { getByText } = render(<DownloadScreen />);

    expect(getByText('download.success')).toBeTruthy();
    expect(getByText('download.fileSaved')).toBeTruthy();
    expect(getByText('/storage/emulated/0/Download/TestVideo.mp3')).toBeTruthy();
  });

  it('shows success actions (back to search and view history) after download', () => {
    mockLocalFilePath = '/storage/emulated/0/Download/TestVideo.mp3';

    const { getByText } = render(<DownloadScreen />);

    expect(getByText('download.goBack')).toBeTruthy();
    expect(getByText('download.viewHistory')).toBeTruthy();
  });

  it('navigates to Search on "go back" press', () => {
    mockLocalFilePath = '/storage/emulated/0/Download/TestVideo.mp3';

    const { getByText } = render(<DownloadScreen />);
    fireEvent.press(getByText('download.goBack'));

    expect(mockNavigate).toHaveBeenCalledWith('Search');
  });

  it('navigates to DownloadsTab on "view history" press', () => {
    mockLocalFilePath = '/storage/emulated/0/Download/TestVideo.mp3';

    const { getByText } = render(<DownloadScreen />);
    fireEvent.press(getByText('download.viewHistory'));

    expect(mockNavigate).toHaveBeenCalledWith('DownloadsTab');
  });

  it('shows error message on download failure', () => {
    mockError = 'Network error';

    const { getByText } = render(<DownloadScreen />);

    expect(getByText('download.failed: Network error')).toBeTruthy();
  });

  it('shows retry button on error', () => {
    mockError = 'Network error';

    const { getByText } = render(<DownloadScreen />);

    expect(getByText('common.retry')).toBeTruthy();
  });

  it('calls download() when Download MP3 button is pressed', async () => {
    const { getByText } = render(<DownloadScreen />);

    fireEvent.press(getByText('download.downloadMp3'));

    await waitFor(() => {
      expect(mockDownload).toHaveBeenCalledTimes(1);
      expect(mockDownload).toHaveBeenCalledWith({
        videoId: 'test-video-123',
        title: 'Test Video Title',
        description: 'Test video description for unit testing',
        imageUrl: 'https://example.com/thumb.jpg',
        videoUrl: 'https://example.com/video.mp4',
        channelTitle: '',
      });
    });
  });
});
