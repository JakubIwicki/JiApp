import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import DayColumn from './DayColumn';
import type { Appointment, Expense, DayTotal } from '../types/api';

const appointments: Appointment[] = [
  {
    id: 1,
    boardId: 1,
    client: { id: 1, boardId: 1, name: 'Anna Kowalska', phone: '+48 601 111 222' },
    service: {
      id: 1,
      boardId: 1,
      name: 'Strzyzenie meskie',
      category: 'MensHaircut',
      baseDuration: 30,
      basePrice: { amount: 60, currency: 'PLN' },
    },
    description: undefined,
    date: '2026-05-30',
    startTime: '09:00',
    endTime: '09:30',
    price: { amount: 60, currency: 'PLN' },
    location: 'Salon Warszawa',
    status: 'Created',
  },
  {
    id: 2,
    boardId: 1,
    client: { id: 2, boardId: 1, name: 'Marta Zielinska', phone: '+48 602 222 333' },
    service: {
      id: 5,
      boardId: 1,
      name: 'Stylizacja wieczorowa',
      category: 'WomensStyling',
      baseDuration: 90,
      basePrice: { amount: 200, currency: 'PLN' },
    },
    description: undefined,
    date: '2026-05-30',
    startTime: '10:00',
    endTime: '11:30',
    price: { amount: 200, currency: 'PLN' },
    location: undefined as unknown as string,
    status: 'Created',
  },
  {
    id: 3,
    boardId: 1,
    client: { id: 3, boardId: 1, name: 'Piotr Nowak' },
    service: {
      id: 3,
      boardId: 1,
      name: 'Trymowanie brody',
      category: 'MensHaircut',
      baseDuration: 15,
      basePrice: { amount: 25, currency: 'PLN' },
    },
    description: undefined,
    date: '2026-05-30',
    startTime: '11:00',
    endTime: '11:15',
    price: { amount: 25, currency: 'PLN' },
    location: 'Salon Krakow',
    status: 'Done',
  },
];

const expenses: Expense[] = [
  {
    id: 1,
    boardId: 1,
    date: '2026-05-30',
    category: 'Fuel',
    amount: { amount: 120, currency: 'PLN' },
    note: 'Paliwo dojazd do salonu Warszawa-Krakow',
  },
  {
    id: 2,
    boardId: 1,
    date: '2026-05-30',
    category: 'Food',
    amount: { amount: 45, currency: 'PLN' },
    note: 'Obiad w miedzymiescie',
  },
];

const dayTotal: DayTotal = {
  revenue: 285,
  expenses: 165,
  net: 120,
};

const meta: Meta<typeof DayColumn> = {
  title: 'Scheduler/DayColumn',
  component: DayColumn,
  decorators: [
    (Story) => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof DayColumn>;

export const WithAppointments: Story = {
  args: {
    label: 'Saturday',
    date: '2026-05-30',
    appointments,
    expenses,
    dayTotal,
    onAppointmentPress: (appt) => console.log('Pressed', appt.id),
    isToday: true,
  },
};

export const Empty: Story = {
  args: {
    label: 'Sunday',
    date: '2026-05-31',
    appointments: [],
    expenses: [],
    dayTotal: { revenue: 0, expenses: 0, net: 0 },
    onAppointmentPress: (appt) => console.log('Pressed', appt.id),
    isToday: false,
  },
};

const styles = StyleSheet.create({
  decorator: {
    padding: 8,
    justifyContent: 'center',
    flex: 1,
  },
});
