import { createMockFn } from '../../../../test/createMockFn';
import type { CreateItemPayload, UpdateItemPayload } from '../itemService';

// ── Internal state ─────────────────────────────────────────────────────────

let _itemError: Error | null = null;
let _clearedCount = 3;
let _resetCount = 5;

// ── Mock functions ─────────────────────────────────────────────────────────

export const createItem = createMockFn(
  async (
    _boardId: number,
    _payload: CreateItemPayload,
  ): Promise<{ id: number }> => {
    if (_itemError) throw _itemError;
    return { id: 99 };
  },
);

export const updateItem = createMockFn(
  async (
    _boardId: number,
    _itemId: number,
    _payload: UpdateItemPayload,
  ): Promise<void> => {},
);

export const setItemStatus = createMockFn(
  async (
    _boardId: number,
    _itemId: number,
    _status: string,
  ): Promise<void> => {},
);

export const deleteItem = createMockFn(
  async (_boardId: number, _itemId: number): Promise<void> => {},
);

export const clearCompleted = createMockFn(
  async (_boardId: number): Promise<{ cleared: number }> => {
    if (_itemError) throw _itemError;
    return { cleared: _clearedCount };
  },
);

export const resetWeekly = createMockFn(
  async (_boardId: number): Promise<{ reset: number }> => {
    if (_itemError) throw _itemError;
    return { reset: _resetCount };
  },
);

// ── Fluent scenario builders (.withX()) ────────────────────────────────────

export function withItemError(error: Error = new Error('Mock error')): Error {
  _itemError = error;
  return error;
}

export function withClearCompleted(cleared: number): number {
  _clearedCount = cleared;
  return _clearedCount;
}

export function withResetWeekly(count: number): number {
  _resetCount = count;
  return _resetCount;
}

// ── Reset ──────────────────────────────────────────────────────────────────

export function reset(): void {
  _itemError = null;
  _clearedCount = 3;
  _resetCount = 5;

  if (typeof jest !== 'undefined') {
    createItem.mockClear();
    updateItem.mockClear();
    setItemStatus.mockClear();
    deleteItem.mockClear();
    clearCompleted.mockClear();
    resetWeekly.mockClear();
  }
}
