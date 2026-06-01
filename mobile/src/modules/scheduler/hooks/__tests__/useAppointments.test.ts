import { renderHook, act } from '@testing-library/react-native';
import useAppointments from '../useAppointments';
import * as appointmentService from '../../services/appointmentService';
import type { Appointment } from '../../types/api';

jest.mock('../../services/appointmentService', () => ({
  createAppointment: jest.fn(),
  listAppointments: jest.fn(),
  getAppointment: jest.fn(),
  updateAppointment: jest.fn(),
  updateStatus: jest.fn(),
  deleteAppointment: jest.fn(),
}));

const mockCreateAppointment = appointmentService.createAppointment as jest.Mock;
const mockListAppointments = appointmentService.listAppointments as jest.Mock;
const mockDeleteAppointment = appointmentService.deleteAppointment as jest.Mock;
const mockUpdateStatus = appointmentService.updateStatus as jest.Mock;

const mockAppointment: Appointment = {
  id: 1, boardId: 1,
  client: { id: 2, name: 'Jane' },
  service: { id: 3, boardId: 1, name: 'Cut', category: 'WomensHaircut', baseDuration: 30, basePrice: { amount: 80, currency: 'PLN' } },
  date: '2026-05-23', startTime: '10:00', endTime: '10:30',
  price: { amount: 80, currency: 'PLN' }, location: 'Salon', status: 'Created',
};

describe('useAppointments', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('has initial state', () => {
    const { result } = renderHook(() => useAppointments());
    expect(result.current.appointments).toEqual([]);
    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it('loads appointments for given board and dates', async () => {
    mockListAppointments.mockResolvedValue([mockAppointment]);

    const { result } = renderHook(() => useAppointments());

    await act(async () => {
      await result.current.loadAppointments(1, ['2026-05-23', '2026-05-24']);
    });

    expect(mockListAppointments).toHaveBeenCalledWith(1, ['2026-05-23', '2026-05-24']);
    expect(result.current.appointments).toEqual([mockAppointment]);
    expect(result.current.isLoading).toBe(false);
  });

  it('sets loading state during fetch', async () => {
    mockListAppointments.mockImplementation(() => new Promise((r) => setTimeout(r, 100)));

    const { result } = renderHook(() => useAppointments());

    let promise: Promise<void>;
    act(() => {
      promise = result.current.loadAppointments(1, ['2026-05-23']);
    });

    expect(result.current.isLoading).toBe(true);

    await act(async () => {
      await promise;
    });
  });

  it('handles load error', async () => {
    mockListAppointments.mockRejectedValue(new Error('Network error'));

    const { result } = renderHook(() => useAppointments());

    await act(async () => {
      await result.current.loadAppointments(1, ['2026-05-23']);
    });

    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBe('Network error');
    expect(result.current.appointments).toEqual([]);
  });

  it('adds appointment optimistically', async () => {
    mockCreateAppointment.mockResolvedValue({ id: 99 });

    const { result } = renderHook(() => useAppointments());

    const data = {
      boardId: 1, clientId: 2, serviceId: 3,
      date: '2026-05-23', startTime: '10:00', endTime: '11:00',
      description: '', location: 'Salon',
      price: { amount: 80, currency: 'PLN' },
    };

    await act(async () => {
      await result.current.addAppointment(data);
    });

    expect(mockCreateAppointment).toHaveBeenCalledWith(data);
  });

  it('removes appointment optimistically and rolls back on error', async () => {
    mockDeleteAppointment.mockRejectedValue(new Error('Delete failed'));

    const { result } = renderHook(() => useAppointments());

    await act(async () => {
      await result.current.loadAppointments(1, ['2026-05-23']);
    });

    mockListAppointments.mockResolvedValue([mockAppointment]);
    await act(async () => {
      await result.current.loadAppointments(1, ['2026-05-23']);
    });

    expect(result.current.appointments).toHaveLength(1);

    await act(async () => {
      await expect(result.current.removeAppointment(1)).rejects.toThrow('Delete failed');
    });
  });

  it('marks appointment as done', async () => {
    mockUpdateStatus.mockResolvedValue(undefined);

    const { result } = renderHook(() => useAppointments());

    await act(async () => {
      await result.current.markDone(1);
    });

    expect(mockUpdateStatus).toHaveBeenCalledWith(1, 'Done');
  });

  it('cancels appointment', async () => {
    mockUpdateStatus.mockResolvedValue(undefined);

    const { result } = renderHook(() => useAppointments());

    await act(async () => {
      await result.current.markCancelled(1);
    });

    expect(mockUpdateStatus).toHaveBeenCalledWith(1, 'Cancelled');
  });
});
