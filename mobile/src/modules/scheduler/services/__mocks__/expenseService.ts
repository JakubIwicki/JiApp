import type { Expense } from '../../types/api';

type Mode = 'success' | 'empty' | 'error';

let _mode: Mode = 'success';

export const setExpenseMode = (mode: Mode) => {
  _mode = mode;
};

function getThisWeekend(): { saturday: string; sunday: string } {
  const now = new Date();
  const dayOfWeek = now.getDay();
  const daysUntilSaturday = dayOfWeek === 6 ? 0 : (6 - dayOfWeek + 7) % 7;
  const saturday = new Date(now);
  saturday.setDate(now.getDate() + daysUntilSaturday);

  const sunday = new Date(saturday);
  sunday.setDate(saturday.getDate() + 1);

  const fmt = (d: Date) => {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  };

  return { saturday: fmt(saturday), sunday: fmt(sunday) };
}

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
    note: undefined,
  },
];

export const listExpenses = async (
  _boardId: number,
  date: string,
): Promise<Expense[]> => {
  if (_mode === 'error') throw new Error('Mock error');
  if (_mode === 'empty') return [];

  const { saturday, sunday } = getThisWeekend();

  return baseExpenses
    .filter((e) => {
      // First 2 expenses on Saturday, last on Sunday
      const expenseDate = e.id <= 2 ? saturday : sunday;
      return expenseDate === date;
    })
    .map((e) => {
      const expenseDate = e.id <= 2 ? saturday : sunday;
      return { ...e, date: expenseDate };
    });
};

export const getExpense = async (id: number): Promise<Expense> => {
  if (_mode === 'error') throw new Error('Mock error');
  const { saturday, sunday } = getThisWeekend();
  const base = baseExpenses.find((e) => e.id === id);
  if (!base) throw new Error('Expense not found');
  return {
    ...base,
    date: base.id <= 2 ? saturday : sunday,
  };
};

export const createExpense = async (): Promise<{ id: number }> => {
  return { id: 99 };
};

export const updateExpense = async (): Promise<void> => {};

export const deleteExpense = async (_id: number): Promise<void> => {};
