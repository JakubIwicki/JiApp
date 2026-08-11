jest.mock('../../../../services/apiClient', () => ({
  __esModule: true,
  default: {
    post: jest.fn(),
    get: jest.fn(),
    put: jest.fn(),
    delete: jest.fn(),
  },
}));

import apiClient from '../../../../services/apiClient';
import {
  createItem,
  updateItem,
  setItemStatus,
  deleteItem,
  clearCompleted,
  resetWeekly,
  type CreateItemPayload,
  type UpdateItemPayload,
} from '../itemService';

const mockPost = apiClient.post as jest.Mock;
const mockPut = apiClient.put as jest.Mock;
const mockDelete = apiClient.delete as jest.Mock;

const createPayload: CreateItemPayload = {
  title: 'Mleko',
  quantity: '1',
  category: 'dairy',
  note: 'from the shop',
  assigneeUserId: 5,
  expiryDate: '2026-12-31',
  isRecurring: true,
};

const updatePayload: UpdateItemPayload = { title: 'Mleko 2%' };

beforeEach(() => {
  jest.clearAllMocks();
});

// --- createItem ---

describe('createItem', () => {
  it('calls POST /lovingboards/boards/7/items with the payload and returns the id', async () => {
    mockPost.mockResolvedValue({ data: { id: 99 } });

    const result = await createItem(7, createPayload);

    expect(mockPost).toHaveBeenCalledWith(
      '/lovingboards/boards/7/items',
      createPayload,
    );
    expect(result).toEqual({ id: 99 });
  });

  it('rejects when the response violates IdResponseSchema', async () => {
    mockPost.mockResolvedValue({ data: { id: 'not-a-number' } });

    await expect(createItem(7, createPayload)).rejects.toThrow();
  });
});

// --- updateItem ---

describe('updateItem', () => {
  it('calls PUT /lovingboards/boards/7/items/9 with the payload', async () => {
    mockPut.mockResolvedValue({});

    await updateItem(7, 9, updatePayload);

    expect(mockPut).toHaveBeenCalledWith(
      '/lovingboards/boards/7/items/9',
      updatePayload,
    );
  });
  // Response is unvalidated — updateItem awaits the call and discards the body.
});

// --- setItemStatus ---

describe('setItemStatus', () => {
  it('calls PUT /lovingboards/boards/7/items/9/status with { status }', async () => {
    mockPut.mockResolvedValue({});

    await setItemStatus(7, 9, 'Completed');

    expect(mockPut).toHaveBeenCalledWith(
      '/lovingboards/boards/7/items/9/status',
      { status: 'Completed' },
    );
  });
  // Response is unvalidated — setItemStatus awaits the call and discards the body.
});

// --- deleteItem ---

describe('deleteItem', () => {
  it('calls DELETE /lovingboards/boards/7/items/9', async () => {
    mockDelete.mockResolvedValue({});

    await deleteItem(7, 9);

    expect(mockDelete).toHaveBeenCalledWith('/lovingboards/boards/7/items/9');
  });
  // Response is unvalidated — deleteItem awaits the call and discards the body.
});

// --- clearCompleted ---

describe('clearCompleted', () => {
  it('calls POST /lovingboards/boards/7/items/clear-completed and returns the cleared count', async () => {
    mockPost.mockResolvedValue({ data: { cleared: 3 } });

    const result = await clearCompleted(7);

    expect(mockPost).toHaveBeenCalledWith(
      '/lovingboards/boards/7/items/clear-completed',
    );
    expect(result).toEqual({ cleared: 3 });
  });

  it('rejects when the response violates ClearedResponseSchema', async () => {
    mockPost.mockResolvedValue({ data: { cleared: 'not-a-number' } });

    await expect(clearCompleted(7)).rejects.toThrow();
  });
});

// --- resetWeekly ---

describe('resetWeekly', () => {
  it('calls POST /lovingboards/boards/7/items/reset-weekly and returns the reset count', async () => {
    mockPost.mockResolvedValue({ data: { reset: 5 } });

    const result = await resetWeekly(7);

    expect(mockPost).toHaveBeenCalledWith(
      '/lovingboards/boards/7/items/reset-weekly',
    );
    expect(result).toEqual({ reset: 5 });
  });

  it('rejects when the response violates ResetResponseSchema', async () => {
    mockPost.mockResolvedValue({ data: { reset: 'not-a-number' } });

    await expect(resetWeekly(7)).rejects.toThrow();
  });
});
