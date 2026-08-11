import { renderHook, act } from '@testing-library/react-native';
import useDownloads from '../useDownloads';
import * as downloadService from '../../services/downloadService';
import type { DownloadHistoryItem } from '../../types/api';

const mockShowSuccess = jest.fn();
const mockShowError = jest.fn();

jest.mock('../useToast', () => ({
  __esModule: true,
  default: () => ({
    showSuccess: mockShowSuccess,
    showError: mockShowError,
    showInfo: jest.fn(),
    showWarning: jest.fn(),
  }),
}));

jest.mock('../../services/downloadService', () => ({
  getDownloadHistory: jest.fn(),
  archiveDownload: jest.fn(),
}));

const mockGetDownloadHistory = downloadService.getDownloadHistory as jest.Mock;
const mockArchiveDownload = downloadService.archiveDownload as jest.Mock;

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

describe('useDownloads', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('has initial state with empty downloads and no fetch fired', () => {
    const { result } = renderHook(() => useDownloads());

    expect(result.current.downloads).toEqual([]);
    expect(result.current.isLoading).toBe(true);
    expect(result.current.isRefreshing).toBe(false);
    expect(result.current.error).toBeNull();
    expect(mockGetDownloadHistory).not.toHaveBeenCalled();
  });

  it('loadDownloads fetches exactly once and populates state', async () => {
    const items = [
      createDownloadItem({ id: 1 }),
      createDownloadItem({ id: 2, videoTitle: 'Second Video' }),
    ];
    mockGetDownloadHistory.mockResolvedValue(items);

    const { result } = renderHook(() => useDownloads());

    let promise: Promise<void>;
    act(() => {
      promise = result.current.loadDownloads(false);
    });

    expect(mockGetDownloadHistory).toHaveBeenCalledTimes(1);

    await act(async () => {
      await promise;
    });

    expect(result.current.downloads).toEqual(items);
    expect(result.current.isLoading).toBe(false);
    expect(result.current.isRefreshing).toBe(false);
    expect(result.current.error).toBeNull();
    expect(mockGetDownloadHistory).toHaveBeenCalledTimes(1);
  });

  it('loadDownloads(true) flags a refresh without flipping isLoading', async () => {
    mockGetDownloadHistory.mockResolvedValue([createDownloadItem()]);

    const { result } = renderHook(() => useDownloads());

    let promise: Promise<void>;
    act(() => {
      promise = result.current.loadDownloads(true);
    });

    expect(result.current.isRefreshing).toBe(true);
    expect(result.current.isLoading).toBe(true);

    await act(async () => {
      await promise;
    });

    expect(result.current.isRefreshing).toBe(false);
    expect(result.current.isLoading).toBe(false);
  });

  it('sets error when the fetch fails and returns isLoading to false', async () => {
    mockGetDownloadHistory.mockRejectedValue(new Error('Network down'));

    const { result } = renderHook(() => useDownloads());

    await act(async () => {
      await result.current.loadDownloads(false);
    });

    expect(result.current.error).toBe('Network down');
    expect(result.current.isLoading).toBe(false);
    expect(result.current.isRefreshing).toBe(false);
    expect(result.current.downloads).toEqual([]);
  });

  it('sets a string error when the thrown value is not an Error instance', async () => {
    mockGetDownloadHistory.mockRejectedValue('Boom');

    const { result } = renderHook(() => useDownloads());

    await act(async () => {
      await result.current.loadDownloads(false);
    });

    expect(result.current.error).toBe('Boom');
  });

  it('archiveDownload removes the entry, calls the service with the id, and shows success without refetching', async () => {
    const items = [
      createDownloadItem({ id: 1 }),
      createDownloadItem({ id: 2, videoTitle: 'Second Video' }),
    ];
    mockGetDownloadHistory.mockResolvedValue(items);
    mockArchiveDownload.mockResolvedValue(undefined);

    const { result } = renderHook(() => useDownloads());

    await act(async () => {
      await result.current.loadDownloads(false);
    });

    await act(async () => {
      await result.current.archiveDownload(1);
    });

    expect(mockArchiveDownload).toHaveBeenCalledTimes(1);
    expect(mockArchiveDownload).toHaveBeenCalledWith(1);
    expect(mockShowSuccess).toHaveBeenCalledWith('toast.downloadArchived');
    expect(mockShowError).not.toHaveBeenCalled();
    expect(result.current.downloads).toEqual([
      createDownloadItem({ id: 2, videoTitle: 'Second Video' }),
    ]);
    expect(mockGetDownloadHistory).toHaveBeenCalledTimes(1);
  });

  it('archiveDownload on failure shows error and refetches the list', async () => {
    mockGetDownloadHistory.mockResolvedValue([
      createDownloadItem({ id: 1 }),
      createDownloadItem({ id: 2, videoTitle: 'Second Video' }),
    ]);
    mockArchiveDownload.mockRejectedValue(new Error('Archive failed'));

    const { result } = renderHook(() => useDownloads());

    await act(async () => {
      await result.current.loadDownloads(false);
    });
    expect(mockGetDownloadHistory).toHaveBeenCalledTimes(1);

    // Server still holds the archived row, so the refetch brings it back
    mockGetDownloadHistory.mockResolvedValue([createDownloadItem({ id: 1 })]);

    await act(async () => {
      await result.current.archiveDownload(2);
    });

    expect(mockArchiveDownload).toHaveBeenCalledWith(2);
    expect(mockShowError).toHaveBeenCalledWith('toast.archiveFailed');
    expect(mockShowSuccess).not.toHaveBeenCalled();
    expect(mockGetDownloadHistory).toHaveBeenCalledTimes(2);
    expect(result.current.downloads).toEqual([createDownloadItem({ id: 1 })]);
    expect(result.current.isLoading).toBe(false);
  });
});
