import EventSource from 'react-native-sse';
import { API_BASE_URL } from '../../../config';
import { PresenceEventSchema } from '../types/events';
import { getToken } from '../../../services/storageService';
import { refreshAuth } from '../../../services/apiClient';

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
  let userClosed = false; // set only by close(), never reset
  let reconnecting = false; // local, mid-refresh guard

  const close = (): void => {
    userClosed = true;
    es?.close();
    es = null;
  };

  const startConnection = async (isRetry: boolean = false): Promise<void> => {
    if (userClosed) return;

    // Read the freshest token immediately before connecting
    const token = await getToken();
    if (userClosed) return;
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
    if (userClosed) {
      es.close();
      es = null;
      return;
    }

    // ── Wire named events ───────────────────────────────────────────────

    // presence → Zod-validated
    es.addEventListener('presence', event => {
      if (userClosed) return;
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
      es.addEventListener(name, _event => {
        if (userClosed) return;
        params.onChange();
      });
    }

    // open event → resync
    es.addEventListener('open', () => {
      if (userClosed) return;
      params.onOpen?.();
    });

    // ── Error handling with 401 re-auth (shared single-flight refresh) ──

    es.addEventListener('error', async event => {
      if (userClosed || reconnecting) return;

      if (
        event.type === 'error' &&
        'xhrStatus' in event &&
        event.xhrStatus === 401 &&
        !isRetry
      ) {
        // Close current connection but allow one reconnect attempt
        reconnecting = true;
        es?.close();
        es = null;

        try {
          const newToken = await refreshAuth();
          // Consumer closed while we were refreshing — do not reconnect
          if (userClosed) return;
          if (newToken) {
            // Reconnect with the fresh token
            await startConnection(true);
            return;
          }
        } catch {
          // Refresh failed — fall through to error
        } finally {
          reconnecting = false;
        }
      }

      close();
      params.onError?.(new Error('Board stream connection failed'));
    });
  };

  startConnection().catch(() => {
    if (!userClosed) {
      close();
      params.onError?.(new Error('Board stream connection failed'));
    }
  });

  return { close };
}
