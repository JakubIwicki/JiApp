import apiClient from '../../../services/apiClient';
import { RevenueReportSchema, ClientReportItemSchema } from '../types/api';
import type { RevenueReport, ClientReportItem } from '../types/api';

export const getRevenueReport = async (
  boardId: number,
  from: string,
  to: string,
  groupBy: string,
): Promise<RevenueReport[]> => {
  const response = await apiClient.get('/scheduler/reports/revenue', {
    params: { boardId, from, to, groupBy },
  });
  return RevenueReportSchema.array().parse(response.data);
};

export const getClientReport = async (
  boardId: number,
  sortBy: string,
): Promise<ClientReportItem[]> => {
  const response = await apiClient.get('/scheduler/reports/clients', {
    params: { boardId, sortBy },
  });
  return ClientReportItemSchema.array().parse(response.data);
};
