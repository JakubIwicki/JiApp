import { wake, checkHealth } from '../serverWakeService';

const mockFetch = jest.fn();
global.fetch = mockFetch;

describe('serverWakeService', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  describe('wake', () => {
    it('POSTs to the wake endpoint and reports ok', async () => {
      mockFetch.mockResolvedValue({ ok: true });

      const result = await wake();

      expect(result.status).toBe('ok');
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/start'),
        expect.objectContaining({ method: 'POST' }),
      );
    });

    it('reports failed when the request throws', async () => {
      mockFetch.mockRejectedValue(new Error('Network error'));

      const result = await wake();

      expect(result.status).toBe('failed');
    });
  });

  describe('checkHealth', () => {
    const signal = () => new AbortController().signal;

    it('reports healthy on an ok response with a valid body', async () => {
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({ status: 'healthy', database: 'connected' }),
      });

      const result = await checkHealth(signal(), 10000);

      expect(result.status).toBe('healthy');
    });

    it('reports unhealthy on a non-ok response', async () => {
      mockFetch.mockResolvedValue({
        ok: false,
        status: 503,
        json: async () => ({}),
      });

      const result = await checkHealth(signal(), 10000);

      expect(result.status).toBe('unhealthy');
    });

    it('reports unhealthy when the body does not match the schema', async () => {
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({ unexpected: 'shape' }),
      });

      const result = await checkHealth(signal(), 10000);

      expect(result.status).toBe('unhealthy');
    });

    it('reports unhealthy when the request throws', async () => {
      mockFetch.mockRejectedValue(new Error('Network error'));

      const result = await checkHealth(signal(), 10000);

      expect(result.status).toBe('unhealthy');
    });

    it('aborts the request after the timeout', async () => {
      jest.useFakeTimers();
      mockFetch.mockImplementation(
        (_url: string, init: { signal: AbortSignal }) =>
          new Promise((_resolve, reject) => {
            init.signal.addEventListener('abort', () => {
              const err = new Error('Aborted');
              err.name = 'AbortError';
              reject(err);
            });
          }),
      );

      const promise = checkHealth(signal(), 10000);
      jest.advanceTimersByTime(10001);

      const result = await promise;

      expect(result.status).toBe('unhealthy');
    });

    it('aborts the request when the caller signal aborts', async () => {
      mockFetch.mockImplementation(
        (_url: string, init: { signal: AbortSignal }) =>
          new Promise((_resolve, reject) => {
            init.signal.addEventListener('abort', () => {
              const err = new Error('Aborted');
              err.name = 'AbortError';
              reject(err);
            });
          }),
      );

      const controller = new AbortController();
      const promise = checkHealth(controller.signal, 10000);
      controller.abort();

      const result = await promise;

      expect(result.status).toBe('unhealthy');
    });
  });
});
