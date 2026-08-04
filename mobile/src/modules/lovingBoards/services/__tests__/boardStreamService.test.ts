import axios from 'axios';
import { getToken } from '../../../../services/storageService';
import { refreshAuth } from '../../../../services/apiClient';
import { openBoardStream } from '../boardStreamService';
import type { BoardStreamParams } from '../boardStreamService';

// ── Mocks ──────────────────────────────────────────────────────────────────

var capturedListeners: Map<string, Array<(e: any) => void>>;

jest.mock('react-native-sse', () => {
  const map = new Map<string, Array<(e: any) => void>>();
  capturedListeners = map;

  return {
    __esModule: true,
    default: jest.fn(() => ({
      addEventListener: jest.fn((type: string, listener: (e: any) => void) => {
        if (!map.has(type)) map.set(type, []);
        map.get(type)!.push(listener);
      }),
      removeEventListener: jest.fn(),
      removeAllEventListeners: jest.fn(() => map.clear()),
      close: jest.fn(() => map.clear()),
    })),
  };
});

jest.mock('../../../../services/storageService', () => ({
  getToken: jest.fn(() => Promise.resolve('test-token')),
}));

jest.mock('../../../../services/apiClient', () => ({
  refreshAuth: jest.fn(),
}));

jest.mock('axios', () => ({
  __esModule: true,
  default: {
    post: jest.fn(),
  },
}));

jest.mock('../../../../config', () => ({
  API_BASE_URL: 'http://test.local/api/v1',
}));

// ── Helpers ────────────────────────────────────────────────────────────────

const mockGetToken = getToken as jest.Mock;
const mockRefreshAuth = refreshAuth as jest.Mock;
const mockAxiosPost = axios.post as jest.Mock;

function emit(type: string, data: unknown): void {
  const listeners = capturedListeners?.get(type) ?? [];
  const dataStr = typeof data === 'string' ? data : JSON.stringify(data);
  for (const l of listeners) {
    l({ type, data: dataStr });
  }
}

/** Flush pending micro/macrotasks so the async re-auth+reconnect chain completes */
const flush = () => new Promise<void>(resolve => setImmediate(resolve));

function createParams(
  overrides: Partial<BoardStreamParams> = {},
): BoardStreamParams {
  return {
    boardId: 5,
    onChange: jest.fn(),
    onPresence: jest.fn(),
    onOpen: jest.fn(),
    onError: jest.fn(),
    ...overrides,
  };
}

beforeEach(() => {
  jest.clearAllMocks();
  capturedListeners?.clear();
  mockGetToken.mockResolvedValue('test-token');
});

// ── Tests ──────────────────────────────────────────────────────────────────

describe('openBoardStream', () => {
  it('creates an EventSource with GET and auth header', async () => {
    const ES = jest.requireMock('react-native-sse').default;

    openBoardStream(createParams());
    await flush();

    expect(ES).toHaveBeenCalledTimes(1);
    const [url, opts] = ES.mock.calls[0];
    expect(url).toBe('http://test.local/api/v1/lovingboards/boards/5/stream');
    expect(opts.method).toBe('GET');
    expect(opts.headers.Authorization).toBe('Bearer test-token');
  });

  it('calls onPresence when a valid presence event arrives', async () => {
    const onPresence = jest.fn();
    openBoardStream(createParams({ onPresence }));
    await flush();

    emit('presence', { userIds: [1, 2, 3] });

    expect(onPresence).toHaveBeenCalledWith([1, 2, 3]);
  });

  it('calls onError when the connection fails with a non-401 error', async () => {
    const onError = jest.fn();
    openBoardStream(createParams({ onError }));
    await flush();

    const errorListeners = capturedListeners?.get('error') ?? [];
    for (const l of errorListeners) {
      l({
        type: 'error',
        message: 'Server error',
        xhrStatus: 500,
        xhrState: 4,
      });
    }

    await flush();
    expect(onError).toHaveBeenCalledWith(
      expect.objectContaining({ message: 'Board stream connection failed' }),
    );
  });

  it('re-auths via shared refreshAuth and reconnects on 401, then proceeds', async () => {
    mockGetToken.mockResolvedValueOnce('expired-token');
    // After re-auth, getToken is called again for the reconnection
    mockGetToken.mockResolvedValueOnce('fresh-token');
    mockRefreshAuth.mockResolvedValueOnce('fresh-token');

    const onPresence = jest.fn();
    openBoardStream(createParams({ onPresence }));
    await flush();

    // Trigger 401 error on the first connection
    const errorListeners = capturedListeners?.get('error') ?? [];
    for (const l of errorListeners) {
      l({
        type: 'error',
        message: 'Unauthorized',
        xhrStatus: 401,
        xhrState: 4,
      });
    }
    await flush();

    // Exactly one shared single-flight refresh call; no raw /auth/refresh
    expect(mockRefreshAuth).toHaveBeenCalledTimes(1);
    expect(mockAxiosPost).not.toHaveBeenCalled();

    // Now the reconnected stream should handle events
    emit('presence', { userIds: [9] });
    expect(onPresence).toHaveBeenCalledWith([9]);
  });
});
