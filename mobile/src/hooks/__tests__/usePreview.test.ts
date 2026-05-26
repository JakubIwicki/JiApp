import { renderHook, act } from '@testing-library/react-native';
import usePreview from '../usePreview';
import type TrackPlayerModule from 'react-native-track-player';

// Shared mutable state for mock TrackPlayer hooks
let mockPlaybackState: { state: string | undefined } = { state: undefined };
let mockProgress = { position: 0, duration: 0, buffered: 0 };

jest.mock('react-native-track-player', () => {
  const State = {
    None: 'none',
    Ready: 'ready',
    Playing: 'playing',
    Paused: 'paused',
    Stopped: 'stopped',
    Loading: 'loading',
    Buffering: 'buffering',
    Error: 'error',
    Ended: 'ended',
  };

  const TrackPlayerMock = {
    reset: jest.fn<Promise<void>, []>().mockResolvedValue(undefined),
    add: jest.fn<Promise<void>, []>().mockResolvedValue(undefined),
    play: jest.fn<Promise<void>, []>().mockResolvedValue(undefined),
    pause: jest.fn<Promise<void>, []>().mockResolvedValue(undefined),
    addEventListener: jest.fn().mockReturnValue({ remove: jest.fn() }),
  };

  return {
    __esModule: true,
    default: TrackPlayerMock,
    ...TrackPlayerMock,
    State,
    Event: {
      PlaybackState: 'playback-state',
      PlaybackActiveTrackChanged: 'playback-active-track-changed',
      PlaybackProgressUpdated: 'playback-progress-updated',
    },
    usePlaybackState: jest.fn(() => mockPlaybackState),
    useProgress: jest.fn(() => mockProgress),
  };
});

// Import the mocked module to access mock functions in assertions
import TrackPlayer from 'react-native-track-player';
const TrackPlayerMock = TrackPlayer as jest.Mocked<typeof TrackPlayerModule>;

jest.mock('../../services/previewService', () => ({
  getPreviewUrl: jest.fn(
    (videoId: string) => `https://example.com/preview/${videoId}`,
  ),
  getPreviewHeaders: jest.fn().mockResolvedValue({
    Authorization: 'Bearer test-token',
  }),
}));

describe('usePreview', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    // Reset mutable mock state
    mockPlaybackState = { state: undefined };
    mockProgress = { position: 0, duration: 0, buffered: 0 };
  });

  // ─── Initial State ──────────────────────────────────────────────

  it('initialState has isPlaying=false, isLoading=false, progress=0, elapsed=0, error=null', () => {
    mockPlaybackState = { state: 'none' };

    const { result } = renderHook(() => usePreview());

    expect(result.current.isPlaying).toBe(false);
    expect(result.current.isLoading).toBe(false);
    expect(result.current.progress).toBe(0);
    expect(result.current.elapsed).toBe(0);
    expect(result.current.error).toBeNull();
  });

  // ─── Play action ─────────────────────────────────────────────────

  it('play() calls TrackPlayer.reset, TrackPlayer.add with correct args, then TrackPlayer.play', async () => {
    const { result } = renderHook(() => usePreview());

    let promise: Promise<void>;
    act(() => {
      promise = result.current.play('abc123');
    });

    expect(result.current.isLoading).toBe(true);

    await act(async () => {
      await promise;
    });

    expect(TrackPlayerMock.reset).toHaveBeenCalled();
    expect(TrackPlayerMock.add).toHaveBeenCalledWith({
      id: 'abc123',
      url: 'https://example.com/preview/abc123',
      headers: { Authorization: 'Bearer test-token' },
      title: 'Preview',
    });
    expect(TrackPlayerMock.play).toHaveBeenCalled();
  });

  it('play() passes AbortSignal checkpoints and stops if aborted', async () => {
    TrackPlayerMock.reset.mockImplementationOnce(async () => {
      // Simulate the play being aborted mid-setup
    });

    const { result } = renderHook(() => usePreview());

    // Start playing one video
    let firstPromise: Promise<void>;
    act(() => {
      firstPromise = result.current.play('abc123');
    });

    // Calling play again before first completes should abort first
    let secondPromise: Promise<void>;
    act(() => {
      secondPromise = result.current.play('def456');
    });

    await act(async () => {
      await firstPromise;
      await secondPromise;
    });

    // Both calls should have the right params for their respective videos
    expect(TrackPlayerMock.add).toHaveBeenCalledTimes(1);
    expect(TrackPlayerMock.add).toHaveBeenCalledWith(
      expect.objectContaining({ id: 'def456' }),
    );
    expect(TrackPlayerMock.play).toHaveBeenCalled();
  });

  // ─── Error handling ──────────────────────────────────────────────

  it('does not set error state when play is aborted via AbortController', async () => {
    const abortError = new Error('The operation was aborted');
    abortError.name = 'AbortError';
    TrackPlayerMock.add.mockRejectedValueOnce(abortError);

    const { result } = renderHook(() => usePreview());

    await act(async () => {
      await result.current.play('abc123');
    });

    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it('sets error on TrackPlayer failure', async () => {
    TrackPlayerMock.add.mockRejectedValueOnce(new Error('Player not ready'));

    const { result } = renderHook(() => usePreview());

    await act(async () => {
      await result.current.play('abc123');
    });

    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBe('Player not ready');
  });

  it('sets fallback error message when error is not an Error instance', async () => {
    TrackPlayerMock.add.mockRejectedValueOnce('String error');

    const { result } = renderHook(() => usePreview());

    await act(async () => {
      await result.current.play('abc123');
    });

    expect(result.current.error).toBe('Preview failed');
  });

  // ─── Stop action ─────────────────────────────────────────────────

  it('stop() calls TrackPlayer.reset, clears error, stops loading', async () => {
    const { result } = renderHook(() => usePreview());

    await act(async () => {
      await result.current.play('abc123');
    });

    // Set an error state to verify it's cleared
    await act(async () => {
      await result.current.stop();
    });

    expect(TrackPlayerMock.reset).toHaveBeenCalled();
    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  // ─── Auto-stop at 10 seconds ─────────────────────────────────────

  it('auto-stops playback at 10 seconds by calling TrackPlayer.pause', async () => {
    // Simulate playing state
    mockPlaybackState = { state: 'playing' };

    const { result, rerender } = renderHook(() => usePreview());

    // Position under 10 — pause should not be called
    mockProgress = { position: 5, duration: 30, buffered: 5 };
    act(() => {
      rerender(undefined);
    });

    expect(TrackPlayerMock.pause).not.toHaveBeenCalled();

    // Position >= 10 — pause should be triggered
    mockProgress = { position: 12, duration: 30, buffered: 12 };
    act(() => {
      rerender(undefined);
    });

    expect(TrackPlayerMock.pause).toHaveBeenCalledTimes(1);
  });

  // ─── Elapsed / Progress derivation ───────────────────────────────

  it('caps elapsed at 10 seconds and computes progress correctly', async () => {
    mockPlaybackState = { state: 'playing' };

    const { result, rerender } = renderHook(() => usePreview());

    // Elapsed capped at max 10
    mockProgress = { position: 8, duration: 30, buffered: 8 };
    act(() => {
      rerender(undefined);
    });
    expect(result.current.elapsed).toBeCloseTo(8, 1);
    expect(result.current.progress).toBeCloseTo(0.8, 1);

    // Beyond 10 — still capped at 10
    mockProgress = { position: 15, duration: 30, buffered: 15 };
    act(() => {
      rerender(undefined);
    });
    expect(result.current.elapsed).toBeCloseTo(10, 1);
    expect(result.current.progress).toBeCloseTo(1.0, 1);
  });

  // ─── Cleanup on unmount ─────────────────────────────────────────

  it('calls TrackPlayer.reset and aborts on unmount', () => {
    const { unmount } = renderHook(() => usePreview());

    unmount();

    expect(TrackPlayerMock.reset).toHaveBeenCalled();
  });
});
