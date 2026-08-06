import { z } from 'zod';
import { API_BASE_URL, WAKE_API_URL } from '../config';

const HealthResponseSchema = z.object({
  status: z.string(),
});

export type WakeResult = { status: 'ok' } | { status: 'failed' };
export type CheckHealthResult = { status: 'healthy' } | { status: 'unhealthy' };

const HEALTH_URL = `${API_BASE_URL.replace(/\/api\/v1\/?$/, '')}/health`;

export const wake = async (): Promise<WakeResult> => {
  try {
    await fetch(`${WAKE_API_URL}/start`, { method: 'POST' });
    return { status: 'ok' };
  } catch {
    // Wake API may be cold-starting (Lambda) — the caller polls health regardless.
    return { status: 'failed' };
  }
};

export const checkHealth = async (
  signal: AbortSignal,
  timeoutMs: number,
): Promise<CheckHealthResult> => {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeoutMs);
  const onExternalAbort = () => controller.abort();
  signal.addEventListener('abort', onExternalAbort);

  try {
    const response = await fetch(HEALTH_URL, {
      method: 'GET',
      signal: controller.signal,
    });
    clearTimeout(timeoutId);
    signal.removeEventListener('abort', onExternalAbort);

    if (!response.ok) {
      return { status: 'unhealthy' };
    }

    const body = await response.json().catch(() => null);
    return HealthResponseSchema.safeParse(body).success
      ? { status: 'healthy' }
      : { status: 'unhealthy' };
  } catch {
    clearTimeout(timeoutId);
    signal.removeEventListener('abort', onExternalAbort);
    return { status: 'unhealthy' };
  }
};
