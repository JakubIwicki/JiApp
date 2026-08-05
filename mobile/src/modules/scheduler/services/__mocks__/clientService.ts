import { createMockFn } from '../../../../test/createMockFn';
import type { Client } from '../../types/api';
import type { ClientWithAppointments } from '../clientService';

// ── Default stub data ──────────────────────────────────────────────────────

const mockClients: Client[] = [
  {
    id: 1,
    boardId: 1,
    name: 'Anna Kowalska',
    phone: '+48 601 111 222',
    notes: null,
  },
  {
    id: 2,
    boardId: 1,
    name: 'Marta Zielinska',
    phone: '+48 602 222 333',
    notes: null,
  },
  { id: 3, boardId: 1, name: 'Piotr Nowak', phone: null, notes: null },
  {
    id: 4,
    boardId: 1,
    name: 'Katarzyna Adamczyk',
    phone: '+48 603 333 444',
    notes: null,
  },
  {
    id: 5,
    boardId: 1,
    name: 'Michał Lewandowski',
    phone: '+48 604 555 666',
    notes: null,
  },
  { id: 6, boardId: 1, name: 'Joanna Wisniewska', phone: null, notes: null },
];

const mockClientDetail: ClientWithAppointments = {
  id: 1,
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

// ── Internal state ─────────────────────────────────────────────────────────

let _clients: Client[] = mockClients;
let _clientError: Error | null = null;

// ── Mock functions ─────────────────────────────────────────────────────────

export const listClients = createMockFn(
  async (_boardId?: number, _q?: string): Promise<Client[]> => {
    if (_clientError) throw _clientError;

    if (_q) {
      const q = _q.toLowerCase();
      return _clients.filter(c => c.name.toLowerCase().includes(q));
    }
    return _clients;
  },
);

export const getClient = createMockFn(
  async (id: number): Promise<ClientWithAppointments> => {
    if (_clientError) throw _clientError;
    if (id === 1) return mockClientDetail;
    return { ...mockClientDetail, id, name: 'Unknown Client' };
  },
);

export const createClient = createMockFn(
  async (
    _boardId?: number,
    _data?: { name: string; phone?: string; notes?: string },
  ): Promise<{ id: number }> => {
    return { id: 99 };
  },
);

export const updateClient = createMockFn(async (): Promise<void> => {});

export const deleteClient = createMockFn(
  async (_id: number): Promise<void> => {},
);

// ── Fluent scenario builders (.withX()) ────────────────────────────────────

export function withClients(clients: Client[] = mockClients): Client[] {
  _clientError = null;
  _clients = clients;
  return _clients;
}

export function withClientError(error: Error = new Error('Mock error')): Error {
  _clientError = error;
  return error;
}

// ── Reset ──────────────────────────────────────────────────────────────────

export function reset(): void {
  _clients = mockClients;
  _clientError = null;

  if (typeof jest !== 'undefined') {
    listClients.mockClear();
    getClient.mockClear();
    createClient.mockClear();
    updateClient.mockClear();
    deleteClient.mockClear();
  }
}
