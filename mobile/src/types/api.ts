import { z } from 'zod';
import type {
  VideoItemSchema,
  SearchHistoryItemSchema,
  DownloadHistoryItemSchema,
  LoginApiRawSchema,
  MeApiRawSchema,
  UpdateProfileApiRawSchema,
  SearchResponseSchema,
  HistoryResponseSchema,
  DownloadResponseSchema,
} from './schemas';

// ── Request interfaces (outbound — no schema needed) ────────────────────────

export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  displayName: string;
}

export interface UpdateProfileRequest {
  displayName: string;
  email: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface SearchRequest {
  query: string;
  maxResults?: number;
}

export interface DownloadRequest {
  videoId: string;
  videoUrl: string;
  title?: string;
  description?: string;
  imageUrl?: string;
}

// ── App-model types (derived — no schema needed) ───────────────────────────

export interface LoginResponse {
  /** Mapped from API `accessToken` field */
  token: string;
  /** Mapped from API `userId` field */
  id: number;
  displayName: string;
  /** Module ids the user is granted (e.g. ["YtDownloader","Scheduler"]). */
  modules: string[];
}

export interface ApiErrorResponse {
  error: string;
  details?: string;
  retryAfterSeconds?: string;
}

// ── Typed augmentation for server-error metadata on axios errors ───────────

/** Error augmented by the apiClient response interceptor with the server's error message. */
export interface ServerAugmentedError extends Error {
  _serverError?: string;
}

// ── Response types inferred from Zod schemas ────────────────────────────────
// These replace the former hand-written interfaces. The schema is the single
// source of truth — types stay in sync with the server contract automatically.

export type VideoItem = z.infer<typeof VideoItemSchema>;
export type SearchHistoryItem = z.infer<typeof SearchHistoryItemSchema>;
export type DownloadHistoryItem = z.infer<typeof DownloadHistoryItemSchema>;

export type LoginApiRaw = z.infer<typeof LoginApiRawSchema>;
export type MeApiRaw = z.infer<typeof MeApiRawSchema>;
export type UpdateProfileApiRaw = z.infer<typeof UpdateProfileApiRawSchema>;

export type SearchResponse = z.infer<typeof SearchResponseSchema>;
export type HistoryResponse = z.infer<typeof HistoryResponseSchema>;
export type DownloadResponse = z.infer<typeof DownloadResponseSchema>;
