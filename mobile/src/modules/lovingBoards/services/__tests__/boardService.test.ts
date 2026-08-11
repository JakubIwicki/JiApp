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
  listBoards,
  getBoard,
  createBoard,
  updateBoard,
  deleteBoard,
  addMember,
  removeMember,
} from '../boardService';
import type { Board, ListBoardsResponse } from '../../types/api';

const mockGet = apiClient.get as jest.Mock;
const mockPost = apiClient.post as jest.Mock;
const mockPut = apiClient.put as jest.Mock;
const mockDelete = apiClient.delete as jest.Mock;

const makeBoard = (id: number, name: string): Board => ({
  id,
  name,
  ownerUserId: 1,
  memberUserIds: [1, 2],
  createdAt: '2026-01-01T00:00:00.000Z',
  items: [],
});

const makeListResponse = (
  boards: Board[],
  hasMore = false,
): ListBoardsResponse => ({ boards, hasMore });

beforeEach(() => {
  jest.clearAllMocks();
});

// --- listBoards ---

describe('listBoards', () => {
  const boards = [makeBoard(1, 'Dom'), makeBoard(2, 'Letnisko')];

  it('calls GET /lovingboards/boards and returns the validated response', async () => {
    mockGet.mockResolvedValue({ data: makeListResponse(boards, true) });

    const result = await listBoards();

    expect(mockGet).toHaveBeenCalledWith('/lovingboards/boards');
    expect(result).toEqual(makeListResponse(boards, true));
    expect(result.hasMore).toBe(true);
  });

  it('rejects when the response violates ListBoardsResponseSchema', async () => {
    mockGet.mockResolvedValue({
      data: { boards: [], hasMore: 'not-a-boolean' },
    });

    await expect(listBoards()).rejects.toThrow();
  });
});

// --- getBoard ---

describe('getBoard', () => {
  it('calls GET /lovingboards/boards/7 and returns the validated board', async () => {
    const board = makeBoard(7, 'Dom');
    mockGet.mockResolvedValue({ data: board });

    const result = await getBoard(7);

    expect(mockGet).toHaveBeenCalledWith('/lovingboards/boards/7');
    expect(result).toEqual(board);
  });

  it('rejects when the response violates BoardSchema', async () => {
    mockGet.mockResolvedValue({
      data: { ...makeBoard(7, 'Dom'), memberUserIds: 'not-an-array' },
    });

    await expect(getBoard(7)).rejects.toThrow();
  });
});

// --- createBoard ---

describe('createBoard', () => {
  it('calls POST /lovingboards/boards with { name } and returns the id', async () => {
    mockPost.mockResolvedValue({ data: { id: 5 } });

    const result = await createBoard('Dom');

    expect(mockPost).toHaveBeenCalledWith('/lovingboards/boards', {
      name: 'Dom',
    });
    expect(result).toEqual({ id: 5 });
  });

  it('rejects when the response violates IdResponseSchema', async () => {
    mockPost.mockResolvedValue({ data: { id: 'not-a-number' } });

    await expect(createBoard('Dom')).rejects.toThrow();
  });
});

// --- updateBoard ---

describe('updateBoard', () => {
  it('calls PUT /lovingboards/boards/7 with the new name', async () => {
    mockPut.mockResolvedValue({});

    await updateBoard(7, 'Dom 2');

    expect(mockPut).toHaveBeenCalledWith('/lovingboards/boards/7', {
      name: 'Dom 2',
    });
  });
  // Response is unvalidated — updateBoard awaits the call and discards the body.
});

// --- deleteBoard ---

describe('deleteBoard', () => {
  it('calls DELETE /lovingboards/boards/7', async () => {
    mockDelete.mockResolvedValue({});

    await deleteBoard(7);

    expect(mockDelete).toHaveBeenCalledWith('/lovingboards/boards/7');
  });
  // Response is unvalidated — deleteBoard awaits the call and discards the body.
});

// --- addMember ---

describe('addMember', () => {
  it('calls POST /lovingboards/boards/7/members with { userId }', async () => {
    mockPost.mockResolvedValue({});

    await addMember(7, 3);

    expect(mockPost).toHaveBeenCalledWith('/lovingboards/boards/7/members', {
      userId: 3,
    });
  });
  // Response is unvalidated — addMember awaits the call and discards the body.
});

// --- removeMember ---

describe('removeMember', () => {
  it('calls DELETE /lovingboards/boards/7/members/3', async () => {
    mockDelete.mockResolvedValue({});

    await removeMember(7, 3);

    expect(mockDelete).toHaveBeenCalledWith('/lovingboards/boards/7/members/3');
  });
  // Response is unvalidated — removeMember awaits the call and discards the body.
});
