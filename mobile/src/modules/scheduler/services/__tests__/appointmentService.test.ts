import apiClient from '../../../../services/apiClient';
import {
  createAppointment,
  listAppointments,
  getAppointment,
  updateAppointment,
  updateStatus,
  deleteAppointment,
} from '../appointmentService';
import type { Appointment, AppointmentStatus } from '../../types/api';

jest.mock('../../../../services/apiClient', () => ({
  post: jest.fn(),
  get: jest.fn(),
  put: jest.fn(),
  patch: jest.fn(),
  delete: jest.fn(),
}));

const mockPost = apiClient.post as jest.Mock;
const mockGet = apiClient.get as jest.Mock;
const mockPut = apiClient.put as jest.Mock;
const mockPatch = apiClient.patch as jest.Mock;
const mockDelete = apiClient.delete as jest.Mock;

describe('appointmentService', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('createAppointment', () => {
    it('posts to /scheduler/appointments and returns id', async () => {
      mockPost.mockResolvedValue({ data: { id: 42 } });

      const data = {
        boardId: 1,
        clientId: 2,
        serviceId: 3,
        date: '2026-05-23',
        startTime: '10:00',
        endTime: '11:00',
        description: 'Test',
        location: 'Salon',
        price: { amount: 100, currency: 'PLN' },
      };

      const result = await createAppointment(data);
      expect(mockPost).toHaveBeenCalledWith('/scheduler/appointments', data);
      expect(result).toEqual({ id: 42 });
    });
  });

  describe('listAppointments', () => {
    it('calls GET with boardId and dates and returns appointments', async () => {
      const mockResponse: Appointment[] = [
        {
          id: 1, boardId: 1,
          client: { id: 2, boardId: 1, name: 'Jane' },
          service: { id: 3, boardId: 1, name: 'Cut', category: 'WomensHaircut', baseDuration: 30, basePrice: { amount: 80, currency: 'PLN' } },
          date: '2026-05-23', startTime: '10:00', endTime: '10:30',
          price: { amount: 80, currency: 'PLN' }, location: 'Salon', status: 'Created',
        },
      ];
      mockGet.mockResolvedValue({ data: mockResponse });

      const result = await listAppointments(1, ['2026-05-23', '2026-05-24']);
      expect(mockGet).toHaveBeenCalledWith('/scheduler/appointments', {
        params: { boardId: 1, date: ['2026-05-23', '2026-05-24'] },
      });
      expect(result).toEqual(mockResponse);
    });
  });

  describe('getAppointment', () => {
    it('calls GET with id and returns appointment', async () => {
      const mockAppointment: Appointment = {
        id: 1, boardId: 1,
        client: { id: 2, boardId: 1, name: 'Jane' },
        service: { id: 3, boardId: 1, name: 'Cut', category: 'WomensHaircut', baseDuration: 30, basePrice: { amount: 80, currency: 'PLN' } },
        date: '2026-05-23', startTime: '10:00', endTime: '10:30',
        price: { amount: 80, currency: 'PLN' }, location: 'Salon', status: 'Created',
      };
      mockGet.mockResolvedValue({ data: mockAppointment });

      const result = await getAppointment(1);
      expect(mockGet).toHaveBeenCalledWith('/scheduler/appointments/1');
      expect(result).toEqual(mockAppointment);
    });
  });

  describe('updateAppointment', () => {
    it('puts to /scheduler/appointments/:id with data', async () => {
      mockPut.mockResolvedValue({ data: { id: 1 } });

      const data = {
        clientId: 2, serviceId: 3, date: '2026-05-23',
        startTime: '11:00', endTime: '12:00',
        description: 'Updated', location: 'Salon',
        price: { amount: 100, currency: 'PLN' },
      };

      await updateAppointment(1, data);
      expect(mockPut).toHaveBeenCalledWith('/scheduler/appointments/1', data);
    });
  });

  describe('updateStatus', () => {
    it('patches appointment status', async () => {
      mockPatch.mockResolvedValue({ data: { id: 1 } });

      await updateStatus(1, 'Done' as AppointmentStatus);
      expect(mockPatch).toHaveBeenCalledWith('/scheduler/appointments/1/status', { status: 'Done' });
    });
  });

  describe('deleteAppointment', () => {
    it('deletes appointment by id', async () => {
      mockDelete.mockResolvedValue({});

      await deleteAppointment(1);
      expect(mockDelete).toHaveBeenCalledWith('/scheduler/appointments/1');
    });
  });
});
