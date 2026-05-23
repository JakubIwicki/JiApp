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

export const downloadFile = async (
  downloadUrl: string,
  fileName: string,
): Promise<string> => {
  const token = await getToken();

  // Step 1: Download to internal cache (safe on scoped storage)
  const result = await ReactNativeBlobUtil.config({
    fileCache: true,
  }).fetch('GET', downloadUrl, token ? { Authorization: `Bearer ${token}` } : {});

  // Step 2: Copy to public Downloads via MediaStore (scoped-storage compatible)
  const destPath = await ReactNativeBlobUtil.MediaCollection.copyToMediaStore(
    {
      name: `${sanitizeFileName(fileName)}.mp3`,
      parentFolder: '',
      mimeType: 'audio/mpeg',
    },
    'Download',
    result.path(),
  );

  return `Download/${sanitizeFileName(fileName)}.mp3`;
};
