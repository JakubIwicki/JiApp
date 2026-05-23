import { isAxiosError } from 'axios';

export const getErrorMessage = (
  err: unknown,
  fallback: string,
): string => (err instanceof Error ? err.message : fallback);

interface AxiosErrorWithServerError {
  response?: {
    status: number;
    data?: unknown;
  };
  _serverError?: string;
  code?: string;
}

export const getDownloadErrorMessage = (err: unknown): string => {
  if (isAxiosError(err)) {
    const axiosErr = err as AxiosErrorWithServerError;

    // Network error (no response received)
    if (!axiosErr.response) {
      return 'Connection failed — check your network';
    }

    // 502 Bad Gateway: yt-dlp failure from backend
    if (axiosErr.response.status === 502 && axiosErr._serverError) {
      return `YouTube download failed: ${axiosErr._serverError}`;
    }

    // 500 Internal Server Error: generic server failure
    if (axiosErr.response.status === 500) {
      return 'Server error — please try again later';
    }
  }

  return 'Download failed';
};
