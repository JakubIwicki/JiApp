import apiClient from '../../../../services/apiClient';
import {
  getRevenueReport,
  getClientReport,
} from '../reportService';

jest.mock('../../../../services/apiClient', () => ({
  get: jest.fn(),
}));

const mockGet = apiClient.get as jest.Mock;

describe('reportService', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('getRevenueReport', () => {
    it('calls GET with boardId, from, to, groupBy', async () => {
      const mockData = [
        { groupKey: '2026-05-23', revenue: 500, expenses: 100, net: 400, appointmentCount: 3 },
      ];
      mockGet.mockResolvedValue({ data: mockData });

      const result = await getRevenueReport(1, '2026-05-01', '2026-05-31', 'weekend');
      expect(mockGet).toHaveBeenCalledWith('/scheduler/reports/revenue', {
        params: { boardId: 1, from: '2026-05-01', to: '2026-05-31', groupBy: 'weekend' },
      });
      expect(result).toEqual(mockData);
    });
  });

  describe('getClientReport', () => {
    it('calls GET with boardId and sortBy', async () => {
      const mockData = [
        { client: { id: 1, name: 'Jane' }, visitCount: 5, totalSpent: 400, lastVisit: '2026-05-23', averagePerVisit: 80 },
      ];
      mockGet.mockResolvedValue({ data: mockData });

      const result = await getClientReport(1, 'visitCount');
      expect(mockGet).toHaveBeenCalledWith('/scheduler/reports/clients', {
        params: { boardId: 1, sortBy: 'visitCount' },
      });
      expect(result).toEqual(mockData);
    });
  });
});
