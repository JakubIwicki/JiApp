import axios from 'axios';
import EventSource from 'react-native-sse';
import type { LoginApiRaw } from '../../../types/api';
import { API_BASE_URL } from '../../../config';
import { PresenceEventSchema } from '../types/events';
import {
  getToken,
  getCredentials,
  saveToken,
  saveUserId,
  saveDisplayName,
  saveUsername,
} from '../../../services/storageService';

// ── Types ──────────────────────────────────────────────────────────────────

export interface BoardStreamParams {
  readonly boardId: number;
  readonly onChange: () => void;
  readonly onPresence: (userIds: number[]) => void;
  readonly onOpen?: () => void;
  readonly onError?: (e: Error) => void;
}

export interface BoardStreamHandle {
  close(): void;
}

// ── Event names the backend SSE stream uses ────────────────────────────────

type BoardEventName =
  | 'presence'
  | 'item.added'
  | 'item.updated'
  | 'item.status'
  | 'item.removed'
  | 'items.cleared'
  | 'board.updated'
  | 'member.changed'
  | 'recurring.reset'
  | 'board.deleted';

const CHANGE_EVENT_NAMES: ReadonlySet<BoardEventName> = new Set([
  'item.added',
  'item.updated',
  'item.status',
  'item.removed',
  'items.cleared',
  'board.updated',
  'member.changed',
  'recurring.reset',
  'board.deleted',
]);

// ── Public API ─────────────────────────────────────────────────────────────

export function openBoardStream(params: BoardStreamParams): BoardStreamHandle {
  let es: EventSource<BoardEventName> | null = null;
  let closed = false;

  const close = (): void => {
    closed = true;
    es?.close();
    es = null;
  };

  const startConnection = async (isRetry: boolean = false): Promise<void> => {
    if (closed) return;

    // Read the freshest token immediately before connecting
    const token = await getToken();
    if (closed) return;
    if (!token) {
      params.onError?.(new Error('Not authenticated'));
      return;
    }

    es = new EventSource<BoardEventName>(
      `${API_BASE_URL}/lovingboards/boards/${params.boardId}/stream`,
      {
        method: 'GET',
        headers: {
          Authorization: `Bearer ${token}`,
        },
      },
    );

    // If close() was called while we were awaiting getToken, tear down
    if (closed) {
      es.close();
      es = null;
      return;
    }

    // ── Wire named events ───────────────────────────────────────────────

    // presence → Zod-validated
    es.addEventListener('presence', event => {
      if (closed) return;
      if (event.data === null) return;

      let raw: unknown;
      try {
        raw = JSON.parse(event.data);
      } catch {
        console.warn(
          '[boardStreamService] Invalid JSON in presence event dropped',
        );
        return;
      }

      const parsed = PresenceEventSchema.safeParse(raw);
      if (!parsed.success) {
        console.warn(
          '[boardStreamService] Zod validation failed for presence event:',
          parsed.error,
        );
        return;
      }

      params.onPresence(parsed.data.userIds);
    });

    // All board/item change events → single onChange callback
    for (const name of CHANGE_EVENT_NAMES) {
      es.addEventListener(name, event => {
        if (closed) return;
        params.onChange();
      });
    }

    // open event → resync
    es.addEventListener('open', () => {
      if (closed) return;
      params.onOpen?.();
    });

    // ── Error handling with 401 re-auth (mirrors chatService) ───────────

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
          const credentials = await getCredentials();
          if (credentials) {
            const loginResponse = await axios.post<LoginApiRaw>(
              `${API_BASE_URL}/auth/login`,
              {
                username: credentials.username,
                password: credentials.password,
              },
              { headers: { 'Content-Type': 'application/json' } },
            );

            const { accessToken, userId, displayName } = loginResponse.data;
            await Promise.all([
              saveToken(accessToken),
              saveUserId(userId),
              saveDisplayName(displayName ?? ''),
              saveUsername(credentials.username),
            ]);

            // Reconnect with the fresh token
            closed = false;
            await startConnection(true);
            return;
          }
        } catch {
          // Re-login failed — fall through to error
        }
      }

      close();
      params.onError?.(new Error('Board stream connection failed'));
    });
  };

  startConnection().catch(() => {
    if (!closed) {
      close();
      params.onError?.(new Error('Board stream connection failed'));
    }
  });

  return { close };
}
