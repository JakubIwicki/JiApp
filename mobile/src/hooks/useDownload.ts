import { useState, useCallback, useEffect, useRef } from 'react';
import {
  requestDownloadLink,
  downloadFile,
  openAudioFile,
} from '../services/downloadService';
import { getDownloadErrorMessage } from '../utils/errorUtils';
import type { VideoItem } from '../types/api';

interface UseDownloadResult {
  isDownloading: boolean;
  error: string | null;
  localFilePath: string | null;
  download: (video: VideoItem) => Promise<void>;
  playInMusicPlayer: (chooserTitle: string) => Promise<void>;
  reset: () => void;
}

const useDownload = (): UseDownloadResult => {
  const [isDownloading, setIsDownloading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [localFilePath, setLocalFilePath] = useState<string | null>(null);
  const contentUriRef = useRef<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    const controller = abortRef.current;
    return () => {
      controller?.abort();
    };
  }, [abortRef]);

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

      const file = await downloadFile(downloadUrl, video.title);
      setLocalFilePath(file.displayPath);
      contentUriRef.current = file.contentUri;
    } catch (err) {
      if (err instanceof Error && err.name === 'AbortError') {
        return;
      }
      setError(getDownloadErrorMessage(err));
    } finally {
      setIsDownloading(false);
    }
  }, []);

  const playInMusicPlayer = useCallback(async (chooserTitle: string) => {
    const uri = contentUriRef.current;
    if (!uri) return;
    await openAudioFile(uri, chooserTitle);
  }, []);

  const reset = useCallback(() => {
    setIsDownloading(false);
    setError(null);
    setLocalFilePath(null);
    contentUriRef.current = null;
  }, []);

  return {
    isDownloading,
    error,
    localFilePath,
    download,
    playInMusicPlayer,
    reset,
  };
};

export default useDownload;
