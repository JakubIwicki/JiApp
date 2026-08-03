import { waitForDownload } from '../downloadJob';
import { getDownloadStatus } from '../downloadService';

jest.mock('../downloadService', () => ({
  getDownloadStatus: jest.fn(),
}));

jest.mock('../../i18n', () => ({
  __esModule: true,
  default: {
    t: (key: string) => {
      if (key === 'download.timedOut') {
        return 'Download timed out — please try again';
      }
      if (key === 'download.failed') {
        return 'Download failed';
      }
      return key;
    },
    language: 'en',
  },
}));

const mockGetDownloadStatus = getDownloadStatus as jest.Mock;

describe('waitForDownload', () => {
  beforeEach(() => {
    jest.useFakeTimers();
    jest.clearAllMocks();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it('resolves immediately when the job is already ready', async () => {
    mockGetDownloadStatus.mockResolvedValue({ status: 'ready' });

    await expect(waitForDownload('job-1')).resolves.toEqual({
      status: 'ready',
    });

    expect(mockGetDownloadStatus).toHaveBeenCalledTimes(1);
    expect(mockGetDownloadStatus).toHaveBeenCalledWith('job-1');
  });

  it('polls until the job is ready', async () => {
    mockGetDownloadStatus
      .mockResolvedValueOnce({ status: 'pending' })
      .mockResolvedValueOnce({ status: 'running' })
      .mockResolvedValueOnce({ status: 'ready' });

    const promise = waitForDownload('job-1');

    await jest.advanceTimersByTimeAsync(2500);
    await jest.advanceTimersByTimeAsync(2500);

    await expect(promise).resolves.toEqual({ status: 'ready' });
    expect(mockGetDownloadStatus).toHaveBeenCalledTimes(3);
  });

  it('throws the server error message when the job fails', async () => {
    mockGetDownloadStatus.mockResolvedValue({
      status: 'failed',
      error: 'Video unavailable',
    });

    await expect(waitForDownload('job-1')).rejects.toThrow('Video unavailable');
  });

  it('throws the fallback message when the job fails without an error', async () => {
    mockGetDownloadStatus.mockResolvedValue({ status: 'failed' });

    await expect(waitForDownload('job-1')).rejects.toThrow('Download failed');
  });

  it('throws a timed out error after the cap while the job stays pending', async () => {
    mockGetDownloadStatus.mockResolvedValue({ status: 'pending' });

    const promise = waitForDownload('job-1');
    // Attach a handler up front so the rejection is captured during the advance
    const caught = promise.catch((err: unknown) => err);

    await jest.advanceTimersByTimeAsync(15 * 60 * 1000 + 1);

    const error = await caught;
    expect(error).toBeInstanceOf(Error);
    expect((error as Error).message).toBe(
      'Download timed out — please try again',
    );
  });

  it('rejects with AbortError when the signal fires during a poll interval', async () => {
    mockGetDownloadStatus.mockResolvedValue({ status: 'pending' });

    const controller = new AbortController();
    const promise = waitForDownload('job-1', controller.signal);

    // Let the first status request resolve so the loop waits on the interval
    await Promise.resolve();

    controller.abort();

    await expect(promise).rejects.toThrow('The operation was aborted');
  });

  it('propagates a status fetch error when not aborted', async () => {
    mockGetDownloadStatus.mockRejectedValue(new Error('Status fetch failed'));

    await expect(waitForDownload('job-1')).rejects.toThrow(
      'Status fetch failed',
    );
  });

  it('continues polling after a transient 429 and resolves when ready', async () => {
    mockGetDownloadStatus
      .mockRejectedValueOnce({ isAxiosError: true, response: { status: 429 } })
      .mockResolvedValueOnce({ status: 'ready' });

    const promise = waitForDownload('job-1');

    await jest.advanceTimersByTimeAsync(2500);

    await expect(promise).resolves.toEqual({ status: 'ready' });
    expect(mockGetDownloadStatus).toHaveBeenCalledTimes(2);
  });

  it('continues polling after a transient 500 and resolves when ready', async () => {
    mockGetDownloadStatus
      .mockRejectedValueOnce({ isAxiosError: true, response: { status: 500 } })
      .mockResolvedValueOnce({ status: 'ready' });

    const promise = waitForDownload('job-1');

    await jest.advanceTimersByTimeAsync(2500);

    await expect(promise).resolves.toEqual({ status: 'ready' });
    expect(mockGetDownloadStatus).toHaveBeenCalledTimes(2);
  });

  it('throws when the status request fails with a non-transient error', async () => {
    const notFoundError = {
      isAxiosError: true,
      response: { status: 404 },
    };
    mockGetDownloadStatus.mockRejectedValue(notFoundError);

    await expect(waitForDownload('job-1')).rejects.toBe(notFoundError);
  });
});
