import React from 'react';
import { render, fireEvent } from '@testing-library/react-native';
import AudioPreviewPlayer from '../AudioPreviewPlayer';

// Mutable mock state for usePreview
let mockPreview: {
  isPlaying: boolean;
  isLoading: boolean;
  progress: number;
  elapsed: number;
  error: string | null;
  play: jest.Mock<Promise<void>, [string]>;
  stop: jest.Mock<Promise<void>, []>;
};

jest.mock('../../hooks/usePreview', () => ({
  __esModule: true,
  default: () => mockPreview,
}));

jest.mock('../../hooks/useKeepAwake', () => ({
  __esModule: true,
  default: jest.fn(),
}));

jest.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => {
      const translations: Record<string, string> = {
        'preview.tapToListen': 'Tap to preview',
        'preview.loading': 'Loading preview...',
        'preview.playing': 'Now playing',
        'preview.buffering': 'Buffering...',
        'preview.duration': '0:10',
      };
      return translations[key] || key;
    },
  }),
}));

describe('AudioPreviewPlayer', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockPreview = {
      isPlaying: false,
      isLoading: false,
      progress: 0,
      elapsed: 0,
      error: null,
      play: jest.fn().mockResolvedValue(undefined),
      stop: jest.fn().mockResolvedValue(undefined),
    };
  });

  // ─── IDLE state ─────────────────────────────────────────────

  it('renders idle state with play button, hint text, and duration', () => {
    const { getByTestId, getByText } = render(
      <AudioPreviewPlayer videoId="abc123" />,
    );

    expect(getByTestId('preview-play-button')).toBeTruthy();
    expect(getByText('Tap to preview')).toBeTruthy();
    expect(getByText('0:10')).toBeTruthy();
  });

  // ─── LOADING state ──────────────────────────────────────────

  it('renders loading state with loading text', () => {
    mockPreview = {
      ...mockPreview,
      isLoading: true,
    };

    const { getByText } = render(<AudioPreviewPlayer videoId="abc123" />);

    expect(getByText('Loading preview...')).toBeTruthy();
  });

  // ─── BUFFERING state ────────────────────────────────────────

  it('renders buffering state when playing but elapsed is still 0', () => {
    mockPreview = {
      ...mockPreview,
      isPlaying: true,
      elapsed: 0,
      progress: 0,
    };

    const { getByText, queryByText } = render(
      <AudioPreviewPlayer videoId="abc123" />,
    );

    expect(getByText('Buffering...')).toBeTruthy();
    expect(queryByText('Now playing')).toBeNull();
    expect(getByText('0:10')).toBeTruthy();
  });

  it('calls stop() when pause button is pressed during buffering', () => {
    mockPreview = {
      ...mockPreview,
      isPlaying: true,
      elapsed: 0,
      progress: 0,
    };

    const { getByTestId } = render(<AudioPreviewPlayer videoId="abc123" />);

    fireEvent.press(getByTestId('preview-play-button'));

    expect(mockPreview.stop).toHaveBeenCalled();
  });

  // ─── PLAYING state ──────────────────────────────────────────

  it('renders playing state with pause button, playing text, progress bar, and counter', () => {
    mockPreview = {
      ...mockPreview,
      isPlaying: true,
      elapsed: 5,
      progress: 0.5,
    };

    const { getByTestId, getByText } = render(
      <AudioPreviewPlayer videoId="abc123" />,
    );

    expect(getByText('Now playing')).toBeTruthy();
    expect(getByTestId('preview-progress-bar')).toBeTruthy();
    expect(getByTestId('preview-counter')).toBeTruthy();
    expect(getByText(/0:05s \/ 0:10/)).toBeTruthy();
  });

  // ─── COMPLETE state ─────────────────────────────────────────

  it('renders complete state with full progress bar and play button for replay', () => {
    mockPreview = {
      ...mockPreview,
      isPlaying: false,
      elapsed: 10,
      progress: 1,
    };

    const { getByTestId, getByText } = render(
      <AudioPreviewPlayer videoId="abc123" />,
    );

    // Should show play button for replay
    expect(getByTestId('preview-play-button')).toBeTruthy();
    // Progress bar should exist
    expect(getByTestId('preview-progress-bar')).toBeTruthy();
    // Duration hint should be back
    expect(getByText('0:10')).toBeTruthy();
  });

  // ─── ERROR state ────────────────────────────────────────────

  it('renders error state with error message in red', () => {
    mockPreview = {
      ...mockPreview,
      error: 'Player not ready',
    };

    const { getByTestId, getByText } = render(
      <AudioPreviewPlayer videoId="abc123" />,
    );

    expect(getByTestId('preview-error')).toBeTruthy();
    expect(getByText('Player not ready')).toBeTruthy();
  });

  // ─── Interactions ───────────────────────────────────────────

  it('calls play() with videoId when play button is pressed in idle state', () => {
    const { getByTestId } = render(<AudioPreviewPlayer videoId="abc123" />);

    fireEvent.press(getByTestId('preview-play-button'));

    expect(mockPreview.play).toHaveBeenCalledWith('abc123');
  });

  it('calls stop() when pause button is pressed during playback', () => {
    mockPreview = {
      ...mockPreview,
      isPlaying: true,
      elapsed: 5,
      progress: 0.5,
    };

    const { getByTestId } = render(<AudioPreviewPlayer videoId="abc123" />);

    fireEvent.press(getByTestId('preview-play-button'));

    expect(mockPreview.stop).toHaveBeenCalled();
  });

  it('calls play() with videoId when pressing play in complete state (replay)', () => {
    mockPreview = {
      ...mockPreview,
      isPlaying: false,
      elapsed: 10,
      progress: 1,
    };

    const { getByTestId } = render(<AudioPreviewPlayer videoId="abc123" />);

    fireEvent.press(getByTestId('preview-play-button'));

    expect(mockPreview.play).toHaveBeenCalledWith('abc123');
  });

  // ─── Cleanup ────────────────────────────────────────────────

  it('calls stop() on unmount', () => {
    const { unmount } = render(<AudioPreviewPlayer videoId="abc123" />);

    unmount();

    expect(mockPreview.stop).toHaveBeenCalled();
  });

  it('calls stop() when videoId changes', () => {
    const { rerender, getByTestId } = render(
      <AudioPreviewPlayer videoId="abc123" />,
    );

    rerender(<AudioPreviewPlayer videoId="def456" />);

    // stop should be called for the old video
    expect(mockPreview.stop).toHaveBeenCalled();

    // After changing videoId and stop is called, pressing play should work for new id
    fireEvent.press(getByTestId('preview-play-button'));
    expect(mockPreview.play).toHaveBeenCalledWith('def456');
  });
});
