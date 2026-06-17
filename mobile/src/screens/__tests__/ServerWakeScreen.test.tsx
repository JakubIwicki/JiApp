import React from 'react';
import { render, fireEvent, act } from '@testing-library/react-native';

// Mock react-i18next
jest.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

// Mock useSafeAreaInsets to avoid SafeAreaProvider dependency
jest.mock('react-native-safe-area-context', () => ({
  SafeAreaProvider: ({ children }: { children: React.ReactNode }) => children,
  SafeAreaView: ({ children }: { children: React.ReactNode }) => children,
  useSafeAreaInsets: () => ({ top: 0, bottom: 0, left: 0, right: 0 }),
}));

// Mock fetch globally
const mockFetch = jest.fn();
global.fetch = mockFetch;

import ServerWakeScreen from '../ServerWakeScreen';

/** Flush all pending microtasks. Each `await act(async () => {})` processes one
 *  microtask queue item. Async functions with await create multiple microtasks. */
const flushMicrotasks = async (count = 200) => {
  for (let i = 0; i < count; i++) {
    await act(async () => {});
  }
};

describe('ServerWakeScreen', () => {
  const mockOnComplete = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    mockFetch.mockReset();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it('renders waking phase with spinner and title', () => {
    jest.useFakeTimers();
    mockFetch.mockImplementation(() => new Promise(() => {}));

    const { getByTestId, getByText } = render(
      <ServerWakeScreen onComplete={mockOnComplete} />,
    );

    expect(getByTestId('server-wake-screen')).toBeTruthy();
    expect(getByTestId('wake-spinner')).toBeTruthy();
    expect(getByText('wake.title')).toBeTruthy();
    expect(getByText('wake.message')).toBeTruthy();
  });

  it('calls wake API on mount', async () => {
    jest.useFakeTimers();
    mockFetch.mockImplementation(() => new Promise(() => {}));

    render(<ServerWakeScreen onComplete={mockOnComplete} />);

    // Flush initial effect microtasks to let startWake call fetch
    await flushMicrotasks(10);

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/start'),
      expect.objectContaining({ method: 'POST' }),
    );
  });

  it('calls onComplete when health check succeeds', async () => {
    jest.useFakeTimers();
    mockFetch
      .mockResolvedValueOnce({ ok: true })
      .mockResolvedValueOnce({ ok: true });

    render(<ServerWakeScreen onComplete={mockOnComplete} />);

    // Flush microtasks: startWake -> await fetch(wake) -> resolves
    // -> pollHealth -> await fetch(health) -> resolves -> onComplete()
    await flushMicrotasks(20);

    expect(mockOnComplete).toHaveBeenCalled();
  });

  it('transitions to unavailable after total timeout and shows buttons', async () => {
    jest.useFakeTimers();
    mockFetch.mockRejectedValue(new Error('Network error'));

    const { queryByTestId } = render(
      <ServerWakeScreen onComplete={mockOnComplete} />,
    );

    // Flush initial microtasks: startWake -> await fetch -> reject -> catch -> setPhase
    // -> pollHealth -> await fetch -> reject -> catch -> setInterval
    await flushMicrotasks(10);

    // Advance time past WAKE_TOTAL_TIMEOUT (120s) to trigger the interval's
    // timeout check that sets phase to 'unavailable'
    await act(async () => {
      jest.advanceTimersByTime(121000);
    });

    // Flush microtasks from each tick's rejected fetch (40 interval ticks x ~3 microtasks)
    await flushMicrotasks(200);

    expect(mockOnComplete).not.toHaveBeenCalled();
    expect(queryByTestId('wake-spinner')).toBeNull();
    expect(queryByTestId('wake-close-button')).toBeTruthy();
    expect(queryByTestId('wake-retry-button')).toBeTruthy();
  });

  it('can be unmounted without errors', async () => {
    jest.useFakeTimers();
    mockFetch.mockImplementation(() => new Promise(() => {}));

    const { unmount } = render(
      <ServerWakeScreen onComplete={mockOnComplete} />,
    );

    await act(async () => {
      unmount();
    });
    await flushMicrotasks(10);
  });

  it('can be unmounted during polling without errors', async () => {
    jest.useFakeTimers();
    mockFetch
      .mockResolvedValueOnce({ ok: true })
      .mockRejectedValue(new Error('Network error'));

    const { unmount } = render(
      <ServerWakeScreen onComplete={mockOnComplete} />,
    );

    await act(async () => {
      jest.advanceTimersByTime(50);
    });
    await flushMicrotasks(20);

    await act(async () => {
      unmount();
    });
    await flushMicrotasks(10);
  });
});
