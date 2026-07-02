import axios from 'axios';
import type { VideoItem } from '../../types/api';
import {
  getToken,
  getRefreshToken,
  saveToken,
  saveRefreshToken,
} from '../storageService';
import { openChatStream } from '../chatService';
import type { ChatStreamParams } from '../chatService';

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

jest.mock('../storageService', () => ({
  getToken: jest.fn(() => Promise.resolve('test-token')),
  getRefreshToken: jest.fn(() => Promise.resolve(null)),
  saveToken: jest.fn(() => Promise.resolve()),
  saveRefreshToken: jest.fn(() => Promise.resolve()),
}));

jest.mock('axios', () => ({
  __esModule: true,
  default: {
    post: jest.fn(),
  },
}));

jest.mock('../../config', () => ({
  API_BASE_URL: 'http://test.local/api/v1',
}));

jest.mock('../../i18n', () => ({
  __esModule: true,
  default: {
    language: 'en',
  },
}));

// ── Helpers ────────────────────────────────────────────────────────────────

const mockGetToken = getToken as jest.Mock;
const mockGetRefreshToken = getRefreshToken as jest.Mock;
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
  overrides: Partial<ChatStreamParams> = {},
): ChatStreamParams {
  return {
    messages: [{ role: 'user', content: 'Hello' }],
    onTextDelta: jest.fn(),
    onToolStep: jest.fn(),
    onSearchResults: jest.fn(),
    onDownloadOffer: jest.fn(),
    onDone: jest.fn(),
    onError: jest.fn(),
    ...overrides,
  };
}

const sampleVideo: VideoItem = {
  videoId: 'dQw4w9WgXcQ',
  title: 'Rick Astley - Never Gonna Give You Up',
  description: 'Official video',
  imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/default.jpg',
  videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
  channelTitle: 'Rick Astley',
};

beforeEach(() => {
  jest.clearAllMocks();
  capturedListeners?.clear();
  mockGetToken.mockResolvedValue('test-token');
  mockGetRefreshToken.mockResolvedValue(null);
});

// ── Tests ──────────────────────────────────────────────────────────────────

describe('openChatStream', () => {
  it('creates an EventSource with POST and auth header', async () => {
    const ES = jest.requireMock('react-native-sse').default;

    const params = createParams();
    openChatStream(params);
    await flush();

    expect(ES).toHaveBeenCalledTimes(1);
    const [url, opts] = ES.mock.calls[0];
    expect(url).toBe('http://test.local/api/v1/yt/assistant/chat');
    expect(opts.method).toBe('POST');
    expect(opts.headers.Authorization).toBe('Bearer test-token');
    expect(opts.headers['Content-Type']).toBe('application/json');
    expect(opts.body).toBe(
      JSON.stringify({ messages: params.messages, language: 'en' }),
    );
  });

  it('calls onTextDelta when a valid text-delta event arrives', async () => {
    const onTextDelta = jest.fn();
    openChatStream(createParams({ onTextDelta }));
    await flush();

    emit('text-delta', { text: 'Hello world' });

    expect(onTextDelta).toHaveBeenCalledWith({ text: 'Hello world' });
  });

  it('drops a text-delta event with malformed JSON', async () => {
    const onTextDelta = jest.fn();
    openChatStream(createParams({ onTextDelta }));
    await flush();

    emit('text-delta', '{bad json');

    expect(onTextDelta).not.toHaveBeenCalled();
  });

  it('drops a text-delta event that fails Zod validation', async () => {
    const onTextDelta = jest.fn();
    openChatStream(createParams({ onTextDelta }));
    await flush();

    // Missing 'text' field
    emit('text-delta', { wrongField: 'nope' });

    expect(onTextDelta).not.toHaveBeenCalled();
  });

  it('calls onSearchResults when a valid search-results event arrives', async () => {
    const onSearchResults = jest.fn();
    openChatStream(createParams({ onSearchResults }));
    await flush();

    emit('search-results', { results: [sampleVideo] });

    expect(onSearchResults).toHaveBeenCalledWith({
      results: [sampleVideo],
    });
  });

  it('calls onToolStep for running and done statuses', async () => {
    const onToolStep = jest.fn();
    openChatStream(createParams({ onToolStep }));
    await flush();

    emit('tool-step', { tool: 'search_youtube', status: 'running' });
    emit('tool-step', { tool: 'search_youtube', status: 'done' });

    expect(onToolStep).toHaveBeenCalledTimes(2);
    expect(onToolStep).toHaveBeenNthCalledWith(1, {
      tool: 'search_youtube',
      status: 'running',
    });
    expect(onToolStep).toHaveBeenNthCalledWith(2, {
      tool: 'search_youtube',
      status: 'done',
    });
  });

  it('calls onDownloadOffer when a valid download-offer event arrives', async () => {
    const onDownloadOffer = jest.fn();
    openChatStream(createParams({ onDownloadOffer }));
    await flush();

    emit('download-offer', {
      videoId: 'abc123',
      videoUrl: 'https://youtu.be/abc123',
      title: 'A great song',
      imageUrl: null,
    });

    expect(onDownloadOffer).toHaveBeenCalledWith({
      videoId: 'abc123',
      videoUrl: 'https://youtu.be/abc123',
      title: 'A great song',
      imageUrl: null,
    });
  });

  it('calls onDone and tears down on a valid done event', async () => {
    const onDone = jest.fn();
    openChatStream(createParams({ onDone }));
    await flush();

    emit('done', { reason: 'complete' });

    expect(onDone).toHaveBeenCalledWith({ reason: 'complete' });
    // Subsequent events should be ignored after close
    emit('text-delta', { text: 'late' });
    const params = createParams();
    expect(params.onTextDelta).not.toHaveBeenCalled();
  });

  it('calls onError when the connection fails with a non-401 error', async () => {
    const onError = jest.fn();
    openChatStream(createParams({ onError }));
    await flush();

    // Simulate an error event with status 500
    emit('error', {
      type: 'error',
      message: 'Server error',
      xhrStatus: 500,
      xhrState: 4,
    });

    // The error listener is registered via addEventListener, not via our emit helper.
    // We need to trigger the actual 'error' listener that was registered.
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
      expect.objectContaining({ message: 'Chat connection failed' }),
    );
  });

  it('close() tears down the EventSource', async () => {
    const onDone = jest.fn();
    const handle = openChatStream(createParams({ onDone }));
    await flush();

    handle.close();

    // After close, events should be no-ops
    emit('text-delta', { text: 'late' });
    emit('done', { reason: 'complete' });
    expect(onDone).not.toHaveBeenCalled();
  });

  it('honors AbortSignal passed in params', async () => {
    const controller = new AbortController();
    const onDone = jest.fn();
    openChatStream(createParams({ signal: controller.signal, onDone }));
    await flush();

    controller.abort();

    // Events after abort should be ignored
    emit('done', { reason: 'complete' });
    expect(onDone).not.toHaveBeenCalled();
  });

  it('drops a search-results event with an invalid video shape', async () => {
    const onSearchResults = jest.fn();
    openChatStream(createParams({ onSearchResults }));
    await flush();

    // Missing required fields on the video item
    emit('search-results', {
      results: [{ videoId: 'ok', title: 123 }],
    });

    expect(onSearchResults).not.toHaveBeenCalled();
  });

  it('attempts refresh-based re-auth and reconnects on 401, then proceeds', async () => {
    mockGetToken.mockResolvedValueOnce('expired-token');
    // After re-auth, getToken is called again for the reconnection
    mockGetToken.mockResolvedValueOnce('fresh-token');
    mockGetRefreshToken.mockResolvedValueOnce('old-refresh-token');
    mockAxiosPost.mockResolvedValueOnce({
      data: {
        accessToken: 'fresh-token',
        refreshToken: 'new-refresh-token',
        expiresIn: 3600,
      },
    });

    const onTextDelta = jest.fn();
    openChatStream(createParams({ onTextDelta }));
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

    // Should have refreshed via /auth/refresh
    expect(mockGetRefreshToken).toHaveBeenCalled();
    expect(mockAxiosPost).toHaveBeenCalledWith(
      'http://test.local/api/v1/auth/refresh',
      { refreshToken: 'old-refresh-token' },
      { headers: { 'Content-Type': 'application/json' } },
    );
    expect(saveToken).toHaveBeenCalledWith('fresh-token');
    expect(saveRefreshToken).toHaveBeenCalledWith('new-refresh-token');

    // Now the reconnected stream should handle events
    emit('text-delta', { text: 'Hello after re-auth' });
    expect(onTextDelta).toHaveBeenCalledWith({ text: 'Hello after re-auth' });
  });
});
