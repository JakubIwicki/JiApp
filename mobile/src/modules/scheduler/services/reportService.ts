import apiClient from '../../../services/apiClient';
import type { RevenueReport, ClientReportItem } from '../types/api';

export const getRevenueReport = async (
  boardId: number,
  from: string,
  to: string,
  groupBy: string,
): Promise<RevenueReport[]> => {
  const response = await apiClient.get<RevenueReport[]>('/scheduler/reports/revenue', {
    params: { boardId, from, to, groupBy },
  });
  return response.data;
};

export const getClientReport = async (
  boardId: number,
  sortBy: string,
): Promise<ClientReportItem[]> => {
  const response = await apiClient.get<ClientReportItem[]>(
    '/scheduler/reports/clients',
    { params: { boardId, sortBy } },
  );
  return response.data;
};
