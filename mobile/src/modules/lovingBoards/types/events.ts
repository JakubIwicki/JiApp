import { z } from 'zod';
import { BoardItemStatusSchema } from './api';

// ── SSE Event Schemas ──────────────────────────────────────────────────────

export const PresenceEventSchema = z.object({
  userIds: z.array(z.number()),
});

export const ItemAddedEventSchema = z.object({
  type: z.literal('item.added'),
  itemId: z.number(),
});

export const ItemUpdatedEventSchema = z.object({
  type: z.literal('item.updated'),
  itemId: z.number(),
});

export const ItemStatusEventSchema = z.object({
  type: z.literal('item.status'),
  itemId: z.number(),
  status: BoardItemStatusSchema,
});

export const ItemRemovedEventSchema = z.object({
  type: z.literal('item.removed'),
  itemId: z.number(),
});

export const ItemsClearedEventSchema = z.object({
  type: z.literal('items.cleared'),
  itemIds: z.array(z.number()),
});

export const BoardUpdatedEventSchema = z.object({
  type: z.literal('board.updated'),
  boardId: z.number(),
});

export const MemberChangedEventSchema = z.object({
  type: z.literal('member.changed'),
  boardId: z.number(),
});

export const RecurringResetEventSchema = z.object({
  type: z.literal('recurring.reset'),
  reset: z.number(),
});

export const BoardDeletedEventSchema = z.object({
  type: z.literal('board.deleted'),
  boardId: z.number(),
});

// ── Discriminated event union ──────────────────────────────────────────────

export const BoardStreamEventSchema = z.discriminatedUnion('type', [
  ItemAddedEventSchema,
  ItemUpdatedEventSchema,
  ItemStatusEventSchema,
  ItemRemovedEventSchema,
  ItemsClearedEventSchema,
  BoardUpdatedEventSchema,
  MemberChangedEventSchema,
  RecurringResetEventSchema,
  BoardDeletedEventSchema,
]);

// ── Inferred types ─────────────────────────────────────────────────────────

export type PresenceEvent = z.infer<typeof PresenceEventSchema>;
export type BoardStreamEvent = z.infer<typeof BoardStreamEventSchema>;
