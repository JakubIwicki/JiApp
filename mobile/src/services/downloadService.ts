import apiClient from './apiClient';
import ReactNativeBlobUtil from 'react-native-blob-util';
import { getToken } from './storageService';
import type { DownloadRequest, DownloadResponse, DownloadHistoryItem } from '../types/api';

const sanitizeFileName = (name: string): string =>
  name.replace(/[/\\:*?"<>|]/g, '').trim() || 'download';

export const requestDownloadLink = async (
  request: DownloadRequest,
  signal?: AbortSignal,
): Promise<DownloadResponse> => {
  const response = await apiClient.post<DownloadResponse>(
    '/downloads/mp3',
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
    '/downloads/history',
    { params: { limit } },
  );
  return response.data.items;
};

export const archiveDownload = async (id: number): Promise<void> => {
  await apiClient.patch(`/downloads/history/${id}/archive`);
};

export interface DownloadedFile {
  contentUri: string;
  displayPath: string;
}

export const downloadFile = async (
  downloadUrl: string,
  fileName: string,
): Promise<DownloadedFile> => {
  const token = await getToken();

  // Step 1: Download to internal cache (safe on scoped storage)
  const result = await ReactNativeBlobUtil.config({
    fileCache: true,
  }).fetch('GET', downloadUrl, token ? { Authorization: `Bearer ${token}` } : {});

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

  return { contentUri, displayPath: `Download/${displayName}.mp3` };
};

export const openAudioFile = (
  filePath: string,
  chooserTitle: string,
): Promise<boolean | null> =>
  ReactNativeBlobUtil.android.actionViewIntent(filePath, 'audio/mpeg', chooserTitle);
