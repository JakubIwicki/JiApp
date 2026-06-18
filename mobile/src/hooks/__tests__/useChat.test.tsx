import { renderHook, act } from '@testing-library/react-native';
import useChat from '../useChat';
import type {
  ChatStreamHandle,
  ChatStreamParams,
} from '../../services/chatService';

// ── Mocks ──────────────────────────────────────────────────────────────────

const mockClose = jest.fn();

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

const mockRequestDownloadLink = jest.fn();
const mockDownloadFile = jest.fn();

jest.mock('../../services/downloadService', () => ({
  requestDownloadLink: (...args: unknown[]) => mockRequestDownloadLink(...args),
  downloadFile: (...args: unknown[]) => mockDownloadFile(...args),
}));

jest.mock('../../i18n', () => ({
  __esModule: true,
  default: {
    t: (key: string, opts?: Record<string, string>) => {
      if (key === 'chat.downloadNote.success') {
        return `Downloaded '${opts?.title ?? '?'}'.`;
      }
      if (key === 'chat.downloadNote.failed') {
        return `Download failed: ${opts?.reason ?? '?'}.`;
      }
      return key;
    },
    language: 'en',
  },
}));

beforeEach(() => {
  jest.clearAllMocks();
  capturedParams = null;
  mockRequestDownloadLink.mockResolvedValue({
    downloadUrl: 'https://example.com/file.mp3',
  });
  mockDownloadFile.mockResolvedValue({
    contentUri: 'content://test/file.mp3',
    displayPath: 'Download/test.mp3',
    filePath: '/tmp/test.mp3',
  });
});

// ── Helpers ────────────────────────────────────────────────────────────────

const emitText = (text: string): void => {
  capturedParams?.onTextDelta({ text });
};

const emitToolStep = (tool: string, status: 'running' | 'done'): void => {
  capturedParams?.onToolStep({ tool, status });
};

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

const emitDownloadOffer = (offer: {
  videoId: string;
  videoUrl: string;
  title: string | null;
  imageUrl: string | null;
}): void => {
  capturedParams?.onDownloadOffer(offer);
};

const emitDone = (
  reason: 'complete' | 'max_iterations' | 'error' = 'complete',
): void => {
  capturedParams?.onDone({ reason });
};

const emitError = (message: string): void => {
  capturedParams?.onError(new Error(message));
};

/** Flush microtask queue so async IIFEs inside confirmDownload settle */
const flushMicrotasks = () =>
  act(async () => {
    await new Promise<void>(r => setTimeout(r, 10));
  });

// ── Tests ──────────────────────────────────────────────────────────────────

describe('useChat', () => {
  // ── Existing tests ─────────────────────────────────────────────────────

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
    const p1 = mockOpenChatStream.mock.calls[0][0] as ChatStreamParams;
    expect(p1.messages).toEqual([{ role: 'user', content: 'First message' }]);

    // Second turn should include both user messages (assistant msgs empty → filtered)
    act(() => {
      emitDone();
    });
    act(() => {
      result.current.send('Second message');
    });

    const p2 = mockOpenChatStream.mock.calls[1][0] as ChatStreamParams;
    expect(p2.messages).toHaveLength(2);
    expect(p2.messages[0]).toEqual({ role: 'user', content: 'First message' });
    expect(p2.messages[1]).toEqual({ role: 'user', content: 'Second message' });
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

  it('sets offer with idle status on download-offer event', () => {
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
    expect(result.current.messages[1].offerStatus).toBe('idle');
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

  // ── confirmDownload ────────────────────────────────────────────────────

  it('confirmDownload success: transitions status and appends success note', async () => {
    const { result } = renderHook(() => useChat());

    act(() => {
      result.current.send('Download that');
    });
    act(() => {
      emitDownloadOffer({
        videoId: 'vid123',
        videoUrl: 'https://youtu.be/vid123',
        title: 'Cool Song',
        imageUrl: null,
      });
    });
    act(() => {
      emitDone();
    });

    const offerMsgId = result.current.messages[1].id;

    act(() => {
      result.current.confirmDownload(offerMsgId);
    });

    // Should immediately mark as downloading
    expect(
      result.current.messages.find(m => m.id === offerMsgId)?.offerStatus,
    ).toBe('downloading');

    // Flush microtasks so the async download flow completes
    await flushMicrotasks();

    // Should be done
    expect(
      result.current.messages.find(m => m.id === offerMsgId)?.offerStatus,
    ).toBe('done');

    // Should have appended a success note
    const msgs = result.current.messages;
    const lastMsg = msgs[msgs.length - 1];
    expect(lastMsg.role).toBe('user');
    expect(lastMsg.text).toContain('Cool Song');

    // Should have called the download service with correct params
    expect(mockRequestDownloadLink).toHaveBeenCalledWith({
      videoId: 'vid123',
      videoUrl: 'https://youtu.be/vid123',
      title: 'Cool Song',
      imageUrl: undefined,
    });
    expect(mockDownloadFile).toHaveBeenCalledWith(
      'https://example.com/file.mp3',
      'Cool Song',
    );
  });

  it('confirmDownload failure: transitions to error and appends failure note', async () => {
    mockRequestDownloadLink.mockImplementationOnce(() =>
      Promise.reject(new Error('Network down')),
    );

    const { result } = renderHook(() => useChat());

    act(() => {
      result.current.send('Download that');
    });
    act(() => {
      emitDownloadOffer({
        videoId: 'vidFail',
        videoUrl: 'https://youtu.be/vidFail',
        title: null,
        imageUrl: null,
      });
    });
    act(() => {
      emitDone();
    });

    const offerMsgId = result.current.messages[1].id;

    act(() => {
      result.current.confirmDownload(offerMsgId);
    });

    await flushMicrotasks();

    expect(
      result.current.messages.find(m => m.id === offerMsgId)?.offerStatus,
    ).toBe('error');

    const msgs = result.current.messages;
    const lastMsg = msgs[msgs.length - 1];
    expect(lastMsg.role).toBe('user');
    expect(lastMsg.text).toContain('Network down');
  });

  // ── History mapping ────────────────────────────────────────────────────

  it('history cap: limits API messages to last CHAT_HISTORY_CAP entries', () => {
    const { result } = renderHook(() => useChat());
    for (let i = 0; i < 16; i++) {
      act(() => {
        result.current.send(`Turn ${i}`);
      });
      act(() => {
        emitText(`Response ${i}`);
      });
      act(() => {
        emitDone();
      });
    }
    act(() => {
      result.current.send('Hello');
    });
    const callIdx = mockOpenChatStream.mock.calls.length - 1;
    const params = mockOpenChatStream.mock.calls[
      callIdx
    ][0] as ChatStreamParams;
    expect(params.messages.length).toBeLessThanOrEqual(20);
  });

  it('excludes assistant turns with unconfirmed offers from API mapping', () => {
    const { result } = renderHook(() => useChat());

    act(() => {
      result.current.send('Offer me something');
    });
    act(() => {
      emitDownloadOffer({
        videoId: 'unconfirmed',
        videoUrl: 'https://youtu.be/unconfirmed',
        title: 'Unconfirmed Song',
        imageUrl: null,
      });
    });
    act(() => {
      emitText('Here is a song');
    });
    act(() => {
      emitDone();
    });

    act(() => {
      result.current.send('Next message');
    });

    const callIdx = mockOpenChatStream.mock.calls.length - 1;
    const params = mockOpenChatStream.mock.calls[
      callIdx
    ][0] as ChatStreamParams;
    const hasUnconfirmed = params.messages.some(m =>
      m.content.includes('Here is a song'),
    );
    expect(hasUnconfirmed).toBe(false);
  });

  it('includes assistant turns with confirmed (done) offers in API mapping', async () => {
    const { result } = renderHook(() => useChat());

    act(() => {
      result.current.send('Offer me something');
    });
    act(() => {
      emitDownloadOffer({
        videoId: 'confirmed',
        videoUrl: 'https://youtu.be/confirmed',
        title: 'Confirmed Song',
        imageUrl: null,
      });
    });
    act(() => {
      emitText('Here is a song to download');
    });
    act(() => {
      emitDone();
    });

    const offerMsgId = result.current.messages[1].id;

    act(() => {
      result.current.confirmDownload(offerMsgId);
    });
    await flushMicrotasks();

    // Now confirmed offer turn should be included in next send
    act(() => {
      result.current.send('Next message');
    });

    const callIdx = mockOpenChatStream.mock.calls.length - 1;
    const params = mockOpenChatStream.mock.calls[
      callIdx
    ][0] as ChatStreamParams;
    const hasConfirmed = params.messages.some(m =>
      m.content.includes('Here is a song to download'),
    );
    expect(hasConfirmed).toBe(true);
  });

  it('strips video/tool payloads from API mapping (only plain text sent)', () => {
    const { result } = renderHook(() => useChat());

    act(() => {
      result.current.send('Find a video');
    });
    act(() => {
      emitToolStep('search_youtube', 'running');
    });
    act(() => {
      emitToolStep('search_youtube', 'done');
    });
    act(() => {
      emitSearchResults([
        {
          videoId: 'abc',
          title: 'Big Video Title',
          description: 'Desc',
          imageUrl: 'https://img.example.com/big.jpg',
          videoUrl: 'https://youtube.com/watch?v=abc',
          channelTitle: 'Channel',
        },
      ]);
    });
    act(() => {
      emitText('Found this video for you');
    });
    act(() => {
      emitDone();
    });

    act(() => {
      result.current.send('Thanks');
    });

    const callIdx = mockOpenChatStream.mock.calls.length - 1;
    const params = mockOpenChatStream.mock.calls[
      callIdx
    ][0] as ChatStreamParams;
    const allContent = params.messages.map(m => m.content).join(' ');
    expect(allContent).not.toContain('Big Video Title');
    expect(allContent).not.toContain('videoId');
  });

  it('synthetic success note is included in next turn API mapping', async () => {
    const { result } = renderHook(() => useChat());

    act(() => {
      result.current.send('Download that');
    });
    act(() => {
      emitDownloadOffer({
        videoId: 'vidNote',
        videoUrl: 'https://youtu.be/vidNote',
        title: 'Note Song',
        imageUrl: null,
      });
    });
    act(() => {
      emitDone();
    });

    const offerMsgId = result.current.messages[1].id;

    act(() => {
      result.current.confirmDownload(offerMsgId);
    });
    await flushMicrotasks();

    act(() => {
      result.current.send('Another message');
    });

    const callIdx = mockOpenChatStream.mock.calls.length - 1;
    const params = mockOpenChatStream.mock.calls[
      callIdx
    ][0] as ChatStreamParams;
    const hasNote = params.messages.some(m =>
      m.content.includes("Downloaded 'Note Song'"),
    );
    expect(hasNote).toBe(true);
  });
});
