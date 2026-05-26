import { useState, useCallback, useEffect, useRef } from 'react';
import TrackPlayer, {
  State,
  usePlaybackState,
  useProgress,
} from 'react-native-track-player';
import { getPreviewUrl, getPreviewHeaders } from '../services/previewService';

interface UsePreviewResult {
  isPlaying: boolean;
  isLoading: boolean;
  progress: number;
  elapsed: number;
  error: string | null;
  play: (videoId: string) => Promise<void>;
  stop: () => Promise<void>;
}

const PREVIEW_MAX_DURATION = 10;

const usePreview = (): UsePreviewResult => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  const playbackState = usePlaybackState();
  const { position, duration } = useProgress(250);

  const isPlaying = playbackState.state === State.Playing;

  // Derived values capped at 10 seconds
  const elapsed = Math.min(position, PREVIEW_MAX_DURATION);
  const cappedDuration =
    duration > 0
      ? Math.min(duration, PREVIEW_MAX_DURATION)
      : PREVIEW_MAX_DURATION;
  const progress = cappedDuration > 0 ? elapsed / cappedDuration : 0;

  // Auto-stop at 10 seconds
  useEffect(() => {
    if (position >= PREVIEW_MAX_DURATION && isPlaying) {
      TrackPlayer.pause();
    }
  }, [position, isPlaying]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      abortRef.current?.abort();
      TrackPlayer.reset();
    };
  }, []);

  const play = useCallback(async (videoId: string) => {
    // Cancel any previous in-flight request
    abortRef.current?.abort();

    const controller = new AbortController();
    abortRef.current = controller;

    setIsLoading(true);
    setError(null);

    try {
      await TrackPlayer.reset();

      if (controller.signal.aborted) {
        return;
      }

      const headers = await getPreviewHeaders();

      if (controller.signal.aborted) {
        return;
      }

      await TrackPlayer.add({
        id: videoId,
        url: getPreviewUrl(videoId),
        headers,
        title: 'Preview',
      });

      if (controller.signal.aborted) {
        return;
      }

      await TrackPlayer.play();
    } catch (err) {
      if (err instanceof Error && err.name === 'AbortError') {
        return;
      }
      setError(
        err instanceof Error ? err.message : 'Preview failed',
      );
    } finally {
      if (!controller.signal.aborted) {
        setIsLoading(false);
      }
    }
  }, []);

  const stop = useCallback(async () => {
    abortRef.current?.abort();
    await TrackPlayer.reset();
    setIsLoading(false);
    setError(null);
  }, []);

  return {
    isPlaying,
    isLoading,
    progress,
    elapsed,
    error,
    play,
    stop,
  };
};

export default usePreview;
