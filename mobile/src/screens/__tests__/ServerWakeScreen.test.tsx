import React from 'react';
import { render, fireEvent, act } from '@testing-library/react-native';

// Mock the hook — the screen is presentational and composes useServerWake.
const mockRetry = jest.fn();
let mockPhase: 'waking' | 'polling' | 'unavailable' = 'waking';

jest.mock('../../hooks/useServerWake', () => ({
  __esModule: true,
  default: (_onComplete: () => void) => ({
    phase: mockPhase,
    retry: mockRetry,
  }),
}));

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

// Mock useKeepAwake
jest.mock('../../hooks/useKeepAwake', () => ({
  __esModule: true,
  default: jest.fn(),
}));

import ServerWakeScreen from '../ServerWakeScreen';

describe('ServerWakeScreen', () => {
  const mockOnComplete = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    mockPhase = 'waking';
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it('renders waking phase with spinner and title', () => {
    jest.useFakeTimers();
    const { getByTestId, getByText } = render(
      <ServerWakeScreen onComplete={mockOnComplete} />,
    );

    expect(getByTestId('server-wake-screen')).toBeTruthy();
    expect(getByTestId('wake-spinner')).toBeTruthy();
    expect(getByText('wake.title')).toBeTruthy();
    expect(getByText('wake.message')).toBeTruthy();
  });

  it('renders spinner during polling phase', () => {
    jest.useFakeTimers();
    mockPhase = 'polling';

    const { getByTestId } = render(
      <ServerWakeScreen onComplete={mockOnComplete} />,
    );

    expect(getByTestId('wake-spinner')).toBeTruthy();
  });

  it('renders unavailable phase with retry and close buttons', () => {
    jest.useFakeTimers();
    mockPhase = 'unavailable';

    const { getByTestId, queryByTestId, getByText } = render(
      <ServerWakeScreen onComplete={mockOnComplete} />,
    );

    expect(queryByTestId('wake-spinner')).toBeNull();
    expect(getByText('wake.unavailable')).toBeTruthy();
    expect(getByText('wake.unavailableMessage')).toBeTruthy();
    expect(getByTestId('wake-retry-button')).toBeTruthy();
    expect(getByTestId('wake-close-button')).toBeTruthy();
  });

  it('calls retry when the retry button is pressed', () => {
    jest.useFakeTimers();
    mockPhase = 'unavailable';

    const { getByTestId } = render(
      <ServerWakeScreen onComplete={mockOnComplete} />,
    );

    fireEvent.press(getByTestId('wake-retry-button'));

    expect(mockRetry).toHaveBeenCalledTimes(1);
  });

  it('can be unmounted without errors', async () => {
    jest.useFakeTimers();

    const { unmount } = render(
      <ServerWakeScreen onComplete={mockOnComplete} />,
    );

    await act(async () => {
      unmount();
    });
  });

  it('can be unmounted in the unavailable phase without errors', async () => {
    jest.useFakeTimers();
    mockPhase = 'unavailable';

    const { unmount } = render(
      <ServerWakeScreen onComplete={mockOnComplete} />,
    );

    await act(async () => {
      unmount();
    });
  });
});
