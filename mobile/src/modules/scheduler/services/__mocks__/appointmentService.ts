import type { Appointment, AppointmentStatus } from '../../types/api';
import type { CreateAppointmentData } from '../appointmentService';

type Mode = 'success' | 'empty' | 'error';

let _mode: Mode = 'success';

export const setAppointmentMode = (mode: Mode) => {
  _mode = mode;
};

function getThisWeekend(): { saturday: string; sunday: string } {
  const now = new Date();
  const dayOfWeek = now.getDay();
  // 0=Sun, 1=Mon, ..., 6=Sat
  const daysUntilSaturday = dayOfWeek === 6 ? 0 : (6 - dayOfWeek + 7) % 7;
  const saturday = new Date(now);
  saturday.setDate(now.getDate() + daysUntilSaturday);

  const sunday = new Date(saturday);
  sunday.setDate(saturday.getDate() + 1);

  const fmt = (d: Date) => {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  };

  return { saturday: fmt(saturday), sunday: fmt(sunday) };
}

const baseAppointments: Omit<Appointment, 'date'>[] = [
  {
    id: 1,
    boardId: 1,
    client: { id: 1, name: 'Anna Kowalska', phone: '+48 601 111 222' },
    service: {
      id: 1,
      boardId: 1,
      name: 'Strzyzenie meskie',
      category: 'MensHaircut',
      baseDuration: 30,
      basePrice: { amount: 60, currency: 'PLN' },
    },
    description: undefined,
    startTime: '09:00',
    endTime: '09:30',
    price: { amount: 60, currency: 'PLN' },
    location: 'Salon Warszawa',
    status: 'Created',
  },
  {
    id: 2,
    boardId: 1,
    client: { id: 2, name: 'Marta Zielinska', phone: '+48 602 222 333' },
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
    client: { id: 3, name: 'Piotr Nowak' },
    service: {
      id: 3,
      boardId: 1,
      name: 'Trymowanie brody',
      category: 'MensHaircut',
      baseDuration: 15,
      basePrice: { amount: 25, currency: 'PLN' },
    },
    description: undefined,
    startTime: '11:00',
    endTime: '11:15',
    price: { amount: 25, currency: 'PLN' },
    location: 'Salon Krakow',
    status: 'Done',
  },
  {
    id: 4,
    boardId: 1,
    client: { id: 4, name: 'Katarzyna Adamczyk', phone: '+48 603 333 444' },
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

export const listAppointments = async (
  _boardId: number,
  _dates: string[],
): Promise<Appointment[]> => {
  if (_mode === 'error') throw new Error('Mock error');
  if (_mode === 'empty') return [];

  const { saturday, sunday } = getThisWeekend();

  return baseAppointments.map((a) => ({
    ...a,
    // First 3 appointments go on Saturday, the rest on Sunday
    date: a.id <= 3 ? saturday : sunday,
  }));
};

export const getAppointment = async (id: number): Promise<Appointment> => {
  if (_mode === 'error') throw new Error('Mock error');
  const { saturday, sunday } = getThisWeekend();
  const base = baseAppointments.find((a) => a.id === id);
  if (!base) throw new Error('Appointment not found');
  return {
    ...base,
    date: base.id <= 3 ? saturday : sunday,
  };
};

export const createAppointment = async (
  _data: CreateAppointmentData,
): Promise<{ id: number }> => {
  return { id: 99 };
};

export const updateAppointment = async (): Promise<void> => {};

export const updateStatus = async (
  _id: number,
  _status: AppointmentStatus,
): Promise<void> => {};

export const deleteAppointment = async (_id: number): Promise<void> => {};
