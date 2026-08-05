import { createMockFn } from '../../../../test/createMockFn';
import { getThisWeekend } from '../../../../test/dateUtils';
import type { Expense } from '../../types/api';

// ── Default stub data ──────────────────────────────────────────────────────

const baseExpenses: Omit<Expense, 'date'>[] = [
  {
    id: 1,
    boardId: 1,
    category: 'Fuel',
    amount: { amount: 120, currency: 'PLN' },
    note: 'Paliwo dojazd do salonu Warszawa-Krakow',
  },
  {
    id: 2,
    boardId: 1,
    category: 'Food',
    amount: { amount: 45, currency: 'PLN' },
    note: 'Obiad w miedzymiescie',
  },
  {
    id: 3,
    boardId: 1,
    category: 'Supplies',
    amount: { amount: 89, currency: 'PLN' },
    note: null,
  },
];

const { saturday, sunday } = getThisWeekend();
const defaultExpenses: Expense[] = baseExpenses.map(e => ({
  ...e,
  // First 2 expenses go on Saturday, the last on Sunday
  date: e.id <= 2 ? saturday : sunday,
}));

// ── Internal state ─────────────────────────────────────────────────────────

let _expenses: Expense[] = defaultExpenses;
let _expenseError: Error | null = null;

// ── Mock functions ─────────────────────────────────────────────────────────

export const listExpenses = createMockFn(
  async (_boardId: number, date: string): Promise<Expense[]> => {
    if (_expenseError) throw _expenseError;
    return _expenses.filter(e => e.date === date);
  },
);

export const getExpense = createMockFn(async (id: number): Promise<Expense> => {
  if (_expenseError) throw _expenseError;
  const expense = _expenses.find(e => e.id === id);
  if (!expense) throw new Error('Expense not found');
  return expense;
});

export const createExpense = createMockFn(async (): Promise<{ id: number }> => {
  return { id: 99 };
});

export const updateExpense = createMockFn(async (): Promise<void> => {});

export const deleteExpense = createMockFn(
  async (_id: number): Promise<void> => {},
);

// ── Fluent scenario builders (.withX()) ────────────────────────────────────

export function withExpenses(expenses: Expense[] = defaultExpenses): Expense[] {
  _expenseError = null;
  _expenses = expenses;
  return _expenses;
}

export function withExpenseError(
  error: Error = new Error('Mock error'),
): Error {
  _expenseError = error;
  return error;
}

// ── Reset ──────────────────────────────────────────────────────────────────

export function reset(): void {
  _expenses = defaultExpenses;
  _expenseError = null;

  if (typeof jest !== 'undefined') {
    listExpenses.mockClear();
    getExpense.mockClear();
    createExpense.mockClear();
    updateExpense.mockClear();
    deleteExpense.mockClear();
  }
}
