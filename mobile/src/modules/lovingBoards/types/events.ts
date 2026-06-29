import { z } from 'zod';

// ── SSE Event Schemas ──────────────────────────────────────────────────────

export const PresenceEventSchema = z.object({
  userIds: z.array(z.number()),
});

// ── Inferred types ─────────────────────────────────────────────────────────

export type PresenceEvent = z.infer<typeof PresenceEventSchema>;
