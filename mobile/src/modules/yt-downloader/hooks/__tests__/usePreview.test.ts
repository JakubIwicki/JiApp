import { renderHook, act } from '@testing-library/react-native';
import usePreview from '../usePreview';
import type TrackPlayerModule from 'react-native-track-player';

// Shared mutable state for mock TrackPlayer hooks
let mockPlaybackState: { state: string | undefined } = { state: undefined };
let mockProgress = { position: 0, duration: 0, buffered: 0 };
let trackPlayerEventHandler: ((event: Record<string, unknown>) => void) | null = null;

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
      PlaybackError: 'playback-error',
    },
    usePlaybackState: jest.fn(() => mockPlaybackState),
    useProgress: jest.fn(() => mockProgress),
    useTrackPlayerEvents: jest.fn((_events, handler) => {
      trackPlayerEventHandler = handler;
    }),
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
    trackPlayerEventHandler = null;
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
      contentType: 'audio/mpeg',
    });
    expect(TrackPlayerMock.play).toHaveBeenCalled();
  });

  it('play() passes AbortSignal checkpoints and stops if aborted', async () => {
    // Make TrackPlayer.reset a delayed promise so the first call parks there
    let resolveReset: () => void;
    TrackPlayerMock.reset.mockImplementationOnce(() => {
      return new Promise<void>((resolve) => {
        resolveReset = resolve;
      });
    });

    const { result } = renderHook(() => usePreview());

    // Start first play — don't await, so it parks at the delayed reset
    let firstPromise: Promise<void>;
    act(() => {
      firstPromise = result.current.play('abc123');
    });

    // Start second play — this aborts the first call's controller
    let secondPromise: Promise<void>;
    act(() => {
      secondPromise = result.current.play('def456');
    });

    // Await the second play (uses the default mock, resolves immediately)
    await act(async () => {
      await secondPromise;
    });

    // The second play should have called TrackPlayer.add with 'def456'
    expect(TrackPlayerMock.add).toHaveBeenCalledTimes(1);
    expect(TrackPlayerMock.add).toHaveBeenCalledWith(
      expect.objectContaining({ id: 'def456' }),
    );
    expect(TrackPlayerMock.play).toHaveBeenCalled();

    // Now resolve the first call's reset
    act(() => {
      resolveReset();
    });

    // Await first promise so it runs its checkout checkpoints
    await act(async () => {
      await firstPromise;
    });

    // TrackPlayer.add should still have been called only once (by the second play)
    // The superseded first call bailed at the abort checkpoint before calling add
    expect(TrackPlayerMock.add).toHaveBeenCalledTimes(1);
    expect(TrackPlayerMock.add).not.toHaveBeenCalledWith(
      expect.objectContaining({ id: 'abc123' }),
    );
  });

  it('keeps isLoading=true until playback state confirms Playing', async () => {
    mockPlaybackState = { state: 'none' };

    const { result, rerender } = renderHook(() => usePreview());

    await act(async () => {
      await result.current.play('abc123');
    });

    // After play() resolves, isLoading should still be true because
    // playback state is still None (not yet Playing)
    expect(result.current.isLoading).toBe(true);

    // Simulate TrackPlayer confirming playback via useTrackPlayerEvents callback
    mockPlaybackState = { state: 'playing' };
    act(() => {
      trackPlayerEventHandler!({ state: 'playing' });
      rerender(undefined);
    });

    expect(result.current.isLoading).toBe(false);
    expect(result.current.isPlaying).toBe(true);
  });

  // ─── Replay ────────────────────────────────────────────────────────

  it('play() works when called twice with the same videoId', async () => {
    const { result } = renderHook(() => usePreview());

    // First play
    await act(async () => {
      await result.current.play('abc123');
    });

    // Stop
    await act(async () => {
      await result.current.stop();
    });

    // Second play with same videoId
    await act(async () => {
      await result.current.play('abc123');
    });

    expect(TrackPlayerMock.add).toHaveBeenCalledTimes(2);
    expect(TrackPlayerMock.play).toHaveBeenCalledTimes(2);
    expect(result.current.error).toBeNull();
  });

  it('play() works with different videoIds sequentially', async () => {
    const { result } = renderHook(() => usePreview());

    await act(async () => {
      await result.current.play('abc123');
    });

    await act(async () => {
      await result.current.stop();
    });

    await act(async () => {
      await result.current.play('def456');
    });

    expect(TrackPlayerMock.add).toHaveBeenCalledTimes(2);
    expect(TrackPlayerMock.add).toHaveBeenNthCalledWith(
      1,
      expect.objectContaining({ id: 'abc123' }),
    );
    expect(TrackPlayerMock.add).toHaveBeenNthCalledWith(
      2,
      expect.objectContaining({ id: 'def456' }),
    );
    expect(result.current.error).toBeNull();
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

  // ─── Error surface from playbackState ───────────────────────────

  it('surfaces error from playbackState when TrackPlayer state is Error', () => {
    const { result } = renderHook(() => usePreview());

    act(() => {
      trackPlayerEventHandler!({
        state: 'error',
        error: { message: 'Network request failed' },
      });
    });

    expect(result.current.error).toBe('Network request failed');
  });

  it('does not set error for non-Error playback states', () => {
    mockPlaybackState = { state: 'paused' };

    const { result } = renderHook(() => usePreview());

    expect(result.current.error).toBeNull();
  });

  // ─── Buffering state treated as active ──────────────────────────

  it('treats buffering state as isPlaying so UI does not show idle play button', () => {
    mockPlaybackState = { state: 'buffering' };

    const { result } = renderHook(() => usePreview());

    expect(result.current.isPlaying).toBe(true);
  });

  it('does not set error when a superseded play throws a non-AbortError', async () => {
    let rejectAdd: (err: Error) => void;
    TrackPlayerMock.add.mockImplementationOnce(() => {
      return new Promise<void>((_resolve, reject) => {
        rejectAdd = reject;
      });
    });

    const { result, rerender } = renderHook(() => usePreview());

    // First play — parks at TrackPlayer.add()
    let firstPromise: Promise<void>;
    act(() => {
      firstPromise = result.current.play('abc123');
    });

    // Yield microtask queue so the first play can advance past
    // await TrackPlayer.reset() and await getPreviewHeaders() to reach add()
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });

    // Second play — aborts the first controller, completes normally
    let secondPromise: Promise<void>;
    act(() => {
      secondPromise = result.current.play('def456');
    });

    await act(async () => {
      await secondPromise;
    });

    // Reject the first call's deferred add with a non-AbortError
    // and flush the first play in the same act to ensure microtask ordering
    await act(async () => {
      rejectAdd!(new Error('TrackPlayer internal error'));
      // Yield so the rejection microtask (play's catch block) runs before we check
      await Promise.resolve();
      // The first play's catch block has now executed — firstPromise should be resolved
      await firstPromise;
    });

    // Error is suppressed because abortRef.current now points to
    // the second controller, not the first one that threw
    expect(result.current.error).toBeNull();

    // isLoading stays true until playback state confirms Playing
    expect(result.current.isLoading).toBe(true);

    // Simulate playback state confirming Playing via useTrackPlayerEvents
    mockPlaybackState = { state: 'playing' };
    act(() => {
      trackPlayerEventHandler!({ state: 'playing' });
    });

    expect(result.current.isLoading).toBe(false);
  });
});
