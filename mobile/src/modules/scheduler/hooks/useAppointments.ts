import { useState, useCallback, useRef, useEffect } from 'react';
import * as appointmentService from '../services/appointmentService';
import type { Appointment, AppointmentStatus } from '../types/api';
import type { CreateAppointmentData } from '../services/appointmentService';

interface UseAppointmentsResult {
  appointments: Appointment[];
  isLoading: boolean;
  error: string | null;
  loadAppointments: (boardId: number, dates: string[]) => Promise<void>;
  addAppointment: (data: CreateAppointmentData) => Promise<number | undefined>;
  removeAppointment: (id: number) => Promise<void>;
  markDone: (id: number) => Promise<void>;
  markCancelled: (id: number) => Promise<void>;
}

const useAppointments = (): UseAppointmentsResult => {
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    const controller = abortRef.current;
    return () => {
      controller?.abort();
    };
  }, [abortRef]);

  const loadAppointments = useCallback(
    async (boardId: number, dates: string[]) => {
      abortRef.current?.abort();
      const controller = new AbortController();
      abortRef.current = controller;

      setIsLoading(true);
      setError(null);

      try {
        const data = await appointmentService.listAppointments(boardId, dates);
        setAppointments(data);
      } catch (err) {
        if (err instanceof Error && err.name === 'AbortError') return;
        setError(err instanceof Error ? err.message : 'Failed to load appointments');
        setAppointments([]);
      } finally {
        setIsLoading(false);
      }
    },
    [],
  );

  const addAppointment = useCallback(
    async (data: CreateAppointmentData): Promise<number | undefined> => {
      setError(null);
      try {
        const result = await appointmentService.createAppointment(data);
        return result.id;
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to create appointment');
        throw err;
      }
    },
    [],
  );

  const removeAppointment = useCallback(async (id: number) => {
    setError(null);
    const previous = appointments;
    setAppointments((prev) => prev.filter((a) => a.id !== id));
    try {
      await appointmentService.deleteAppointment(id);
    } catch (err) {
      setAppointments(previous);
      setError(err instanceof Error ? err.message : 'Failed to delete appointment');
      throw err;
    }
  }, [appointments]);

  const updateStatus = useCallback(
    async (id: number, status: AppointmentStatus) => {
      setError(null);
      try {
        await appointmentService.updateStatus(id, status);
        setAppointments((prev) =>
          prev.map((a) => (a.id === id ? { ...a, status } : a)),
        );
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to update status');
        throw err;
      }
    },
    [],
  );

  const markDone = useCallback(
    async (id: number) => updateStatus(id, 'Done'),
    [updateStatus],
  );

  const markCancelled = useCallback(
    async (id: number) => updateStatus(id, 'Cancelled'),
    [updateStatus],
  );

  return {
    appointments,
    isLoading,
    error,
    loadAppointments,
    addAppointment,
    removeAppointment,
    markDone,
    markCancelled,
  };
};

export default useAppointments;
