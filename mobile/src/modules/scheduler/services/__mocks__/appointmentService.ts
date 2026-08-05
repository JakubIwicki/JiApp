import { createMockFn } from '../../../../test/createMockFn';
import { getThisWeekend } from '../../../../test/dateUtils';
import type { Appointment, AppointmentStatus } from '../../types/api';
import type { CreateAppointmentData } from '../appointmentService';

// ── Default stub data ──────────────────────────────────────────────────────

const baseAppointments: Omit<Appointment, 'date'>[] = [
  {
    id: 1,
    boardId: 1,
    client: {
      id: 1,
      boardId: 1,
      name: 'Anna Kowalska',
      phone: '+48 601 111 222',
      notes: null,
    },
    service: {
      id: 1,
      boardId: 1,
      name: 'Strzyzenie meskie',
      category: 'MensHaircut',
      baseDuration: 30,
      basePrice: { amount: 60, currency: 'PLN' },
    },
    description: null,
    startTime: '09:00',
    endTime: '09:30',
    price: { amount: 60, currency: 'PLN' },
    location: 'Salon Warszawa',
    status: 'Created',
  },
  {
    id: 2,
    boardId: 1,
    client: {
      id: 2,
      boardId: 1,
      name: 'Marta Zielinska',
      phone: '+48 602 222 333',
      notes: null,
    },
    service: {
      id: 5,
      boardId: 1,
      name: 'Stylizacja wieczorowa',
      category: 'WomensStyling',
      baseDuration: 90,
      basePrice: { amount: 200, currency: 'PLN' },
    },
    description:
      'Klientka chce upiecie z warkoczem i delikatnymi falami. Inspiracja ze zdjecia z Pinteresta.',
    startTime: '10:00',
    endTime: '11:30',
    price: { amount: 200, currency: 'PLN' },
    location: undefined as unknown as string,
    status: 'Created',
  },
  {
    id: 3,
    boardId: 1,
    client: {
      id: 3,
      boardId: 1,
      name: 'Piotr Nowak',
      phone: null,
      notes: null,
    },
    service: {
      id: 3,
      boardId: 1,
      name: 'Trymowanie brody',
      category: 'MensHaircut',
      baseDuration: 15,
      basePrice: { amount: 25, currency: 'PLN' },
    },
    description: null,
    startTime: '11:00',
    endTime: '11:15',
    price: { amount: 25, currency: 'PLN' },
    location: 'Salon Krakow',
    status: 'Done',
  },
  {
    id: 4,
    boardId: 1,
    client: {
      id: 4,
      boardId: 1,
      name: 'Katarzyna Adamczyk',
      phone: '+48 603 333 444',
      notes: null,
    },
    service: {
      id: 7,
      boardId: 1,
      name: 'Koloryzacja pelna',
      category: 'Coloring',
      baseDuration: 120,
      basePrice: { amount: 300, currency: 'PLN' },
    },
    description:
      'Pelna koloryzacja z pasemkami. Poprzednio robiona 6 tygodni temu.',
    startTime: '09:00',
    endTime: '11:00',
    price: { amount: 300, currency: 'PLN' },
    location: 'Salon Warszawa',
    status: 'Created',
  },
];

const { saturday, sunday } = getThisWeekend();
const defaultAppointments: Appointment[] = baseAppointments.map(a => ({
  ...a,
  // First 3 appointments go on Saturday, the rest on Sunday
  date: a.id <= 3 ? saturday : sunday,
}));

// ── Internal state ─────────────────────────────────────────────────────────

let _appointments: Appointment[] = defaultAppointments;
let _appointmentError: Error | null = null;
let _createAppointmentError: Error | null = null;

// ── Mock functions ─────────────────────────────────────────────────────────

export const listAppointments = createMockFn(
  async (_boardId: number, _dates: string[]): Promise<Appointment[]> => {
    if (_appointmentError) throw _appointmentError;
    return _appointments;
  },
);

export const getAppointment = createMockFn(
  async (id: number): Promise<Appointment> => {
    if (_appointmentError) throw _appointmentError;
    const appointment = _appointments.find(a => a.id === id);
    if (!appointment) throw new Error('Appointment not found');
    return appointment;
  },
);

export const createAppointment = createMockFn(
  async (_data: CreateAppointmentData): Promise<{ id: number }> => {
    if (_createAppointmentError) throw _createAppointmentError;
    return { id: 99 };
  },
);

export const updateAppointment = createMockFn(async (): Promise<void> => {});

export const updateStatus = createMockFn(
  async (_id: number, _status: AppointmentStatus): Promise<void> => {},
);

export const deleteAppointment = createMockFn(
  async (_id: number): Promise<void> => {},
);

// ── Fluent scenario builders (.withX()) ────────────────────────────────────

export function withAppointments(
  appointments: Appointment[] = defaultAppointments,
): Appointment[] {
  _appointmentError = null;
  _appointments = appointments;
  return _appointments;
}

export function withAppointmentError(
  error: Error = new Error('Mock error'),
): Error {
  _appointmentError = error;
  return error;
}

export function withCreateAppointmentError(
  error: Error = new Error('Mock error'),
): Error {
  _createAppointmentError = error;
  return error;
}

// ── Reset ──────────────────────────────────────────────────────────────────

export function reset(): void {
  _appointments = defaultAppointments;
  _appointmentError = null;
  _createAppointmentError = null;

  if (typeof jest !== 'undefined') {
    listAppointments.mockClear();
    getAppointment.mockClear();
    createAppointment.mockClear();
    updateAppointment.mockClear();
    updateStatus.mockClear();
    deleteAppointment.mockClear();
  }
}
