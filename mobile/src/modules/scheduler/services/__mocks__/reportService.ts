import { createMockFn } from '../../../../test/createMockFn';
import type { RevenueReport, ClientReportItem } from '../../types/api';

// ── Default stub data ──────────────────────────────────────────────────────

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
    clientId: 1,
    clientName: 'Anna Kowalska',
    visitCount: 12,
    totalSpent: 720,
    lastVisitDate: '2026-05-30',
    averagePerVisit: 60,
  },
  {
    clientId: 2,
    clientName: 'Marta Zielinska',
    visitCount: 8,
    totalSpent: 1600,
    lastVisitDate: '2026-05-28',
    averagePerVisit: 200,
  },
  {
    clientId: 3,
    clientName: 'Piotr Nowak',
    visitCount: 6,
    totalSpent: 300,
    lastVisitDate: '2026-05-25',
    averagePerVisit: 50,
  },
  {
    clientId: 4,
    clientName: 'Katarzyna Adamczyk',
    visitCount: 5,
    totalSpent: 1250,
    lastVisitDate: '2026-05-20',
    averagePerVisit: 250,
  },
  {
    clientId: 5,
    clientName: 'Michal Lewandowski',
    visitCount: 3,
    totalSpent: 120,
    lastVisitDate: '2026-05-15',
    averagePerVisit: 40,
  },
];

// ── Internal state ─────────────────────────────────────────────────────────

let _revenueReports: RevenueReport[] = mockRevenueReports;
let _clientReports: ClientReportItem[] = mockClientReports;
let _reportError: Error | null = null;

// ── Mock functions ─────────────────────────────────────────────────────────

export const getRevenueReport = createMockFn(
  async (
    _boardId: number,
    _from: string,
    _to: string,
    _groupBy: string,
  ): Promise<RevenueReport[]> => {
    if (_reportError) throw _reportError;
    return _revenueReports;
  },
);

export const getClientReport = createMockFn(
  async (_boardId: number, _sortBy: string): Promise<ClientReportItem[]> => {
    if (_reportError) throw _reportError;
    return _clientReports;
  },
);

// ── Fluent scenario builders (.withX()) ────────────────────────────────────

export function withRevenueReport(
  reports: RevenueReport[] = mockRevenueReports,
): RevenueReport[] {
  _reportError = null;
  _revenueReports = reports;
  return _revenueReports;
}

export function withClientReport(
  reports: ClientReportItem[] = mockClientReports,
): ClientReportItem[] {
  _reportError = null;
  _clientReports = reports;
  return _clientReports;
}

export function withReportError(error: Error = new Error('Mock error')): Error {
  _reportError = error;
  return error;
}

// ── Reset ──────────────────────────────────────────────────────────────────

export function reset(): void {
  _revenueReports = mockRevenueReports;
  _clientReports = mockClientReports;
  _reportError = null;

  if (typeof jest !== 'undefined') {
    getRevenueReport.mockClear();
    getClientReport.mockClear();
  }
}
