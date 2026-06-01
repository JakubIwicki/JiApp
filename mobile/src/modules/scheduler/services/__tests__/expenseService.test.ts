import apiClient from '../../../../services/apiClient';
import {
  createExpense,
  listExpenses,
  getExpense,
  updateExpense,
  deleteExpense,
} from '../expenseService';

jest.mock('../../../../services/apiClient', () => ({
  post: jest.fn(),
  get: jest.fn(),
  put: jest.fn(),
  delete: jest.fn(),
}));

const mockPost = apiClient.post as jest.Mock;
const mockGet = apiClient.get as jest.Mock;
const mockPut = apiClient.put as jest.Mock;
const mockDelete = apiClient.delete as jest.Mock;

describe('expenseService', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('createExpense', () => {
    it('posts to /scheduler/expenses', async () => {
      mockPost.mockResolvedValue({ data: { id: 7 } });

      const data = { boardId: 1, date: '2026-05-23', category: 'Fuel', amount: { amount: 50, currency: 'PLN' }, note: 'Gas' };
      const result = await createExpense(data);
      expect(mockPost).toHaveBeenCalledWith('/scheduler/expenses', data);
      expect(result).toEqual({ id: 7 });
    });
  });

  describe('listExpenses', () => {
    it('calls GET with boardId and date', async () => {
      mockGet.mockResolvedValue({ data: [] });

      await listExpenses(1, '2026-05-23');
      expect(mockGet).toHaveBeenCalledWith('/scheduler/expenses', { params: { boardId: 1, date: '2026-05-23' } });
    });
  });

  describe('getExpense', () => {
    it('calls GET with id and transforms response', async () => {
      const apiResponse = { id: 1, boardId: 1, date: '2026-05-23', category: 'Fuel', amount: 50, currency: 'PLN', note: 'Gas' };
      mockGet.mockResolvedValue({ data: apiResponse });

      const result = await getExpense(1);
      expect(mockGet).toHaveBeenCalledWith('/scheduler/expenses/1');
      expect(result).toEqual({
        id: 1,
        boardId: 1,
        date: '2026-05-23',
        category: 'Fuel',
        amount: { amount: 50, currency: 'PLN' },
        note: 'Gas',
      });
    });
  });

  describe('updateExpense', () => {
    it('puts to /scheduler/expenses/:id', async () => {
      mockPut.mockResolvedValue({ data: { id: 1 } });

      const data = { date: '2026-05-23', category: 'Supplies', amount: { amount: 30, currency: 'PLN' }, note: '' };
      await updateExpense(1, data);
      expect(mockPut).toHaveBeenCalledWith('/scheduler/expenses/1', data);
    });
  });

  describe('deleteExpense', () => {
    it('deletes expense by id', async () => {
      mockDelete.mockResolvedValue({});

      await deleteExpense(1);
      expect(mockDelete).toHaveBeenCalledWith('/scheduler/expenses/1');
    });
  });
});
