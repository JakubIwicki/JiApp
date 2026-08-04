import { z } from 'zod';
import apiClient from '../../../services/apiClient';
import { AppointmentSchema } from '../types/api';
import type { Appointment, AppointmentStatus } from '../types/api';

export interface CreateAppointmentData {
  boardId: number;
  clientId: number;
  serviceId: number;
  date: string;
  startTime: string;
  endTime: string;
  description?: string;
  location: string;
  price: { amount: number; currency: string };
}

export interface UpdateAppointmentData {
  clientId: number;
  serviceId: number;
  date: string;
  startTime: string;
  endTime: string;
  description?: string;
  location: string;
  price: { amount: number; currency: string };
}

const IdResponseSchema = z.object({ id: z.number() });
type IdResponse = z.infer<typeof IdResponseSchema>;

export const createAppointment = async (
  data: CreateAppointmentData,
): Promise<IdResponse> => {
  const response = await apiClient.post('/scheduler/appointments', data);
  return IdResponseSchema.parse(response.data);
};

export const listAppointments = async (
  boardId: number,
  dates: string[],
): Promise<Appointment[]> => {
  const response = await apiClient.get('/scheduler/appointments', {
    params: { boardId, date: dates },
  });
  return AppointmentSchema.array().parse(response.data);
};

export const getAppointment = async (id: number): Promise<Appointment> => {
  const response = await apiClient.get(`/scheduler/appointments/${id}`);
  return AppointmentSchema.parse(response.data);
};

export const updateAppointment = async (
  id: number,
  data: UpdateAppointmentData,
): Promise<void> => {
  await apiClient.put(`/scheduler/appointments/${id}`, data);
};

export const updateStatus = async (
  id: number,
  status: AppointmentStatus,
): Promise<void> => {
  await apiClient.patch(`/scheduler/appointments/${id}/status`, { status });
};

export const deleteAppointment = async (id: number): Promise<void> => {
  await apiClient.delete(`/scheduler/appointments/${id}`);
};
