import { createMockFn } from '../../../../test/createMockFn';
import type { ServiceItem } from '../../types/api';

// ── Default stub data ──────────────────────────────────────────────────────

const mockServices: ServiceItem[] = [
  {
    id: 1,
    boardId: 1,
    name: 'Strzyzenie meskie',
    category: 'MensHaircut',
    baseDuration: 30,
    basePrice: { amount: 60, currency: 'PLN' },
  },
  {
    id: 2,
    boardId: 1,
    name: 'Strzyzenie maszynka',
    category: 'MensHaircut',
    baseDuration: 20,
    basePrice: { amount: 40, currency: 'PLN' },
  },
  {
    id: 3,
    boardId: 1,
    name: 'Trymowanie brody',
    category: 'MensHaircut',
    baseDuration: 15,
    basePrice: { amount: 25, currency: 'PLN' },
  },
  {
    id: 4,
    boardId: 1,
    name: 'Strzyzenie damskie',
    category: 'WomensHaircut',
    baseDuration: 45,
    basePrice: { amount: 100, currency: 'PLN' },
  },
  {
    id: 5,
    boardId: 1,
    name: 'Stylizacja wieczorowa',
    category: 'WomensStyling',
    baseDuration: 90,
    basePrice: { amount: 200, currency: 'PLN' },
  },
  {
    id: 6,
    boardId: 1,
    name: 'Upiecie okolicznosciowe',
    category: 'WomensStyling',
    baseDuration: 60,
    basePrice: { amount: 150, currency: 'PLN' },
  },
  {
    id: 7,
    boardId: 1,
    name: 'Koloryzacja pelna',
    category: 'Coloring',
    baseDuration: 120,
    basePrice: { amount: 300, currency: 'PLN' },
  },
  {
    id: 8,
    boardId: 1,
    name: 'Pasemka',
    category: 'Coloring',
    baseDuration: 90,
    basePrice: { amount: 220, currency: 'PLN' },
  },
  {
    id: 9,
    boardId: 1,
    name: 'Zabieg regenerujacy',
    category: 'Treatment',
    baseDuration: 45,
    basePrice: { amount: 120, currency: 'PLN' },
  },
  {
    id: 10,
    boardId: 1,
    name: 'Botoks keratynowy',
    category: 'Treatment',
    baseDuration: 60,
    basePrice: { amount: 180, currency: 'PLN' },
  },
];

// ── Internal state ─────────────────────────────────────────────────────────

let _services: ServiceItem[] = mockServices;
let _serviceError: Error | null = null;

// ── Mock functions ─────────────────────────────────────────────────────────

export const listServices = createMockFn(
  async (_boardId?: number, _category?: string): Promise<ServiceItem[]> => {
    if (_serviceError) throw _serviceError;
    if (_category) {
      return _services.filter(s => s.category === _category);
    }
    return _services;
  },
);

export const getService = createMockFn(
  async (id: number): Promise<ServiceItem> => {
    if (_serviceError) throw _serviceError;
    const svc = _services.find(s => s.id === id);
    if (!svc) throw new Error('Service not found');
    return svc;
  },
);

export const createService = createMockFn(async (): Promise<{ id: number }> => {
  return { id: 99 };
});

export const updateService = createMockFn(async (): Promise<void> => {});

export const deleteService = createMockFn(
  async (_id: number): Promise<void> => {},
);

// ── Fluent scenario builders (.withX()) ────────────────────────────────────

export function withServices(
  services: ServiceItem[] = mockServices,
): ServiceItem[] {
  _serviceError = null;
  _services = services;
  return _services;
}

export function withServiceError(
  error: Error = new Error('Mock error'),
): Error {
  _serviceError = error;
  return error;
}

// ── Reset ──────────────────────────────────────────────────────────────────

export function reset(): void {
  _services = mockServices;
  _serviceError = null;

  if (typeof jest !== 'undefined') {
    listServices.mockClear();
    getService.mockClear();
    createService.mockClear();
    updateService.mockClear();
    deleteService.mockClear();
  }
}
