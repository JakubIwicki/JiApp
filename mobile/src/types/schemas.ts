import { z } from 'zod';

// ── Item/shared schemas ────────────────────────────────────────────────────

export const VideoItemSchema = z.object({
  videoId: z.string(),
  title: z.string(),
  description: z.string(),
  imageUrl: z.string(),
  videoUrl: z.string(),
  channelTitle: z.string(),
});

export const SearchHistoryItemSchema = z.object({
  id: z.number(),
  searchText: z.string(),
  searchedAt: z.string(),
});

export const DownloadHistoryItemSchema = z.object({
  id: z.number(),
  videoTitle: z.string(),
  videoDescription: z.string(),
  videoId: z.string(),
  videoUrl: z.string(),
  imageUrl: z.string(),
  downloadedAt: z.string(),
});

// ── Auth raw response schemas ──────────────────────────────────────────────

export const LoginApiRawSchema = z.object({
  userId: z.number(),
  displayName: z.string().nullish(),
  accessToken: z.string(),
  refreshToken: z.string(),
  expiresIn: z.number(),
  roles: z.array(z.string()).optional(),
  permissions: z.array(z.string()).optional(),
});

export const MeApiRawSchema = z.object({
  id: z.number(),
  displayName: z.string().optional(),
  username: z.string().optional(),
  email: z.string().optional(),
  roles: z.array(z.string()).optional(),
  permissions: z.array(z.string()).optional(),
});

export const UpdateProfileApiRawSchema = z.object({
  id: z.number(),
  displayName: z.string().optional(),
  username: z.string().optional(),
  email: z.string().optional(),
});

// ── YT response schemas ────────────────────────────────────────────────────

export const SearchResponseSchema = z.object({
  results: z.array(VideoItemSchema),
  hasMore: z.boolean(),
});

export const SearchHistoryResponseSchema = z.object({
  items: z.array(SearchHistoryItemSchema),
});

export const HistoryResponseSchema = z.object({
  searches: z.array(SearchHistoryItemSchema),
  downloads: z.array(DownloadHistoryItemSchema),
});

export const DownloadResponseSchema = z.object({
  downloadUrl: z.string(),
});

export const DownloadHistoryResponseSchema = z.object({
  items: z.array(DownloadHistoryItemSchema),
});
