import apiClient from '../../../services/apiClient';
import type { Expense } from '../types/api';

interface IdResponse {
  id: number;
}

interface CreateExpenseRequest {
  boardId: number;
  date: string;
  category: string;
  amount: { amount: number; currency: string };
  note?: string;
}

interface UpdateExpenseRequest {
  date: string;
  category: string;
  amount: { amount: number; currency: string };
  note?: string;
}

/** Backend returns flat amount+currency, not Price object. */
interface ExpenseApiResponse {
  id: number;
  boardId: number;
  date: string;
  category: string;
  amount: number;
  currency: string;
  note?: string;
}

function toExpense(raw: ExpenseApiResponse): Expense {
  return {
    id: raw.id,
    boardId: raw.boardId,
    date: raw.date,
    category: raw.category as Expense['category'],
    amount: { amount: raw.amount, currency: raw.currency },
    note: raw.note,
  };
}

export const createExpense = async (
  data: CreateExpenseRequest,
): Promise<IdResponse> => {
  const response = await apiClient.post<IdResponse>('/scheduler/expenses', data);
  return response.data;
};

export const listExpenses = async (
  boardId: number,
  date: string,
): Promise<Expense[]> => {
  const response = await apiClient.get<ExpenseApiResponse[]>(
    '/scheduler/expenses',
    { params: { boardId, date } },
  );
  return response.data.map(toExpense);
};

export const getExpense = async (id: number): Promise<Expense> => {
  const response = await apiClient.get<ExpenseApiResponse>(
    `/scheduler/expenses/${id}`,
  );
  return toExpense(response.data);
};

export const updateExpense = async (
  id: number,
  data: UpdateExpenseRequest,
): Promise<void> => {
  await apiClient.put(`/scheduler/expenses/${id}`, data);
};

export const deleteExpense = async (id: number): Promise<void> => {
  await apiClient.delete(`/scheduler/expenses/${id}`);
};
