import apiClient from '../../../services/apiClient';
import type { Client } from '../types/api';

interface IdResponse {
  id: number;
}

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

export interface ClientWithAppointments extends Client {
  appointments: Array<{
    id: number;
    date: string;
    startTime: string;
    endTime: string;
    serviceName: string;
    status: string;
  }>;
}

export const createClient = async (
  boardId: number,
  data: Omit<CreateClientRequest, 'boardId'>,
): Promise<IdResponse> => {
  const response = await apiClient.post<IdResponse>('/scheduler/clients', { ...data, boardId });
  return response.data;
};

export const listClients = async (boardId: number, q?: string): Promise<Client[]> => {
  const response = await apiClient.get<Client[]>('/scheduler/clients', {
    params: { boardId, ...(q ? { q } : {}) },
  });
  return response.data;
};

export const getClient = async (id: number): Promise<ClientWithAppointments> => {
  const response = await apiClient.get<ClientWithAppointments>(
    `/scheduler/clients/${id}`,
  );
  return response.data;
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
