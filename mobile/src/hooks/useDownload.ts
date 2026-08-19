import { useState, useCallback, useEffect, useRef } from 'react';
import {
  requestDownloadLink,
  downloadFile,
  openAudioFile,
} from '../services/downloadService';
import { waitForDownload } from '../services/downloadJob';
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
  const filePathRef = useRef<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  // mount only — read the ref at cleanup time so the in-flight controller
  // (created after mount) is aborted when the screen unmounts
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
      const { tempId, downloadUrl } = await requestDownloadLink(
        {
          videoId: video.videoId,
          videoUrl: video.videoUrl,
          title: video.title,
          description: video.description,
          imageUrl: video.imageUrl,
        },
        controller.signal,
      );

      await waitForDownload(tempId, controller.signal);

      const file = await downloadFile(
        downloadUrl,
        video.title,
        controller.signal,
      );
      setLocalFilePath(file.displayPath);
      contentUriRef.current = file.contentUri;
      filePathRef.current = file.filePath;
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
    // Use the cached file path (real file) instead of content:// URI,
    // since Samsung Music and other players have trouble with content URIs.
    const path = filePathRef.current || contentUriRef.current;
    if (!path) return;
    await openAudioFile(path, chooserTitle);
  }, []);

  const reset = useCallback(() => {
    setIsDownloading(false);
    setError(null);
    setLocalFilePath(null);
    contentUriRef.current = null;
    filePathRef.current = null;
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
