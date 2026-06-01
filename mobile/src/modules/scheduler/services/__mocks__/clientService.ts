import type { Client } from '../../types/api';
import type { ClientWithAppointments } from '../clientService';

type Mode = 'success' | 'empty' | 'error';

let _mode: Mode = 'success';

export const setClientMode = (mode: Mode) => {
  _mode = mode;
};

const mockClients: Client[] = [
  { id: 1, boardId: 1, name: 'Anna Kowalska', phone: '+48 601 111 222' },
  { id: 2, boardId: 1, name: 'Marta Zielinska', phone: '+48 602 222 333' },
  { id: 3, boardId: 1, name: 'Piotr Nowak' },
  { id: 4, boardId: 1, name: 'Katarzyna Adamczyk', phone: '+48 603 333 444' },
  { id: 5, boardId: 1, name: 'Michał Lewandowski', phone: '+48 604 555 666' },
  { id: 6, boardId: 1, name: 'Joanna Wisniewska' },
];

const mockClientDetail: ClientWithAppointments = {
  id: 1,
  boardId: 1,
  name: 'Anna Kowalska',
  phone: '+48 601 111 222',
  notes: 'Stala klientka od 2023. Preferuje wizyty w godzinach porannych.',
  appointments: [
    {
      id: 1,
      date: '2026-05-30',
      startTime: '09:00',
      endTime: '09:30',
      serviceName: 'Strzyzenie meskie',
      status: 'Created',
    },
    {
      id: 5,
      date: '2026-05-16',
      startTime: '10:00',
      endTime: '10:30',
      serviceName: 'Strzyzenie meskie',
      status: 'Done',
    },
    {
      id: 8,
      date: '2026-05-02',
      startTime: '14:00',
      endTime: '14:30',
      serviceName: 'Trymowanie brody',
      status: 'Done',
    },
    {
      id: 12,
      date: '2026-04-18',
      startTime: '11:00',
      endTime: '11:45',
      serviceName: 'Strzyzenie maszynka',
      status: 'Done',
    },
  ],
};

export const listClients = async (_boardId?: number, _q?: string): Promise<Client[]> => {
  if (_mode === 'error') throw new Error('Mock error');
  if (_mode === 'empty') return [];

  if (_q) {
    const q = _q.toLowerCase();
    return mockClients.filter((c) => c.name.toLowerCase().includes(q));
  }
  return mockClients;
};

export const getClient = async (id: number): Promise<ClientWithAppointments> => {
  if (_mode === 'error') throw new Error('Mock error');
  if (id === 1) return mockClientDetail;
  return { ...mockClientDetail, id, name: 'Unknown Client' };
};

export const createClient = async (_boardId?: number, _data?: { name: string; phone?: string; notes?: string }): Promise<{ id: number }> => {
  return { id: 99 };
};

export const updateClient = async (): Promise<void> => {};

export const deleteClient = async (_id: number): Promise<void> => {};
