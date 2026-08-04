import { z } from 'zod';
import apiClient from '../../../services/apiClient';
import { ServiceItemSchema } from '../types/api';
import type { ServiceItem } from '../types/api';

const IdResponseSchema = z.object({ id: z.number() });
type IdResponse = z.infer<typeof IdResponseSchema>;

interface CreateServiceRequest {
  boardId: number;
  name: string;
  category: string;
  baseDuration: number;
  basePrice: { amount: number; currency: string };
}

interface UpdateServiceRequest {
  name: string;
  category: string;
  baseDuration: number;
  basePrice: { amount: number; currency: string };
}

export const createService = async (
  data: CreateServiceRequest,
): Promise<IdResponse> => {
  const response = await apiClient.post('/scheduler/services', data);
  return IdResponseSchema.parse(response.data);
};

export const listServices = async (
  boardId?: number,
  category?: string,
): Promise<ServiceItem[]> => {
  const params: Record<string, string | number> = {};
  if (boardId !== undefined) params.boardId = boardId;
  if (category !== undefined) params.category = category;

  const response = await apiClient.get('/scheduler/services', { params });
  return ServiceItemSchema.array().parse(response.data);
};

export const getService = async (id: number): Promise<ServiceItem> => {
  const response = await apiClient.get(`/scheduler/services/${id}`);
  return ServiceItemSchema.parse(response.data);
};

export const updateService = async (
  id: number,
  data: UpdateServiceRequest,
): Promise<void> => {
  await apiClient.put(`/scheduler/services/${id}`, data);
};

export const deleteService = async (id: number): Promise<void> => {
  await apiClient.delete(`/scheduler/services/${id}`);
};
