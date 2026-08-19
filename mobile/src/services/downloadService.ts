import apiClient from './apiClient';
import ReactNativeBlobUtil from 'react-native-blob-util';
import type { FetchBlobResponse } from 'react-native-blob-util';
import { getToken } from './storageService';
import { createAbortError } from '../utils/errorUtils';
import type {
  DownloadRequest,
  DownloadResponse,
  DownloadStatus,
  DownloadHistoryItem,
} from '../types/api';
import {
  DownloadResponseSchema,
  DownloadStatusSchema,
  DownloadHistoryResponseSchema,
} from '../types/schemas';

const sanitizeFileName = (name: string): string =>
  name.replace(/[/\\:*?"<>|]/g, '').trim() || 'download';

export const requestDownloadLink = async (
  request: DownloadRequest,
  signal?: AbortSignal,
): Promise<DownloadResponse> => {
  const response = await apiClient.post<DownloadResponse>(
    '/yt/downloads/mp3',
    request,
    { signal },
  );
  return DownloadResponseSchema.parse(response.data);
};

export const getDownloadStatus = async (
  tempId: string,
  signal?: AbortSignal,
): Promise<DownloadStatus> => {
  const response = await apiClient.get<DownloadStatus>(
    `/yt/downloads/mp3/status/${tempId}`,
    { signal },
  );
  return DownloadStatusSchema.parse(response.data);
};

export const getDownloadHistory = async (
  limit?: number,
): Promise<DownloadHistoryItem[]> => {
  const response = await apiClient.get<{ items: DownloadHistoryItem[] }>(
    '/yt/downloads/history',
    { params: { limit } },
  );
  return DownloadHistoryResponseSchema.parse(response.data).items;
};

export const archiveDownload = async (id: number): Promise<void> => {
  await apiClient.patch(`/yt/downloads/history/${id}/archive`);
};

export interface DownloadedFile {
  contentUri: string;
  displayPath: string;
  filePath: string;
}

/**
 * RNBlobUtil cancels a fetch by rejecting with CanceledFetchError('canceled')
 * (see fetch.js promise.cancel) — classify that rejection shape as an abort.
 */
const isCanceledFetch = (err: unknown): boolean =>
  err instanceof Error &&
  (err.name === 'ReactNativeBlobUtilCanceledFetch' ||
    err.message.toLowerCase().includes('cancel'));

export const downloadFile = async (
  downloadUrl: string,
  fileName: string,
  signal?: AbortSignal,
): Promise<DownloadedFile> => {
  const token = await getToken();

  // Step 1: Download to internal cache with a named file (safe on scoped storage)
  const displayName = sanitizeFileName(fileName);
  const cachePath = `${ReactNativeBlobUtil.fs.dirs.CacheDir}/${displayName}.mp3`;

  // Remove stale file from a previous download of the same song
  if (await ReactNativeBlobUtil.fs.exists(cachePath)) {
    await ReactNativeBlobUtil.fs.unlink(cachePath);
  }

  let result: FetchBlobResponse;
  try {
    const task = ReactNativeBlobUtil.config({
      path: cachePath,
    }).fetch(
      'GET',
      downloadUrl,
      token ? { Authorization: `Bearer ${token}` } : {},
    );

    // Cancel the blob task when the caller aborts, so an unmount stops the
    // stream promptly (the library's 60s default timeout is the backstop).
    const cancel = () => {
      task.cancel();
    };
    if (signal?.aborted) {
      cancel();
    } else {
      signal?.addEventListener('abort', cancel);
    }

    try {
      result = await task;
    } finally {
      signal?.removeEventListener('abort', cancel);
    }
  } catch (err) {
    if (isCanceledFetch(err)) {
      throw createAbortError();
    }
    if (err instanceof Error) {
      const msg = err.message.toLowerCase();
      if (
        msg.includes('cert') ||
        msg.includes('ssl') ||
        msg.includes('handshake')
      ) {
        throw new Error(
          'SSL connection failed. The development certificate may not be trusted.',
        );
      }
    }
    throw err;
  }

  // cancel() is a no-op after the task resolves, so an abort landing here must
  // not let the file reach the public Downloads folder via copyToMediaStore.
  if (signal?.aborted) {
    await ReactNativeBlobUtil.fs.unlink(cachePath);
    throw createAbortError();
  }

  // Check for HTTP errors
  if (result.respInfo?.status && result.respInfo.status >= 400) {
    throw new Error(`Server returned status ${result.respInfo.status}`);
  }

  // Step 2: Copy to public Downloads via MediaStore (scoped-storage compatible)
  const contentUri = await ReactNativeBlobUtil.MediaCollection.copyToMediaStore(
    {
      name: `${displayName}.mp3`,
      parentFolder: '',
      mimeType: 'audio/mpeg',
    },
    'Download',
    result.path(),
  );

  return {
    contentUri,
    displayPath: `Download/${displayName}.mp3`,
    filePath: result.path(),
  };
};

export const openAudioFile = (
  filePath: string,
  _chooserTitle: string,
): Promise<boolean | null> =>
  ReactNativeBlobUtil.android.actionViewIntent(filePath, 'audio/mpeg');
