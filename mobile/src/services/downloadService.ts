import apiClient from './apiClient';
import ReactNativeBlobUtil from 'react-native-blob-util';
import { getToken } from './storageService';
import type { DownloadRequest, DownloadResponse, DownloadHistoryItem } from '../types/api';

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
  const { dirs } = ReactNativeBlobUtil.fs;
  const destPath = `${dirs.DownloadDir}/${fileName}.mp3`;
  const result = await ReactNativeBlobUtil.config({
    fileCache: true,
    path: destPath,
    addAndroidDownloads: {
      useDownloadManager: true,
      notification: true,
      title: fileName,
      path: destPath,
    },
  }).fetch('GET', downloadUrl, token ? { Authorization: `Bearer ${token}` } : {});
  return result.path();
};
