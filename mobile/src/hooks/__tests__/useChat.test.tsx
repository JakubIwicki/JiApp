import { renderHook, act } from '@testing-library/react-native';
import useChat from '../useChat';
import type {
  ChatStreamHandle,
  ChatStreamParams,
} from '../../services/chatService';

// ── Mocks ──────────────────────────────────────────────────────────────────

const mockClose = jest.fn();

// Capture the params passed to openChatStream so tests can drive callbacks
let capturedParams: ChatStreamParams | null = null;

jest.mock('../../services/chatService', () => ({
  openChatStream: jest.fn((params: ChatStreamParams): ChatStreamHandle => {
    capturedParams = params;
    return { close: mockClose };
  }),
}));

const { openChatStream: mockOpenChatStream } = jest.requireMock(
  '../../services/chatService',
) as { openChatStream: jest.Mock };

beforeEach(() => {
  jest.clearAllMocks();
  capturedParams = null;
});

// ── Helpers ────────────────────────────────────────────────────────────────

/** Call the captured onTextDelta callback */
const emitText = (text: string): void => {
  capturedParams?.onTextDelta({ text });
};

/** Call the captured onToolStep callback */
const emitToolStep = (tool: string, status: 'running' | 'done'): void => {
  capturedParams?.onToolStep({ tool, status });
};

/** Call the captured onSearchResults callback */
const emitSearchResults = (
  results: Array<{
    videoId: string;
    title: string;
    description: string;
    imageUrl: string;
    videoUrl: string;
    channelTitle: string;
  }>,
): void => {
  capturedParams?.onSearchResults({ results });
};

/** Call the captured onDownloadOffer callback */
const emitDownloadOffer = (offer: {
  videoId: string;
  videoUrl: string;
  title: string | null;
  imageUrl: string | null;
}): void => {
  capturedParams?.onDownloadOffer(offer);
};

/** Call the captured onDone callback */
const emitDone = (
  reason: 'complete' | 'max_iterations' | 'error' = 'complete',
): void => {
  capturedParams?.onDone({ reason });
};

/** Call the captured onError callback */
const emitError = (message: string): void => {
  capturedParams?.onError(new Error(message));
};

// ── Tests ──────────────────────────────────────────────────────────────────

describe('useChat', () => {
  it('initial state has empty messages, isStreaming=false, error=null', () => {
    const { result } = renderHook(() => useChat());

    expect(result.current.messages).toEqual([]);
    expect(result.current.isStreaming).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it('send() appends user and assistant messages', () => {
    const { result } = renderHook(() => useChat());

    act(() => {
      result.current.send('Hello there');
    });

    expect(result.current.messages).toHaveLength(2);
    expect(result.current.messages[0]).toMatchObject({
      role: 'user',
      text: 'Hello there',
    });
    expect(result.current.messages[1]).toMatchObject({
      role: 'assistant',
      text: '',
      pending: true,
    });
    expect(result.current.isStreaming).toBe(true);
  });

  it('send() maps conversation history to API message format', () => {
    const { result } = renderHook(() => useChat());

    act(() => {
      result.current.send('First message');
    });

    expect(mockOpenChatStream).toHaveBeenCalledTimes(1);
    const params = mockOpenChatStream.mock.calls[0][0] as ChatStreamParams;
    expect(params.messages).toEqual([
      { role: 'user', content: 'First message' },
    ]);

    // Second turn should include both user messages
    act(() => {
      // Manually complete the first stream
      emitDone();
    });
    act(() => {
      result.current.send('Second message');
    });

    const params2 = mockOpenChatStream.mock.calls[1][0] as ChatStreamParams;
    expect(params2.messages).toHaveLength(2);
    expect(params2.messages[0]).toEqual({
      role: 'user',
      content: 'First message',
    });
    expect(params2.messages[1]).toEqual({
      role: 'user',
      content: 'Second message',
    });
  });

  it('applies text-delta events to the in-flight assistant message', () => {
    const { result } = renderHook(() => useChat());

    act(() => {
      result.current.send('Hi');
    });

    act(() => {
      emitText('Hello ');
    });
    act(() => {
      emitText('world!');
    });

    expect(result.current.messages[1].text).toBe('Hello world!');
    expect(result.current.messages[1].pending).toBe(true);
  });

  it('tracks tool steps (running then done)', () => {
    const { result } = renderHook(() => useChat());

    act(() => {
      result.current.send('Search for lofi');
    });

    act(() => {
      emitToolStep('search_youtube', 'running');
    });

    expect(result.current.messages[1].toolSteps).toEqual([
      { tool: 'search_youtube', status: 'running' },
    ]);

    act(() => {
      emitToolStep('search_youtube', 'done');
    });

    expect(result.current.messages[1].toolSteps).toEqual([
      { tool: 'search_youtube', status: 'done' },
    ]);
  });

  it('tracks multiple distinct tool steps', () => {
    const { result } = renderHook(() => useChat());

    act(() => {
      result.current.send('Download this');
    });

    act(() => {
      emitToolStep('search_youtube', 'running');
    });
    act(() => {
      emitToolStep('search_youtube', 'done');
    });
    act(() => {
      emitToolStep('offer_download', 'running');
    });

    expect(result.current.messages[1].toolSteps).toEqual([
      { tool: 'search_youtube', status: 'done' },
      { tool: 'offer_download', status: 'running' },
    ]);
  });

  it('sets videos on search-results event', () => {
    const { result } = renderHook(() => useChat());

    act(() => {
      result.current.send('Find me a song');
    });

    const videos = [
      {
        videoId: 'abc123',
        title: 'A Great Song',
        description: 'Music video',
        imageUrl: 'https://img.example.com/abc.jpg',
        videoUrl: 'https://youtube.com/watch?v=abc123',
        channelTitle: 'MusicChannel',
      },
    ];

    act(() => {
      emitSearchResults(videos);
    });

    expect(result.current.messages[1].videos).toEqual(videos);
  });

  it('sets offer on download-offer event', () => {
    const { result } = renderHook(() => useChat());

    act(() => {
      result.current.send('Download that video');
    });

    act(() => {
      emitDownloadOffer({
        videoId: 'vid123',
        videoUrl: 'https://youtu.be/vid123',
        title: 'Cool Video',
        imageUrl: null,
      });
    });

    expect(result.current.messages[1].offer).toEqual({
      videoId: 'vid123',
      videoUrl: 'https://youtu.be/vid123',
      title: 'Cool Video',
      imageUrl: null,
    });
  });

  it('clears pending and stops streaming on done', () => {
    const { result } = renderHook(() => useChat());

    act(() => {
      result.current.send('Hi');
    });

    expect(result.current.isStreaming).toBe(true);
    expect(result.current.messages[1].pending).toBe(true);

    act(() => {
      emitDone();
    });

    expect(result.current.messages[1].pending).toBe(false);
    expect(result.current.isStreaming).toBe(false);
  });

  it('sets error and clears pending on stream error', () => {
    const { result } = renderHook(() => useChat());

    act(() => {
      result.current.send('Hi');
    });

    act(() => {
      emitError('Network failure');
    });

    expect(result.current.error).toBe('Network failure');
    expect(result.current.messages[1].pending).toBe(false);
    expect(result.current.isStreaming).toBe(false);
  });

  it('preserves accumulated text on error', () => {
    const { result } = renderHook(() => useChat());

    act(() => {
      result.current.send('Hi');
    });
    act(() => {
      emitText('Partial response');
    });
    act(() => {
      emitError('Connection lost');
    });

    expect(result.current.messages[1].text).toBe('Partial response');
    expect(result.current.error).toBe('Connection lost');
  });

  it('clear() resets conversation and closes stream', () => {
    const { result } = renderHook(() => useChat());

    act(() => {
      result.current.send('Hello');
    });
    act(() => {
      emitText('World');
    });

    expect(result.current.messages).toHaveLength(2);

    act(() => {
      result.current.clear();
    });

    expect(result.current.messages).toEqual([]);
    expect(result.current.isStreaming).toBe(false);
    expect(result.current.error).toBeNull();
    expect(mockClose).toHaveBeenCalled();
  });

  it('closes previous stream when send() is called again', () => {
    const { result } = renderHook(() => useChat());

    act(() => {
      result.current.send('First');
    });

    const firstClose = mockClose;

    act(() => {
      result.current.send('Second');
    });

    expect(firstClose).toHaveBeenCalled();
  });
});
