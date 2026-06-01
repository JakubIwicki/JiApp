import { renderHook, act } from '@testing-library/react-native';
import useExpenses from '../useExpenses';
import * as expenseService from '../../services/expenseService';
import type { Expense } from '../../types/api';

jest.mock('../../services/expenseService', () => ({
  listExpenses: jest.fn(),
  createExpense: jest.fn(),
  deleteExpense: jest.fn(),
}));

const mockListExpenses = expenseService.listExpenses as jest.Mock;
const mockCreateExpense = expenseService.createExpense as jest.Mock;
const mockDeleteExpense = expenseService.deleteExpense as jest.Mock;

const mockExpense: Expense = {
  id: 1,
  boardId: 1,
  date: '2026-05-23',
  category: 'Fuel',
  amount: { amount: 50, currency: 'PLN' },
  note: 'Gas station',
};

describe('useExpenses', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('has initial state', () => {
    const { result } = renderHook(() => useExpenses());
    expect(result.current.expenses).toEqual([]);
    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it('loads expenses for board and date', async () => {
    mockListExpenses.mockResolvedValue([mockExpense]);

    const { result } = renderHook(() => useExpenses());

    await act(async () => {
      await result.current.loadExpenses(1, '2026-05-23');
    });

    expect(mockListExpenses).toHaveBeenCalledWith(1, '2026-05-23');
    expect(result.current.expenses).toEqual([mockExpense]);
    expect(result.current.isLoading).toBe(false);
  });

  it('handles load error', async () => {
    mockListExpenses.mockRejectedValue(new Error('Load error'));

    const { result } = renderHook(() => useExpenses());

    await act(async () => {
      await result.current.loadExpenses(1, '2026-05-23');
    });

    expect(result.current.error).toBe('Load error');
  });

  it('adds an expense', async () => {
    mockCreateExpense.mockResolvedValue({ id: 7 });

    const { result } = renderHook(() => useExpenses());

    const data = {
      boardId: 1,
      date: '2026-05-23',
      category: 'Supplies',
      amount: { amount: 30, currency: 'PLN' },
      note: '',
    };

    await act(async () => {
      await result.current.addExpense(data);
    });

    expect(mockCreateExpense).toHaveBeenCalledWith(data);
  });

  it('removes an expense', async () => {
    mockDeleteExpense.mockResolvedValue(undefined);

    const { result } = renderHook(() => useExpenses());

    await act(async () => {
      await result.current.removeExpense(1);
    });

    expect(mockDeleteExpense).toHaveBeenCalledWith(1);
  });
});
