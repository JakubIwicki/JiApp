import { useState, useCallback, useEffect, useRef } from 'react';
import {
  requestDownloadLink,
  downloadFile,
} from '../services/downloadService';
import { getDownloadErrorMessage } from '../utils/errorUtils';
import type { VideoItem } from '../types/api';

interface UseDownloadResult {
  isDownloading: boolean;
  error: string | null;
  localFilePath: string | null;
  download: (video: VideoItem) => Promise<void>;
  reset: () => void;
}

const useDownload = (): UseDownloadResult => {
  const [isDownloading, setIsDownloading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [localFilePath, setLocalFilePath] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    return () => {
      abortRef.current?.abort();
    };
  }, []);

  const download = useCallback(async (video: VideoItem) => {
    // Cancel any previous in-flight request
    abortRef.current?.abort();

    const controller = new AbortController();
    abortRef.current = controller;

    setIsDownloading(true);
    setError(null);
    setLocalFilePath(null);

    try {
      const { downloadUrl } = await requestDownloadLink(
        {
          videoId: video.videoId,
          videoUrl: video.videoUrl,
          title: video.title,
          description: video.description,
          imageUrl: video.imageUrl,
        },
        controller.signal,
      );

      const filePath = await downloadFile(downloadUrl, video.title);
      setLocalFilePath(filePath);
    } catch (err) {
      if (err instanceof Error && err.name === 'AbortError') {
        return;
      }
      setError(getDownloadErrorMessage(err));
    } finally {
      setIsDownloading(false);
    }
  }, []);

  const reset = useCallback(() => {
    setIsDownloading(false);
    setError(null);
    setLocalFilePath(null);
  }, []);

  return {
    isDownloading,
    error,
    localFilePath,
    download,
    reset,
  };
};

export default useDownload;
