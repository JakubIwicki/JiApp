import { useState, useCallback, useEffect } from 'react';
import useAppointments from './useAppointments';
import useExpenses from './useExpenses';
import { getWeekendDates } from '../utils/weekendUtils';
import type { DayTotal } from '../types/api';

interface UseWeekendGridResult {
  saturday: string;
  sunday: string;
  weekLabel: string;
  appointments: ReturnType<typeof useAppointments>;
  expenses: ReturnType<typeof useExpenses>;
  dayTotals: { saturday: DayTotal; sunday: DayTotal };
  goToPreviousWeekend: () => void;
  goToNextWeekend: () => void;
  goToToday: () => void;
  refresh: () => Promise<void>;
  isLoading: boolean;
}

function computeDayTotal(
  appointments: Array<{ date: string; price: { amount: number }; status: string }>,
  expenses: Array<{ date: string; amount: { amount: number } }>,
  date: string,
): DayTotal {
  const revenue = appointments
    .filter((a) => a.date === date && a.status !== 'Cancelled')
    .reduce((sum, a) => sum + a.price.amount, 0);

  const dayExpenses = expenses
    .filter((e) => e.date === date)
    .reduce((sum, e) => sum + e.amount.amount, 0);

  return {
    revenue,
    expenses: dayExpenses,
    net: revenue - dayExpenses,
  };
}

const useWeekendGrid = (boardId: number): UseWeekendGridResult => {
  const [referenceDate, setReferenceDate] = useState(() => new Date());
  const appointments = useAppointments();
  const expenses = useExpenses();

  const { saturday, sunday } = getWeekendDates(referenceDate);
  const weekLabel = `${saturday} / ${sunday}`;

  const loadData = useCallback(async () => {
    await Promise.all([
      appointments.loadAppointments(boardId, [saturday, sunday]),
      expenses.loadExpenses(boardId, saturday),
    ]);
    // Also load Sunday expenses
    await expenses.loadExpenses(boardId, sunday);
  }, [boardId, saturday, sunday, appointments.loadAppointments, expenses.loadExpenses]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const isLoading = appointments.isLoading || expenses.isLoading;

  const goToPreviousWeekend = useCallback(() => {
    setReferenceDate((prev) => {
      const d = new Date(prev);
      d.setDate(d.getDate() - 7);
      return d;
    });
  }, []);

  const goToNextWeekend = useCallback(() => {
    setReferenceDate((prev) => {
      const d = new Date(prev);
      d.setDate(d.getDate() + 7);
      return d;
    });
  }, []);

  const goToToday = useCallback(() => {
    setReferenceDate(new Date());
  }, []);

  const refresh = useCallback(async () => {
    await loadData();
  }, [loadData]);

  const dayTotals = {
    saturday: computeDayTotal(appointments.appointments, expenses.expenses, saturday),
    sunday: computeDayTotal(appointments.appointments, expenses.expenses, sunday),
  };

  return {
    saturday,
    sunday,
    weekLabel,
    appointments,
    expenses,
    dayTotals,
    goToPreviousWeekend,
    goToNextWeekend,
    goToToday,
    refresh,
    isLoading,
  };
};

export default useWeekendGrid;
