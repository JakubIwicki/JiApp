import { API_BASE_URL } from '../config';
import { getAuthToken } from './storageService';

export function getPreviewUrl(videoId: string): string {
  return `${API_BASE_URL}/preview/${videoId}`;
}

export async function getPreviewHeaders(): Promise<Record<string, string>> {
  const token = await getAuthToken();
  return token
    ? { Authorization: `Bearer ${token}` }
    : {};
}
