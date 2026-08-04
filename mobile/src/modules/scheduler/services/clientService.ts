import { z } from 'zod';
import apiClient from '../../../services/apiClient';
import { ClientSchema } from '../types/api';
import type { Client } from '../types/api';

const IdResponseSchema = z.object({ id: z.number() });
type IdResponse = z.infer<typeof IdResponseSchema>;

interface CreateClientRequest {
  boardId: number;
  name: string;
  phone?: string;
  notes?: string;
}

interface UpdateClientRequest {
  name: string;
  phone?: string;
  notes?: string;
}

const ClientAppointmentSummarySchema = z.object({
  id: z.number(),
  date: z.string(),
  startTime: z.string(),
  endTime: z.string(),
  serviceName: z.string(),
  status: z.string(),
});

const ClientWithAppointmentsSchema = z.object({
  id: z.number(),
  name: z.string(),
  phone: z.string().nullable(),
  notes: z.string().nullable(),
  appointments: z.array(ClientAppointmentSummarySchema),
});

export type ClientWithAppointments = z.infer<
  typeof ClientWithAppointmentsSchema
>;

export const createClient = async (
  boardId: number,
  data: Omit<CreateClientRequest, 'boardId'>,
): Promise<IdResponse> => {
  const response = await apiClient.post('/scheduler/clients', {
    ...data,
    boardId,
  });
  return IdResponseSchema.parse(response.data);
};

export const listClients = async (
  boardId: number,
  q?: string,
): Promise<Client[]> => {
  const response = await apiClient.get('/scheduler/clients', {
    params: { boardId, ...(q ? { q } : {}) },
  });
  return ClientSchema.array().parse(response.data);
};

export const getClient = async (
  id: number,
): Promise<ClientWithAppointments> => {
  const response = await apiClient.get(`/scheduler/clients/${id}`);
  return ClientWithAppointmentsSchema.parse(response.data);
};

export const updateClient = async (
  id: number,
  data: UpdateClientRequest,
): Promise<void> => {
  await apiClient.put(`/scheduler/clients/${id}`, data);
};

export const deleteClient = async (id: number): Promise<void> => {
  await apiClient.delete(`/scheduler/clients/${id}`);
};
