import apiClient from './apiClient';
import ReactNativeBlobUtil from 'react-native-blob-util';
import { getToken } from './storageService';
import type {
  DownloadRequest,
  DownloadResponse,
  DownloadHistoryItem,
} from '../types/api';

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
  return response.data;
};

interface DownloadHistoryResponse {
  items: DownloadHistoryItem[];
}

export const getDownloadHistory = async (
  limit?: number,
): Promise<DownloadHistoryItem[]> => {
  const response = await apiClient.get<DownloadHistoryResponse>(
    '/yt/downloads/history',
    { params: { limit } },
  );
  return response.data.items;
};

export const archiveDownload = async (id: number): Promise<void> => {
  await apiClient.patch(`/yt/downloads/history/${id}/archive`);
};

export interface DownloadedFile {
  contentUri: string;
  displayPath: string;
  filePath: string;
}

export const downloadFile = async (
  downloadUrl: string,
  fileName: string,
): Promise<DownloadedFile> => {
  const token = await getToken();

  // Step 1: Download to internal cache (safe on scoped storage)
  let result;
  try {
    result = await ReactNativeBlobUtil.config({
      fileCache: true,
    }).fetch(
      'GET',
      downloadUrl,
      token ? { Authorization: `Bearer ${token}` } : {},
    );
  } catch (err) {
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

  // Check for HTTP errors
  if (result.respInfo?.status && result.respInfo.status >= 400) {
    throw new Error(`Server returned status ${result.respInfo.status}`);
  }

  // Step 2: Copy to public Downloads via MediaStore (scoped-storage compatible)
  const displayName = sanitizeFileName(fileName);
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
