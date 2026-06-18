import { useState, useCallback, useRef } from 'react';
import { openChatStream } from '../services/chatService';
import { getErrorMessage } from '../utils/errorUtils';
import type { ChatMessage } from '../types/chat';
import type {
  ChatStreamHandle,
  ChatStreamMessage,
} from '../services/chatService';

// ── Ephemeral message id counter ───────────────────────────────────────────

let nextId = 0;

// ── Hook result type ───────────────────────────────────────────────────────

interface UseChatResult {
  readonly messages: ChatMessage[];
  readonly isStreaming: boolean;
  readonly error: string | null;
  readonly send: (text: string) => void;
  readonly clear: () => void;
}

// ── Hook ───────────────────────────────────────────────────────────────────

const useChat = (): UseChatResult => {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isStreaming, setIsStreaming] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const streamRef = useRef<ChatStreamHandle | null>(null);

  const clear = useCallback(() => {
    streamRef.current?.close();
    streamRef.current = null;
    setMessages([]);
    setIsStreaming(false);
    setError(null);
  }, [streamRef]);

  const send = useCallback(
    (text: string) => {
      // Cancel any in-flight stream
      streamRef.current?.close();

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

      // Build API message history (prior turns + the new user message).
      // Drop empty assistant turns (e.g. a search turn that returned only
      // results with no prose) — resending blank assistant content adds no
      // context and the backend is stateless, reconstructing from this list.
      const apiMessages: ChatStreamMessage[] = [...messages, userMsg]
        .filter(m => m.role === 'user' || m.text.trim().length > 0)
        .map(m => ({
          role: m.role as 'user' | 'assistant',
          content: m.text,
        }));

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

  return { messages, isStreaming, error, send, clear };
};

export default useChat;
