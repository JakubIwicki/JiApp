import { z } from 'zod';
import type { VideoItem } from './api';

// ── VideoItem schema (must match the API type exactly) ──────────────────────

export const VideoItemSchema = z.object({
  videoId: z.string(),
  title: z.string(),
  description: z.string(),
  imageUrl: z.string(),
  videoUrl: z.string(),
  channelTitle: z.string(),
}) satisfies z.ZodSchema<VideoItem>;

// ── SSE event schemas ──────────────────────────────────────────────────────

export const TextDeltaEventSchema = z.object({
  text: z.string(),
});

export const ToolStepEventSchema = z.object({
  tool: z.string(),
  status: z.enum(['running', 'done']),
});

export const SearchResultsEventSchema = z.object({
  results: z.array(VideoItemSchema),
});

export const DownloadOfferEventSchema = z.object({
  videoId: z.string(),
  videoUrl: z.string(),
  title: z.string().nullable(),
  imageUrl: z.string().nullable(),
});

export const DoneEventSchema = z.object({
  reason: z.enum(['complete', 'max_iterations', 'error']),
});

// ── Inferred event types ───────────────────────────────────────────────────

export type TextDeltaEvent = z.output<typeof TextDeltaEventSchema>;
export type ToolStepEvent = z.output<typeof ToolStepEventSchema>;
export type SearchResultsEvent = z.output<typeof SearchResultsEventSchema>;
export type DownloadOfferEvent = z.output<typeof DownloadOfferEventSchema>;
export type DoneEvent = z.output<typeof DoneEventSchema>;

// ── Domain types for the hook ──────────────────────────────────────────────

export type ChatRole = 'user' | 'assistant';

export interface ToolStep {
  readonly tool: string;
  readonly status: 'running' | 'done';
}

export interface DownloadOfferData {
  readonly videoId: string;
  readonly videoUrl: string;
  readonly title: string | null;
  readonly imageUrl: string | null;
}

export interface ChatMessage {
  readonly id: string;
  readonly role: ChatRole;
  readonly text: string;
  readonly videos?: VideoItem[];
  readonly offer?: DownloadOfferData;
  readonly toolSteps?: ToolStep[];
  readonly pending?: boolean;
}
