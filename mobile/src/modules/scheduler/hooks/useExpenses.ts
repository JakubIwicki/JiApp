import { useState, useCallback, useRef, useEffect } from 'react';
import * as expenseService from '../services/expenseService';
import type { Expense } from '../types/api';

interface UseExpensesResult {
  expenses: Expense[];
  isLoading: boolean;
  error: string | null;
  loadExpenses: (boardId: number, date: string) => Promise<void>;
  addExpense: (data: {
    boardId: number;
    date: string;
    category: string;
    amount: { amount: number; currency: string };
    note?: string;
  }) => Promise<number | undefined>;
  removeExpense: (id: number) => Promise<void>;
}

const useExpenses = (): UseExpensesResult => {
  const [expenses, setExpenses] = useState<Expense[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    const controller = abortRef.current;
    return () => {
      controller?.abort();
    };
  }, [abortRef]);

  const loadExpenses = useCallback(
    async (boardId: number, date: string) => {
      abortRef.current?.abort();
      const controller = new AbortController();
      abortRef.current = controller;

      setIsLoading(true);
      setError(null);

      try {
        const data = await expenseService.listExpenses(boardId, date);
        setExpenses(data);
      } catch (err) {
        if (err instanceof Error && err.name === 'AbortError') return;
        setError(err instanceof Error ? err.message : 'Failed to load expenses');
        setExpenses([]);
      } finally {
        setIsLoading(false);
      }
    },
    [],
  );

  const addExpense = useCallback(
    async (data: {
      boardId: number;
      date: string;
      category: string;
      amount: { amount: number; currency: string };
      note?: string;
    }): Promise<number | undefined> => {
      setError(null);
      try {
        const result = await expenseService.createExpense(data);
        return result.id;
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to create expense');
        throw err;
      }
    },
    [],
  );

  const removeExpense = useCallback(async (id: number) => {
    setError(null);
    const previous = expenses;
    setExpenses((prev) => prev.filter((e) => e.id !== id));
    try {
      await expenseService.deleteExpense(id);
    } catch (err) {
      setExpenses(previous);
      setError(err instanceof Error ? err.message : 'Failed to delete expense');
      throw err;
    }
  }, [expenses]);

  return {
    expenses,
    isLoading,
    error,
    loadExpenses,
    addExpense,
    removeExpense,
  };
};

export default useExpenses;
