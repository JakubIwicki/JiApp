import { renderHook, act } from '@testing-library/react-native';
import useDownload from '../useDownload';
import * as downloadService from '../../../../services/downloadService';
import type { VideoItem } from '../../types/api';

jest.mock('../../../../services/downloadService', () => ({
  requestDownloadLink: jest.fn(),
  downloadFile: jest.fn(),
}));

const mockRequestDownloadLink =
  downloadService.requestDownloadLink as jest.Mock;
const mockDownloadFile = downloadService.downloadFile as jest.Mock;

const createVideoItem = (id: string): VideoItem => ({
  videoId: id,
  title: `Video ${id}`,
  description: `Description for video ${id}`,
  imageUrl: `https://example.com/${id}.jpg`,
  videoUrl: `https://example.com/${id}.mp4`,
  channelTitle: 'TestChannel',
});

describe('useDownload', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('passes AbortSignal to requestDownloadLink', async () => {
    mockRequestDownloadLink.mockResolvedValue({
      downloadUrl: 'https://example.com/dl',
    });
    mockDownloadFile.mockResolvedValue({
      displayPath: '/path/file.mp3',
      contentUri: 'content://test/file.mp3',
    });

    const video = createVideoItem('1');
    const { result } = renderHook(() => useDownload());

    await act(async () => {
      await result.current.download(video);
    });

    expect(mockRequestDownloadLink).toHaveBeenCalledWith(
      expect.objectContaining({ videoId: video.videoId }),
      expect.any(AbortSignal),
    );
  });

  it('does not set error state when request is aborted', async () => {
    const abortError = new Error('The operation was aborted');
    abortError.name = 'AbortError';
    mockRequestDownloadLink.mockRejectedValue(abortError);

    const video = createVideoItem('1');
    const { result } = renderHook(() => useDownload());

    await act(async () => {
      await result.current.download(video);
    });

    expect(result.current.isDownloading).toBe(false);
    expect(result.current.error).toBeNull();
    expect(result.current.localFilePath).toBeNull();
  });

  it('initialState has isDownloading=false, error=null, localFilePath=null', () => {
    const { result } = renderHook(() => useDownload());

    expect(result.current.isDownloading).toBe(false);
    expect(result.current.error).toBeNull();
    expect(result.current.localFilePath).toBeNull();
  });

  it('download() sets isDownloading=true, then sets localFilePath on success', async () => {
    const mockDownloadUrl = 'https://example.com/download/abc123';
    const mockFilePath = '/storage/emulated/0/Download/TestVideo.mp3';
    mockRequestDownloadLink.mockResolvedValue({
      downloadUrl: mockDownloadUrl,
    });
    mockDownloadFile.mockResolvedValue({
      displayPath: mockFilePath,
      contentUri: 'content://test/TestVideo.mp3',
    });

    const video = createVideoItem('1');
    const { result } = renderHook(() => useDownload());

    let promise: Promise<void>;
    act(() => {
      promise = result.current.download(video);
    });

    expect(result.current.isDownloading).toBe(true);
    expect(result.current.error).toBeNull();

    await act(async () => {
      await promise;
    });

    expect(result.current.isDownloading).toBe(false);
    expect(result.current.localFilePath).toBe(mockFilePath);
    expect(result.current.error).toBeNull();
    expect(mockRequestDownloadLink).toHaveBeenCalledWith(
      {
        videoId: video.videoId,
        videoUrl: video.videoUrl,
        title: video.title,
        description: video.description,
        imageUrl: video.imageUrl,
      },
      expect.any(AbortSignal),
    );
    expect(mockDownloadFile).toHaveBeenCalledWith(mockDownloadUrl, video.title);
  });

  it('download() sets error on API failure showing 502 yt-dlp error message', async () => {
    const apiError = {
      isAxiosError: true,
      response: {
        status: 502,
        data: { error: 'Failed to download video: Video unavailable' },
      },
      _serverError: 'Failed to download video: Video unavailable',
    };
    mockRequestDownloadLink.mockRejectedValue(apiError);

    const video = createVideoItem('1');
    const { result } = renderHook(() => useDownload());

    let promise: Promise<void>;
    act(() => {
      promise = result.current.download(video);
    });

    expect(result.current.isDownloading).toBe(true);

    await act(async () => {
      await promise;
    });

    expect(result.current.isDownloading).toBe(false);
    expect(result.current.error).toBe(
      'YouTube download failed: Failed to download video: Video unavailable',
    );
    expect(result.current.localFilePath).toBeNull();
    expect(mockDownloadFile).not.toHaveBeenCalled();
  });

  it('download() sets error on API failure showing 500 generic server message', async () => {
    const apiError = {
      isAxiosError: true,
      response: { status: 500, data: { error: 'Something broke' } },
      _serverError: 'Something broke',
    };
    mockRequestDownloadLink.mockRejectedValue(apiError);

    const video = createVideoItem('1');
    const { result } = renderHook(() => useDownload());

    let promise: Promise<void>;
    act(() => {
      promise = result.current.download(video);
    });

    expect(result.current.isDownloading).toBe(true);

    await act(async () => {
      await promise;
    });

    expect(result.current.isDownloading).toBe(false);
    expect(result.current.error).toBe('Server error — please try again later');
    expect(result.current.localFilePath).toBeNull();
    expect(mockDownloadFile).not.toHaveBeenCalled();
  });

  it('download() sets error on file download failure (downloadFile fails) with fallback', async () => {
    const mockDownloadUrl = 'https://example.com/download/abc123';
    mockRequestDownloadLink.mockResolvedValue({
      downloadUrl: mockDownloadUrl,
    });
    const fileError = new Error('File download failed');
    mockDownloadFile.mockRejectedValue(fileError);

    const video = createVideoItem('1');
    const { result } = renderHook(() => useDownload());

    let promise: Promise<void>;
    act(() => {
      promise = result.current.download(video);
    });

    expect(result.current.isDownloading).toBe(true);

    await act(async () => {
      await promise;
    });

    expect(result.current.isDownloading).toBe(false);
    expect(result.current.error).toBe('File download failed');
    expect(result.current.localFilePath).toBeNull();
    expect(mockRequestDownloadLink).toHaveBeenCalled();
    expect(mockDownloadFile).toHaveBeenCalled();
  });

  it('download() sets generic error when thrown error is not an Error instance', async () => {
    mockRequestDownloadLink.mockRejectedValue('String error');

    const video = createVideoItem('1');
    const { result } = renderHook(() => useDownload());

    await act(async () => {
      await result.current.download(video);
    });

    expect(result.current.isDownloading).toBe(false);
    expect(result.current.error).toBe('Download failed');
  });
});
