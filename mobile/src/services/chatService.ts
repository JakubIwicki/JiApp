import axios from 'axios';
import EventSource from 'react-native-sse';
import { API_BASE_URL } from '../config';
import i18next from '../i18n';
import {
  getToken,
  getRefreshToken,
  saveToken,
  saveRefreshToken,
} from './storageService';
import { RefreshResponseSchema } from '../types/schemas';
import {
  TextDeltaEventSchema,
  ToolStepEventSchema,
  SearchResultsEventSchema,
  DownloadOfferEventSchema,
  DoneEventSchema,
} from '../types/chat';
import type {
  TextDeltaEvent,
  ToolStepEvent,
  SearchResultsEvent,
  DownloadOfferEvent,
  DoneEvent,
} from '../types/chat';
import type { z } from 'zod';

// ── Types ──────────────────────────────────────────────────────────────────

export interface ChatStreamMessage {
  readonly role: 'user' | 'assistant';
  readonly content: string;
}

export interface ChatStreamParams {
  readonly messages: ChatStreamMessage[];
  readonly language?: string;
  readonly signal?: AbortSignal;
  readonly onTextDelta: (event: TextDeltaEvent) => void;
  readonly onToolStep: (event: ToolStepEvent) => void;
  readonly onSearchResults: (event: SearchResultsEvent) => void;
  readonly onDownloadOffer: (event: DownloadOfferEvent) => void;
  readonly onDone: (event: DoneEvent) => void;
  readonly onError: (error: Error) => void;
}

export interface ChatStreamHandle {
  close(): void;
}

// ── Event names the backend SSE stream uses ────────────────────────────────

type ChatEventName =
  | 'text-delta'
  | 'tool-step'
  | 'search-results'
  | 'download-offer'
  | 'done';

const EVENT_SCHEMAS: Record<ChatEventName, z.ZodSchema> = {
  'text-delta': TextDeltaEventSchema,
  'tool-step': ToolStepEventSchema,
  'search-results': SearchResultsEventSchema,
  'download-offer': DownloadOfferEventSchema,
  done: DoneEventSchema,
};

// ── Public API ─────────────────────────────────────────────────────────────

export function openChatStream(params: ChatStreamParams): ChatStreamHandle {
  let es: EventSource<ChatEventName> | null = null;
  let closed = false;

  const close = (): void => {
    closed = true;
    es?.close();
    es = null;
  };

  // Respect abort signal if provided
  if (params.signal) {
    if (params.signal.aborted) return { close };
    params.signal.addEventListener('abort', close, { once: true });
  }

  const startConnection = async (isRetry: boolean = false): Promise<void> => {
    if (closed) return;

    // Read the freshest token immediately before connecting
    const token = await getToken();
    if (closed) return;
    if (!token) {
      params.onError(new Error('Not authenticated'));
      return;
    }

    const body = JSON.stringify({
      messages: params.messages,
      language: params.language ?? (i18next.language || 'pl'),
    });

    es = new EventSource<ChatEventName>(`${API_BASE_URL}/yt/assistant/chat`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body,
    });

    // If closed() was called while we were awaiting getToken, tear down
    if (closed) {
      es.close();
      es = null;
      return;
    }

    // ── Wire named events with Zod validation ───────────────────────────

    const eventNames: ChatEventName[] = [
      'text-delta',
      'tool-step',
      'search-results',
      'download-offer',
      'done',
    ];

    for (const name of eventNames) {
      es.addEventListener(name, event => {
        if (closed) return;
        if (event.data === null) return;

        let raw: unknown;
        try {
          raw = JSON.parse(event.data);
        } catch {
          console.warn(`[chatService] Invalid JSON in ${name} event dropped`);
          return;
        }

        const schema = EVENT_SCHEMAS[name];
        const parsed = schema.safeParse(raw);
        if (!parsed.success) {
          console.warn(
            `[chatService] Zod validation failed for ${name} event:`,
            parsed.error,
          );
          return;
        }

        switch (name) {
          case 'text-delta':
            params.onTextDelta(parsed.data as TextDeltaEvent);
            break;
          case 'tool-step':
            params.onToolStep(parsed.data as ToolStepEvent);
            break;
          case 'search-results':
            params.onSearchResults(parsed.data as SearchResultsEvent);
            break;
          case 'download-offer':
            params.onDownloadOffer(parsed.data as DownloadOfferEvent);
            break;
          case 'done':
            close();
            params.onDone(parsed.data as DoneEvent);
            break;
        }
      });
    }

    // ── Error handling with 401 re-auth (mirrors apiClient.ts) ──────────

    es.addEventListener('error', async event => {
      if (closed) return;

      if (
        event.type === 'error' &&
        'xhrStatus' in event &&
        event.xhrStatus === 401 &&
        !isRetry
      ) {
        // Close current connection but allow one reconnect attempt
        es?.close();
        es = null;
        closed = true;

        try {
          const storedRefreshToken = await getRefreshToken();
          if (storedRefreshToken) {
            const refreshResponse = await axios.post<unknown>(
              `${API_BASE_URL}/auth/refresh`,
              { refreshToken: storedRefreshToken },
              { headers: { 'Content-Type': 'application/json' } },
            );

            const data = RefreshResponseSchema.parse(refreshResponse.data);
            await Promise.all([
              saveToken(data.accessToken),
              saveRefreshToken(data.refreshToken),
            ]);

            // Reconnect with the fresh token
            closed = false;
            await startConnection(true);
            return;
          }
        } catch {
          // Refresh failed — fall through to error
        }
      }

      close();
      params.onError(new Error('Chat connection failed'));
    });
  };

  startConnection().catch(() => {
    if (!closed) {
      close();
      params.onError(new Error('Chat connection failed'));
    }
  });

  return { close };
}
