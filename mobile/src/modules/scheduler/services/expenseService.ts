import { z } from 'zod';
import apiClient from '../../../services/apiClient';
import { ExpenseSchema } from '../types/api';
import type { Expense } from '../types/api';

const IdResponseSchema = z.object({ id: z.number() });
type IdResponse = z.infer<typeof IdResponseSchema>;

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

export const createExpense = async (
  data: CreateExpenseRequest,
): Promise<IdResponse> => {
  const response = await apiClient.post('/scheduler/expenses', data);
  return IdResponseSchema.parse(response.data);
};

export const listExpenses = async (
  boardId: number,
  date: string,
): Promise<Expense[]> => {
  const response = await apiClient.get('/scheduler/expenses', {
    params: { boardId, date },
  });
  return ExpenseSchema.array().parse(response.data);
};

export const getExpense = async (id: number): Promise<Expense> => {
  const response = await apiClient.get(`/scheduler/expenses/${id}`);
  return ExpenseSchema.parse(response.data);
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
