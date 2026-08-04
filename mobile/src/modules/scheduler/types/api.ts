import { z } from 'zod';

// ── Schemas ──────────────────────────────────────────────────────────────────

export const PriceSchema = z.object({
  amount: z.number(),
  currency: z.string(),
});

export const ServiceCategorySchema = z.enum([
  'MensHaircut',
  'WomensHaircut',
  'WomensStyling',
  'Coloring',
  'Treatment',
  'Other',
]);

export const AppointmentStatusSchema = z.enum(['Created', 'Done', 'Cancelled']);

export const ExpenseCategorySchema = z.enum([
  'Fuel',
  'Hotel',
  'Parking',
  'Supplies',
  'Food',
  'Other',
]);

export const BoardSchema = z.object({
  id: z.number(),
  name: z.string(),
  memberUserIds: z.array(z.number()),
  createdAt: z.string(),
});

export const ClientSchema = z.object({
  id: z.number(),
  boardId: z.number(),
  name: z.string(),
  phone: z.string().nullable(),
  notes: z.string().nullable(),
});

export const ServiceItemSchema = z.object({
  id: z.number(),
  boardId: z.number(),
  name: z.string(),
  category: ServiceCategorySchema,
  baseDuration: z.number(),
  basePrice: PriceSchema,
});

export const AppointmentSchema = z.object({
  id: z.number(),
  boardId: z.number(),
  client: ClientSchema,
  service: ServiceItemSchema,
  description: z.string().nullable(),
  date: z.string(),
  startTime: z.string(),
  endTime: z.string(),
  price: PriceSchema,
  location: z.string(),
  status: AppointmentStatusSchema,
});

// Backend ExpenseResponse is flat (amount + currency at top level); the app
// model nests them under `amount: Price`, so the parse maps raw -> app model.
export const ExpenseApiRawSchema = z.object({
  id: z.number(),
  boardId: z.number(),
  date: z.string(),
  category: ExpenseCategorySchema,
  amount: z.number(),
  currency: z.string(),
  note: z.string().nullable(),
});

export const ExpenseSchema = ExpenseApiRawSchema.transform(raw => ({
  id: raw.id,
  boardId: raw.boardId,
  date: raw.date,
  category: raw.category,
  amount: { amount: raw.amount, currency: raw.currency },
  note: raw.note,
}));

export const DayTotalSchema = z.object({
  revenue: z.number(),
  expenses: z.number(),
  net: z.number(),
});

export const RevenueReportSchema = z.object({
  groupKey: z.string(),
  revenue: z.number(),
  expenses: z.number(),
  net: z.number(),
  appointmentCount: z.number(),
});

export const ClientReportItemSchema = z.object({
  clientId: z.number(),
  clientName: z.string(),
  visitCount: z.number(),
  totalSpent: z.number(),
  lastVisitDate: z.string().nullable(),
  averagePerVisit: z.number(),
});

// ── Inferred types ───────────────────────────────────────────────────────────

export type Price = z.infer<typeof PriceSchema>;
export type ServiceCategory = z.infer<typeof ServiceCategorySchema>;
export type AppointmentStatus = z.infer<typeof AppointmentStatusSchema>;
export type ExpenseCategory = z.infer<typeof ExpenseCategorySchema>;
export type Board = z.infer<typeof BoardSchema>;
export type Client = z.infer<typeof ClientSchema>;
export type ServiceItem = z.infer<typeof ServiceItemSchema>;
export type Appointment = z.infer<typeof AppointmentSchema>;
export type Expense = z.infer<typeof ExpenseSchema>;
export type DayTotal = z.infer<typeof DayTotalSchema>;
export type RevenueReport = z.infer<typeof RevenueReportSchema>;
export type ClientReportItem = z.infer<typeof ClientReportItemSchema>;
