import { isAxiosError } from 'axios';
import i18next from '../i18n';
import { getDownloadStatus } from './downloadService';
import type { DownloadStatus } from '../types/api';

/** Interval between status polls while a download job is in flight. */
const POLL_INTERVAL_MS = 2500;

/** Hard cap on how long a job may run before we give up. */
const DOWNLOAD_TIMEOUT_MS = 15 * 60 * 1000;

/**
 * Gateway 429s and 5xx are transient blips — the job keeps running
 * server-side, so a poll that hits one must not fail the download.
 */
const isTransientStatusError = (err: unknown): boolean => {
  if (!isAxiosError(err)) {
    return false;
  }
  const status = err.response?.status;
  return status === 429 || (status !== undefined && status >= 500);
};

const createAbortError = (): Error => {
  const error = new Error('The operation was aborted');
  error.name = 'AbortError';
  return error;
};

const delay = (ms: number, signal?: AbortSignal): Promise<void> =>
  new Promise<void>((resolve, reject) => {
    if (signal?.aborted) {
      reject(createAbortError());
      return;
    }
    const timer = setTimeout(() => {
      signal?.removeEventListener('abort', onAbort);
      resolve();
    }, ms);
    const onAbort = () => {
      clearTimeout(timer);
      signal?.removeEventListener('abort', onAbort);
      reject(createAbortError());
    };
    signal?.addEventListener('abort', onAbort);
  });

/**
 * Poll a download job until it is 'ready' or 'failed'.
 * Rejects with the server's error message on failure, throws after the
 * timeout cap, and honours the optional AbortSignal at every step.
 */
export const waitForDownload = async (
  tempId: string,
  signal?: AbortSignal,
): Promise<DownloadStatus> => {
  const deadline = Date.now() + DOWNLOAD_TIMEOUT_MS;

  for (;;) {
    if (signal?.aborted) {
      throw createAbortError();
    }

    let status: DownloadStatus | undefined;
    try {
      status = await getDownloadStatus(tempId);
    } catch (err) {
      if (signal?.aborted) {
        throw createAbortError();
      }
      // Transient gateway blips don't fail the job — keep polling.
      if (!isTransientStatusError(err)) {
        throw err;
      }
    }

    if (status) {
      if (status.status === 'ready') {
        return status;
      }
      if (status.status === 'failed') {
        throw new Error(status.error ?? i18next.t('download.failed'));
      }
    }

    if (Date.now() >= deadline) {
      throw new Error(i18next.t('download.timedOut'));
    }

    await delay(POLL_INTERVAL_MS, signal);
  }
};
