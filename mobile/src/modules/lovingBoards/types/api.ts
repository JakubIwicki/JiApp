import { z } from 'zod';

// ── Schemas ──────────────────────────────────────────────────────────────────

export const BoardItemStatusSchema = z.enum(['Needed', 'Completed', 'Removed']);

export const ItemSchema = z.object({
  id: z.number(),
  boardId: z.number(),
  title: z.string(),
  quantity: z.string().nullable(),
  category: z.string().nullable(),
  note: z.string().nullable(),
  assigneeUserId: z.number().nullable(),
  expiryDate: z.string().nullable(),
  isRecurring: z.boolean(),
  status: BoardItemStatusSchema,
  addedByUserId: z.number(),
  completedByUserId: z.number().nullable(),
  createdAt: z.string(),
  updatedAt: z.string(),
  removedAt: z.string().nullable(),
});

export const BoardSchema = z.object({
  id: z.number(),
  name: z.string(),
  ownerUserId: z.number(),
  memberUserIds: z.array(z.number()),
  createdAt: z.string(),
  items: z.array(ItemSchema),
});

export const ListBoardsResponseSchema = z.object({
  boards: z.array(BoardSchema),
  hasMore: z.boolean(),
});

// ── Inferred types ───────────────────────────────────────────────────────────

export type BoardItemStatus = z.infer<typeof BoardItemStatusSchema>;
export type Item = z.infer<typeof ItemSchema>;
export type Board = z.infer<typeof BoardSchema>;
export type ListBoardsResponse = z.infer<typeof ListBoardsResponseSchema>;
