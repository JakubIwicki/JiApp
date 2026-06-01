import type { ServiceItem } from '../../types/api';

type Mode = 'success' | 'empty' | 'error';

let _mode: Mode = 'success';

export const setServiceMode = (mode: Mode) => {
  _mode = mode;
};

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

export const listServices = async (
  _boardId?: number,
  _category?: string,
): Promise<ServiceItem[]> => {
  if (_mode === 'error') throw new Error('Mock error');
  if (_mode === 'empty') return [];
  if (_category) {
    return mockServices.filter((s) => s.category === _category);
  }
  return mockServices;
};

export const getService = async (id: number): Promise<ServiceItem> => {
  if (_mode === 'error') throw new Error('Mock error');
  const svc = mockServices.find((s) => s.id === id);
  if (!svc) throw new Error('Service not found');
  return svc;
};

export const createService = async (): Promise<{ id: number }> => {
  return { id: 99 };
};

export const updateService = async (): Promise<void> => {};

export const deleteService = async (_id: number): Promise<void> => {};
