import { useState, useCallback, useRef, useEffect } from 'react';
import i18next from '../i18n';
import { openChatStream } from '../services/chatService';
import { requestDownloadLink, downloadFile } from '../services/downloadService';
import { waitForDownload } from '../services/downloadJob';
import { getErrorMessage } from '../utils/errorUtils';
import type { ChatMessage, OfferStatus } from '../types/chat';
import type {
  ChatStreamHandle,
  ChatStreamMessage,
} from '../services/chatService';

// ── Constants ──────────────────────────────────────────────────────────────

/** Maximum number of messages sent to the backend per turn. */
const CHAT_HISTORY_CAP = 14;

/** Ephemeral message id counter */
let nextId = 0;

// ── Hook result type ───────────────────────────────────────────────────────

interface UseChatResult {
  readonly messages: ChatMessage[];
  readonly isStreaming: boolean;
  readonly error: string | null;
  readonly send: (text: string) => void;
  readonly clear: () => void;
  readonly confirmDownload: (messageId: string) => void;
}

// ── API-message mapping helpers ────────────────────────────────────────────

/**
 * Map client-held ChatMessages to the compact format sent to the backend.
 * Applies: history cap (last N messages), strips video/tool payloads
 * (only plain text is sent), and excludes assistant turns whose offer was
 * never confirmed (stale-offer expiry).
 */
function mapToApiMessages(messages: ChatMessage[]): ChatStreamMessage[] {
  return messages
    .slice(-CHAT_HISTORY_CAP)
    .filter(m => {
      // Exclude assistant messages with an unconfirmed offer
      if (
        m.role === 'assistant' &&
        m.offer &&
        m.offerStatus !== 'done' &&
        m.offerStatus !== 'error'
      ) {
        return false;
      }
      // Drop empty assistant turns (no prose content)
      if (m.role === 'assistant' && m.text.trim().length === 0) {
        return false;
      }
      return true;
    })
    .map(m => ({
      role: m.role as 'user' | 'assistant',
      content: m.text,
    }));
}

// ── Hook ───────────────────────────────────────────────────────────────────

const useChat = (): UseChatResult => {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isStreaming, setIsStreaming] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const streamRef = useRef<ChatStreamHandle | null>(null);
  const abortRef = useRef<AbortController | null>(null);
  const downloadControllersRef = useRef<Set<AbortController>>(new Set());

  // mount only — abort in-flight downloads and close the SSE stream on unmount
  useEffect(() => {
    // The Set reference is stable (never reassigned, only mutated), so
    // capturing it here still sees every controller added by later runs.
    const controllers = downloadControllersRef.current;
    return () => {
      streamRef.current?.close();
      controllers.forEach(controller => controller.abort());
    };
  }, []);

  const clear = useCallback(() => {
    streamRef.current?.close();
    streamRef.current = null;
    setMessages([]);
    setIsStreaming(false);
    setError(null);
  }, [streamRef]);

  // ── confirmDownload (runs through REST, NOT the SSE stream) ─────────────────

  const confirmDownload = useCallback(
    (messageId: string) => {
      // Capture the offer data before mutating state — a missing offer must
      // not flip the chip to 'downloading' with no request in flight.
      const message = messages.find(m => m.id === messageId);
      const offer = message?.offer;
      if (!offer) {
        console.warn(
          '[useChat] confirmDownload: no offer for message',
          messageId,
        );
        return;
      }

      // Mark the offer as downloading
      setMessages(prev =>
        prev.map(m =>
          m.id === messageId && m.offer
            ? { ...m, offerStatus: 'downloading' as OfferStatus }
            : m,
        ),
      );

      const title = offer.title ?? offer.videoId;

      const controller = new AbortController();
      downloadControllersRef.current.add(controller);

      // Run the existing download flow outside the SSE stream
      (async () => {
        try {
          const { tempId, downloadUrl } = await requestDownloadLink(
            {
              videoId: offer.videoId,
              videoUrl: offer.videoUrl,
              title: offer.title ?? undefined,
              imageUrl: offer.imageUrl ?? undefined,
            },
            controller.signal,
          );

          await waitForDownload(tempId, controller.signal);

          await downloadFile(downloadUrl, title, controller.signal);

          // Success
          setMessages(prev =>
            prev.map(m =>
              m.id === messageId
                ? { ...m, offerStatus: 'done' as OfferStatus }
                : m,
            ),
          );

          // Append a synthetic note so the model can reason from the result
          const note: ChatMessage = {
            id: `msg-${nextId++}`,
            role: 'user',
            text: i18next.t('chat.downloadNote.success', { title }),
          };
          setMessages(prev => [...prev, note]);
        } catch (err) {
          // A cancelled download must not surface the abort message to the chip
          if (err instanceof Error && err.name === 'AbortError') return;
          console.warn('[useChat] confirmDownload failed', err);
          // Failure
          setMessages(prev =>
            prev.map(m =>
              m.id === messageId
                ? { ...m, offerStatus: 'error' as OfferStatus }
                : m,
            ),
          );

          const reason = err instanceof Error ? err.message : 'Download failed';
          const note: ChatMessage = {
            id: `msg-${nextId++}`,
            role: 'user',
            text: i18next.t('chat.downloadNote.failed', { reason }),
          };
          setMessages(prev => [...prev, note]);
        } finally {
          downloadControllersRef.current.delete(controller);
        }
      })();
    },
    [messages],
  );

  // ── send (SSE chat stream) ───────────────────────────────────────────────

  const send = useCallback(
    (text: string) => {
      // Cancel any in-flight stream
      streamRef.current?.close();
      abortRef.current?.abort();

      const userMsg: ChatMessage = {
        id: `msg-${nextId++}`,
        role: 'user',
        text,
      };

      const assistantId = `msg-${nextId++}`;
      const assistantMsg: ChatMessage = {
        id: assistantId,
        role: 'assistant',
        text: '',
        pending: true,
        toolSteps: [],
      };

      setMessages(prev => [...prev, userMsg, assistantMsg]);
      setIsStreaming(true);
      setError(null);

      // Build API message history with cap, offer-expiry, and payload stripping
      const apiMessages = mapToApiMessages([...messages, userMsg]);

      const inFlightId = assistantId;

      // Helper to update the in-flight assistant message in state
      const updateAssistant = (
        updater: (msg: ChatMessage) => ChatMessage,
      ): void => {
        setMessages(prev =>
          prev.map(m => (m.id === inFlightId ? updater(m) : m)),
        );
      };

      const handle = openChatStream({
        messages: apiMessages,

        onTextDelta: event => {
          updateAssistant(msg => ({
            ...msg,
            text: msg.text + event.text,
          }));
        },

        onToolStep: event => {
          updateAssistant(msg => {
            const steps = msg.toolSteps ?? [];
            const existingIdx = steps.findIndex(s => s.tool === event.tool);
            if (existingIdx >= 0) {
              const updated = [...steps];
              updated[existingIdx] = {
                tool: event.tool,
                status: event.status,
              };
              return { ...msg, toolSteps: updated };
            }
            return {
              ...msg,
              toolSteps: [...steps, { tool: event.tool, status: event.status }],
            };
          });
        },

        onSearchResults: event => {
          updateAssistant(msg => ({
            ...msg,
            videos: event.results,
          }));
        },

        onDownloadOffer: event => {
          updateAssistant(msg => ({
            ...msg,
            offer: {
              videoId: event.videoId,
              videoUrl: event.videoUrl,
              title: event.title,
              imageUrl: event.imageUrl,
            },
            offerStatus: 'idle' as OfferStatus,
          }));
        },

        onDone: () => {
          updateAssistant(msg => ({ ...msg, pending: false }));
          setIsStreaming(false);
          streamRef.current = null;
        },

        onError: err => {
          setError(getErrorMessage(err, 'Chat failed'));
          updateAssistant(msg => ({
            ...msg,
            pending: false,
            text: msg.text || err.message,
          }));
          setIsStreaming(false);
          streamRef.current = null;
        },
      });

      streamRef.current = handle;
    },
    [messages, streamRef],
  );

  return { messages, isStreaming, error, send, clear, confirmDownload };
};

export default useChat;
