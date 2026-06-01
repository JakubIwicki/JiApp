import type { RevenueReport, ClientReportItem } from '../../types/api';

type Mode = 'success' | 'empty' | 'error';

let _mode: Mode = 'success';

export const setReportMode = (mode: Mode) => {
  _mode = mode;
};

const mockRevenueReports: RevenueReport[] = [
  {
    groupKey: '2026-05-30',
    revenue: 870,
    expenses: 185,
    net: 685,
    appointmentCount: 8,
  },
  {
    groupKey: '2026-05-31',
    revenue: 620,
    expenses: 89,
    net: 531,
    appointmentCount: 5,
  },
  {
    groupKey: 'MensHaircut',
    revenue: 650,
    expenses: 0,
    net: 650,
    appointmentCount: 12,
  },
  {
    groupKey: 'Coloring',
    revenue: 1040,
    expenses: 0,
    net: 1040,
    appointmentCount: 4,
  },
  {
    groupKey: 'Salon Warszawa',
    revenue: 1100,
    expenses: 120,
    net: 980,
    appointmentCount: 9,
  },
  {
    groupKey: 'Salon Krakow',
    revenue: 390,
    expenses: 154,
    net: 236,
    appointmentCount: 4,
  },
];

const mockClientReports: ClientReportItem[] = [
  {
    client: { id: 1, boardId: 1, name: 'Anna Kowalska', phone: '+48 601 111 222' },
    visitCount: 12,
    totalSpent: 720,
    lastVisit: '2026-05-30',
    averagePerVisit: 60,
  },
  {
    client: { id: 2, boardId: 1, name: 'Marta Zielinska', phone: '+48 602 222 333' },
    visitCount: 8,
    totalSpent: 1600,
    lastVisit: '2026-05-28',
    averagePerVisit: 200,
  },
  {
    client: { id: 3, boardId: 1, name: 'Piotr Nowak' },
    visitCount: 6,
    totalSpent: 300,
    lastVisit: '2026-05-25',
    averagePerVisit: 50,
  },
  {
    client: { id: 4, boardId: 1, name: 'Katarzyna Adamczyk', phone: '+48 603 333 444' },
    visitCount: 5,
    totalSpent: 1250,
    lastVisit: '2026-05-20',
    averagePerVisit: 250,
  },
  {
    client: { id: 5, boardId: 1, name: 'Michal Lewandowski', phone: '+48 604 555 666' },
    visitCount: 3,
    totalSpent: 120,
    lastVisit: '2026-05-15',
    averagePerVisit: 40,
  },
];

export const getRevenueReport = async (
  _boardId: number,
  _from: string,
  _to: string,
  _groupBy: string,
): Promise<RevenueReport[]> => {
  if (_mode === 'error') throw new Error('Mock error');
  if (_mode === 'empty') return [];
  return mockRevenueReports;
};

export const getClientReport = async (
  _boardId: number,
  _sortBy: string,
): Promise<ClientReportItem[]> => {
  if (_mode === 'error') throw new Error('Mock error');
  if (_mode === 'empty') return [];
  return mockClientReports;
};
