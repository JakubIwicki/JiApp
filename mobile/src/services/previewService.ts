import { API_BASE_URL } from '../config';
import { getToken } from './storageService';

export function getPreviewUrl(videoId: string): string {
  return `${API_BASE_URL}/yt/preview/${videoId}`;
}

export async function getPreviewHeaders(): Promise<Record<string, string>> {
  const token = await getToken();
  return token ? { Authorization: `Bearer ${token}` } : {};
}
