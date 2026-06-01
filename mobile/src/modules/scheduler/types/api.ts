export interface Price {
  amount: number;
  currency: string;
}

export type ServiceCategory =
  | 'MensHaircut'
  | 'WomensHaircut'
  | 'WomensStyling'
  | 'Coloring'
  | 'Treatment'
  | 'Other';

export type AppointmentStatus = 'Created' | 'Done' | 'Cancelled';

export type ExpenseCategory =
  | 'Fuel'
  | 'Hotel'
  | 'Parking'
  | 'Supplies'
  | 'Food'
  | 'Other';

export interface Board {
  id: number;
  name: string;
  memberUserIds: number[];
  createdAt: string;
}

export interface Client {
  id: number;
  boardId: number;
  name: string;
  phone?: string;
  notes?: string;
}

export interface ServiceItem {
  id: number;
  boardId: number;
  name: string;
  category: ServiceCategory;
  baseDuration: number;
  basePrice: Price;
}

export interface Appointment {
  id: number;
  boardId: number;
  client: Client;
  service: ServiceItem;
  description?: string;
  date: string;
  startTime: string;
  endTime: string;
  price: Price;
  location: string;
  status: AppointmentStatus;
}

export interface Expense {
  id: number;
  boardId: number;
  date: string;
  category: ExpenseCategory;
  amount: Price;
  note?: string;
}

export interface DayTotal {
  revenue: number;
  expenses: number;
  net: number;
}

export interface RevenueReport {
  groupKey: string;
  revenue: number;
  expenses: number;
  net: number;
  appointmentCount: number;
}

export interface ClientReportItem {
  client: Client;
  visitCount: number;
  totalSpent: number;
  lastVisit: string;
  averagePerVisit: number;
}
