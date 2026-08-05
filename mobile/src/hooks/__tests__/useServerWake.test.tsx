import { renderHook, act } from '@testing-library/react-native';
import useServerWake, { WAKE_TOTAL_TIMEOUT } from '../useServerWake';
import * as serverWakeService from '../../services/serverWakeService';
import type { CheckHealthResult } from '../../services/serverWakeService';

jest.mock('../../services/serverWakeService', () => ({
  wake: jest.fn(),
  checkHealth: jest.fn(),
}));

const mockWake = serverWakeService.wake as jest.Mock;
const mockCheckHealth = serverWakeService.checkHealth as jest.Mock;

/** Flush all pending microtasks. Each `await act(async () => {})` processes one
 *  microtask queue item. Async functions with await create multiple microtasks. */
const flushMicrotasks = async (count = 200) => {
  for (let i = 0; i < count; i++) {
    await act(async () => {});
  }
};

describe('useServerWake', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it('initializes in the waking phase', () => {
    const { result } = renderHook(() => useServerWake(() => {}));

    expect(result.current.phase).toBe('waking');
  });

  it('calls wake() on mount then transitions to polling', async () => {
    jest.useFakeTimers();
    mockWake.mockResolvedValue({ status: 'ok' });
    mockCheckHealth.mockResolvedValue({ status: 'unhealthy' });

    const { result } = renderHook(() => useServerWake(() => {}));

    await flushMicrotasks(10);

    expect(mockWake).toHaveBeenCalledTimes(1);
    expect(mockCheckHealth).toHaveBeenCalled();
    expect(result.current.phase).toBe('polling');
  });

  it('calls onComplete when the health check reports healthy', async () => {
    jest.useFakeTimers();
    mockWake.mockResolvedValue({ status: 'ok' });
    mockCheckHealth.mockResolvedValue({ status: 'healthy' });

    const onComplete = jest.fn();
    renderHook(() => useServerWake(onComplete));

    await flushMicrotasks(20);

    expect(onComplete).toHaveBeenCalledTimes(1);
  });

  it('does not call onComplete when the health check reports unhealthy', async () => {
    jest.useFakeTimers();
    mockWake.mockResolvedValue({ status: 'ok' });
    mockCheckHealth.mockResolvedValue({ status: 'unhealthy' });

    const onComplete = jest.fn();
    const { result } = renderHook(() => useServerWake(onComplete));

    await flushMicrotasks(20);

    expect(onComplete).not.toHaveBeenCalled();
    expect(result.current.phase).toBe('polling');
  });

  it('proceeds to polling even when wake() fails', async () => {
    jest.useFakeTimers();
    mockWake.mockResolvedValue({ status: 'failed' });
    mockCheckHealth.mockResolvedValue({ status: 'unhealthy' });

    const { result } = renderHook(() => useServerWake(() => {}));

    await flushMicrotasks(10);

    expect(result.current.phase).toBe('polling');
  });

  it('transitions to unavailable after the total timeout', async () => {
    jest.useFakeTimers();
    mockWake.mockResolvedValue({ status: 'ok' });
    mockCheckHealth.mockResolvedValue({ status: 'unhealthy' });

    const onComplete = jest.fn();
    const { result } = renderHook(() => useServerWake(onComplete));

    await flushMicrotasks(10);

    await act(async () => {
      jest.advanceTimersByTime(WAKE_TOTAL_TIMEOUT + 1000);
    });
    await flushMicrotasks(200);

    expect(result.current.phase).toBe('unavailable');
    expect(onComplete).not.toHaveBeenCalled();
  });

  it('does not call onComplete when a health check resolves after unmount', async () => {
    jest.useFakeTimers();
    mockWake.mockResolvedValue({ status: 'ok' });

    let resolveHealth!: (value: CheckHealthResult) => void;
    mockCheckHealth.mockReturnValue(
      new Promise<CheckHealthResult>(resolve => {
        resolveHealth = resolve;
      }),
    );

    const onComplete = jest.fn();
    const { unmount } = renderHook(() => useServerWake(onComplete));

    await flushMicrotasks(20);

    await act(async () => {
      unmount();
    });
    await flushMicrotasks(10);

    await act(async () => {
      resolveHealth({ status: 'healthy' });
    });
    await flushMicrotasks(20);

    expect(onComplete).not.toHaveBeenCalled();
  });

  it('can be unmounted during polling without errors', async () => {
    jest.useFakeTimers();
    mockWake.mockResolvedValue({ status: 'ok' });
    mockCheckHealth.mockResolvedValue({ status: 'unhealthy' });

    const { unmount } = renderHook(() => useServerWake(() => {}));

    await flushMicrotasks(10);

    await act(async () => {
      jest.advanceTimersByTime(50);
    });
    await flushMicrotasks(20);

    await act(async () => {
      unmount();
    });
    await flushMicrotasks(10);
  });

  it('retry resets to waking and restarts the wake flow', async () => {
    jest.useFakeTimers();
    mockWake.mockResolvedValue({ status: 'ok' });
    mockCheckHealth.mockResolvedValue({ status: 'unhealthy' });

    const { result } = renderHook(() => useServerWake(() => {}));

    await flushMicrotasks(10);
    expect(result.current.phase).toBe('polling');
    expect(mockWake).toHaveBeenCalledTimes(1);

    act(() => {
      result.current.retry();
    });
    await flushMicrotasks(10);

    expect(result.current.phase).toBe('polling');
    expect(mockWake).toHaveBeenCalledTimes(2);
  });
});
