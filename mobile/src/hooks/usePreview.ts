import { useState, useCallback, useEffect, useRef } from 'react';
import TrackPlayer, {
  State,
  Event,
  usePlaybackState,
  useProgress,
  useTrackPlayerEvents,
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

  const state = playbackState.state;
  const isPlaying = state === State.Playing || state === State.Buffering;

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

  // Clear loading on Playing / Buffering; surface TrackPlayer errors
  useTrackPlayerEvents([Event.PlaybackState], event => {
    if (event.state === State.Playing || event.state === State.Buffering) {
      setIsLoading(false);
    }
    if (event.state === State.Error && 'error' in event && event.error) {
      setError(event.error.message);
      setIsLoading(false);
    }
  });

  // Cleanup on unmount
  useEffect(() => {
    const controller = abortRef.current;
    return () => {
      controller?.abort();
      TrackPlayer.reset();
    };
  }, [abortRef]);

  const play = useCallback(async (videoId: string) => {
    // Cancel any previous in-flight request
    abortRef.current?.abort();

    const controller = new AbortController();
    abortRef.current = controller;

    setIsLoading(true);
    setError(null);

    try {
      if (controller.signal.aborted) {
        return;
      }

      await TrackPlayer.reset();
      const abortedAfterReset = controller.signal.aborted;
      if (abortedAfterReset) {
        return;
      }

      const headers = await getPreviewHeaders();
      const abortedAfterHeaders = controller.signal.aborted;
      if (abortedAfterHeaders) {
        return;
      }

      await TrackPlayer.add({
        id: videoId,
        url: getPreviewUrl(videoId),
        headers,
        title: 'Preview',
        contentType: 'audio/mpeg',
      });
      const abortedAfterAdd = controller.signal.aborted;
      if (abortedAfterAdd) {
        return;
      }

      await TrackPlayer.play();
    } catch (err) {
      if (err instanceof Error && err.name === 'AbortError') {
        if (abortRef.current === controller) {
          setIsLoading(false);
        }
        return;
      }
      if (abortRef.current === controller) {
        setError(err instanceof Error ? err.message : 'Preview failed');
        setIsLoading(false);
      }
    }
    // isLoading stays true until playback state confirms Playing
    // preventing UI flicker between play() completing and actual playback starting
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
