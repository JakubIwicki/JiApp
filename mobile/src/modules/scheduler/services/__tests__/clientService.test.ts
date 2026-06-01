import apiClient from '../../../../services/apiClient';
import {
  createClient,
  listClients,
  getClient,
  updateClient,
  deleteClient,
} from '../clientService';

jest.mock('../../../../services/apiClient', () => ({
  post: jest.fn(),
  get: jest.fn(),
  put: jest.fn(),
  delete: jest.fn(),
}));

const mockPost = apiClient.post as jest.Mock;
const mockGet = apiClient.get as jest.Mock;
const mockPut = apiClient.put as jest.Mock;
const mockDelete = apiClient.delete as jest.Mock;

describe('clientService', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('createClient', () => {
    it('posts to /scheduler/clients with boardId and returns id', async () => {
      mockPost.mockResolvedValue({ data: { id: 5 } });

      const result = await createClient(1, { name: 'John', phone: '123', notes: 'VIP' });
      expect(mockPost).toHaveBeenCalledWith('/scheduler/clients', { name: 'John', phone: '123', notes: 'VIP', boardId: 1 });
      expect(result).toEqual({ id: 5 });
    });
  });

  describe('listClients', () => {
    it('calls GET with boardId and search query', async () => {
      mockGet.mockResolvedValue({ data: [{ id: 1, boardId: 1, name: 'Jane', phone: '456' }] });

      const result = await listClients(1, 'Jane');
      expect(mockGet).toHaveBeenCalledWith('/scheduler/clients', { params: { boardId: 1, q: 'Jane' } });
      expect(result).toEqual([{ id: 1, boardId: 1, name: 'Jane', phone: '456' }]);
    });

    it('calls GET with boardId without query when not provided', async () => {
      mockGet.mockResolvedValue({ data: [] });

      await listClients(1);
      expect(mockGet).toHaveBeenCalledWith('/scheduler/clients', { params: { boardId: 1 } });
    });
  });

  describe('getClient', () => {
    it('calls GET with id', async () => {
      const mockResponse = {
        id: 1, name: 'Jane', phone: '456', notes: '',
        appointments: [{ id: 10, date: '2026-05-23', startTime: '10:00', endTime: '10:30', serviceName: 'Cut', status: 'Done' }],
      };
      mockGet.mockResolvedValue({ data: mockResponse });

      const result = await getClient(1);
      expect(mockGet).toHaveBeenCalledWith('/scheduler/clients/1');
      expect(result).toEqual(mockResponse);
    });
  });

  describe('updateClient', () => {
    it('puts to /scheduler/clients/:id', async () => {
      mockPut.mockResolvedValue({});

      await updateClient(1, { name: 'Jane Updated', phone: '789', notes: '' });
      expect(mockPut).toHaveBeenCalledWith('/scheduler/clients/1', { name: 'Jane Updated', phone: '789', notes: '' });
    });
  });

  describe('deleteClient', () => {
    it('deletes client by id', async () => {
      mockDelete.mockResolvedValue({});

      await deleteClient(1);
      expect(mockDelete).toHaveBeenCalledWith('/scheduler/clients/1');
    });
  });
});
