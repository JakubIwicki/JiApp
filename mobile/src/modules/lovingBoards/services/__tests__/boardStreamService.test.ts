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
    onEvent: jest.fn(),
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

  it('does not reconnect when consumer closes during the 401 refresh round-trip', async () => {
    const ES = jest.requireMock('react-native-sse').default;
    const onError = jest.fn();
    let resolveRefresh: (token: string) => void = () => {};
    mockRefreshAuth.mockImplementationOnce(
      () =>
        new Promise<string>(resolve => {
          resolveRefresh = resolve;
        }),
    );

    const handle = openBoardStream(createParams({ onError }));
    await flush();

    // Trigger a 401; refreshAuth is now pending
    const errorListeners = capturedListeners?.get('error') ?? [];
    for (const l of errorListeners) {
      l({
        type: 'error',
        message: 'Unauthorized',
        xhrStatus: 401,
        xhrState: 4,
      });
    }

    // Close while the refresh promise is still in flight
    handle.close();

    resolveRefresh('fresh-token');
    await flush();

    // Consumer asked to close, so no reconnect EventSource is created
    expect(ES).toHaveBeenCalledTimes(1);
    expect(onError).not.toHaveBeenCalled();
  });

  it('delivers each change event to onEvent with its validated payload', async () => {
    const onEvent = jest.fn();
    openBoardStream(createParams({ onEvent }));
    await flush();

    emit('item.added', { itemId: 1 });
    emit('item.updated', { itemId: 2 });
    emit('item.status', { itemId: 3, status: 'Completed' });
    emit('item.removed', { itemId: 4 });
    emit('items.cleared', { itemIds: [5, 6] });
    emit('board.updated', { boardId: 7 });
    emit('member.changed', { boardId: 8 });
    emit('recurring.reset', { reset: 9 });
    emit('board.deleted', { boardId: 10 });

    expect(onEvent).toHaveBeenCalledTimes(9);
    expect(onEvent).toHaveBeenCalledWith({ type: 'item.added', itemId: 1 });
    expect(onEvent).toHaveBeenCalledWith({ type: 'item.updated', itemId: 2 });
    expect(onEvent).toHaveBeenCalledWith({
      type: 'item.status',
      itemId: 3,
      status: 'Completed',
    });
    expect(onEvent).toHaveBeenCalledWith({ type: 'item.removed', itemId: 4 });
    expect(onEvent).toHaveBeenCalledWith({
      type: 'items.cleared',
      itemIds: [5, 6],
    });
    expect(onEvent).toHaveBeenCalledWith({ type: 'board.updated', boardId: 7 });
    expect(onEvent).toHaveBeenCalledWith({
      type: 'member.changed',
      boardId: 8,
    });
    expect(onEvent).toHaveBeenCalledWith({ type: 'recurring.reset', reset: 9 });
    expect(onEvent).toHaveBeenCalledWith({
      type: 'board.deleted',
      boardId: 10,
    });
  });

  it.each([
    ['item.added', { itemId: 'not-a-number' }],
    ['item.updated', {}],
    ['item.status', { itemId: 3 }],
    ['item.status', { itemId: 3, status: 'Unknown' }],
    ['item.removed', { itemId: null }],
    ['items.cleared', { itemIds: 'not-an-array' }],
    ['board.updated', { boardId: 'not-a-number' }],
    ['member.changed', {}],
    ['recurring.reset', { reset: 'not-a-number' }],
    ['board.deleted', { wrong: true }],
  ])(
    'drops a malformed %s payload without throwing or tearing down the stream',
    async (name, payload) => {
      const onEvent = jest.fn();
      const onError = jest.fn();
      openBoardStream(createParams({ onEvent, onError }));
      await flush();

      expect(() => emit(name, payload)).not.toThrow();
      expect(onError).not.toHaveBeenCalled();

      // A valid event right after still reaches the consumer — stream intact
      emit('item.added', { itemId: 42 });
      expect(onEvent).toHaveBeenCalledTimes(1);
      expect(onEvent).toHaveBeenCalledWith({ type: 'item.added', itemId: 42 });
    },
  );

  it('drops an invalid-JSON change payload without throwing', async () => {
    const onEvent = jest.fn();
    const onError = jest.fn();
    openBoardStream(createParams({ onEvent, onError }));
    await flush();

    const listeners = capturedListeners?.get('item.added') ?? [];
    expect(() => {
      for (const l of listeners) {
        l({ type: 'item.added', data: '{not json' });
      }
    }).not.toThrow();
    expect(onEvent).not.toHaveBeenCalled();
    expect(onError).not.toHaveBeenCalled();
  });
});
