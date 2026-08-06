import { useCallback, useEffect, useRef, useState } from 'react';
import { checkHealth, wake } from '../services/serverWakeService';

export const WAKE_POLL_INTERVAL = 3000;
export const WAKE_POLL_TIMEOUT = 10000;
export const WAKE_TOTAL_TIMEOUT = 120000;

export type WakePhase = 'waking' | 'polling' | 'unavailable';

interface UseServerWakeResult {
  phase: WakePhase;
  retry: () => void;
}

const useServerWake = (onComplete: () => void): UseServerWakeResult => {
  const [phase, setPhase] = useState<WakePhase>('waking');
  const [retryCount, setRetryCount] = useState(0);
  const activeRef = useRef(true);
  const onCompleteRef = useRef(onComplete);
  onCompleteRef.current = onComplete;

  // Wake-up + health polling — re-runs when retryCount changes
  useEffect(() => {
    activeRef.current = true;
    const startedAt = Date.now();
    let pollTimer: ReturnType<typeof setInterval> | undefined;
    let pollAborted = false;

    const clearPollTimer = () => {
      if (pollTimer !== undefined) {
        clearInterval(pollTimer as unknown as number);
        pollTimer = undefined;
      }
    };

    const pollHealth = async (): Promise<void> => {
      const controller = new AbortController();
      try {
        const result = await checkHealth(controller.signal, WAKE_POLL_TIMEOUT);
        if (result.status === 'healthy' && activeRef.current) {
          clearPollTimer();
          if (activeRef.current) {
            onCompleteRef.current();
          }
        }
      } catch {
        // Poll failed — will retry on next interval
      }
    };

    const startWake = async () => {
      await wake();

      if (!activeRef.current || pollAborted) return;

      setPhase('polling');
      pollHealth();
      pollTimer = setInterval(() => {
        if (pollAborted) return;

        if (Date.now() - startedAt >= WAKE_TOTAL_TIMEOUT) {
          clearPollTimer();
          if (activeRef.current && !pollAborted) {
            setPhase('unavailable');
          }
          return;
        }

        pollHealth();
      }, WAKE_POLL_INTERVAL);
    };

    startWake();

    return () => {
      pollAborted = true;
      activeRef.current = false;
      if (pollTimer) clearInterval(pollTimer);
    };
  }, [retryCount]);

  const retry = useCallback(() => {
    setPhase('waking');
    setRetryCount(c => c + 1);
  }, []);

  return { phase, retry };
};

export default useServerWake;
