import apiClient from '../../../../services/apiClient';
import {
  createService,
  listServices,
  getService,
  updateService,
  deleteService,
} from '../serviceCatalogService';

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

describe('serviceCatalogService', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('createService', () => {
    it('posts to /scheduler/services', async () => {
      mockPost.mockResolvedValue({ data: { id: 10 } });

      const data = { boardId: 1, name: 'Haircut', category: 'MensHaircut', baseDuration: 30, basePrice: { amount: 60, currency: 'PLN' } };
      const result = await createService(data);
      expect(mockPost).toHaveBeenCalledWith('/scheduler/services', data);
      expect(result).toEqual({ id: 10 });
    });
  });

  describe('listServices', () => {
    it('calls GET with boardId filter', async () => {
      mockGet.mockResolvedValue({ data: [] });

      await listServices(1);
      expect(mockGet).toHaveBeenCalledWith('/scheduler/services', { params: { boardId: 1 } });
    });

    it('calls GET with boardId and category', async () => {
      mockGet.mockResolvedValue({ data: [] });

      await listServices(1, 'MensHaircut');
      expect(mockGet).toHaveBeenCalledWith('/scheduler/services', { params: { boardId: 1, category: 'MensHaircut' } });
    });
  });

  describe('getService', () => {
    it('calls GET with id', async () => {
      const mockService = { id: 1, boardId: 1, name: 'Cut', category: 'MensHaircut', baseDuration: 30, basePrice: { amount: 60, currency: 'PLN' } };
      mockGet.mockResolvedValue({ data: mockService });

      const result = await getService(1);
      expect(mockGet).toHaveBeenCalledWith('/scheduler/services/1');
      expect(result).toEqual(mockService);
    });
  });

  describe('updateService', () => {
    it('puts to /scheduler/services/:id', async () => {
      mockPut.mockResolvedValue({ data: { id: 1 } });

      const data = { name: 'Premium Cut', category: 'MensHaircut', baseDuration: 45, basePrice: { amount: 80, currency: 'PLN' } };
      await updateService(1, data);
      expect(mockPut).toHaveBeenCalledWith('/scheduler/services/1', data);
    });
  });

  describe('deleteService', () => {
    it('deletes service by id', async () => {
      mockDelete.mockResolvedValue({});

      await deleteService(1);
      expect(mockDelete).toHaveBeenCalledWith('/scheduler/services/1');
    });
  });
});
