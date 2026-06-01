import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import AppointmentCard from './AppointmentCard';
import type { Appointment } from '../types/api';

const mensHaircutAppointment: Appointment = {
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
  date: '2026-05-30',
  startTime: '09:00',
  endTime: '09:30',
  price: { amount: 60, currency: 'PLN' },
  location: 'Salon Warszawa',
  status: 'Created',
};

const womensStylingAppointment: Appointment = {
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
  description: undefined,
  date: '2026-05-30',
  startTime: '10:00',
  endTime: '11:30',
  price: { amount: 200, currency: 'PLN' },
  location: undefined as unknown as string,
  status: 'Created',
};

const appointmentWithDescription: Appointment = {
  id: 3,
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
    'Pelna koloryzacja z pasemkami. Klientka chce zrobic tez delikatne fale. Poprzednia wizyta byla 6 tygodni temu. Nalezy przygotowac farbe w odcieniu 7.3.',
  date: '2026-05-31',
  startTime: '09:00',
  endTime: '11:00',
  price: { amount: 300, currency: 'PLN' },
  location: 'Salon Warszawa',
  status: 'Created',
};

const meta: Meta<typeof AppointmentCard> = {
  title: 'Scheduler/AppointmentCard',
  component: AppointmentCard,
  decorators: [
    (Story) => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof AppointmentCard>;

export const MensHaircut: Story = {
  args: {
    appointment: mensHaircutAppointment,
    onPress: () => console.log('Pressed appointment 1'),
  },
};

export const WomensService: Story = {
  args: {
    appointment: womensStylingAppointment,
    onPress: () => console.log('Pressed appointment 2'),
  },
};

export const WithDescription: Story = {
  args: {
    appointment: appointmentWithDescription,
    onPress: () => console.log('Pressed appointment 3'),
  },
};

const styles = StyleSheet.create({
  decorator: {
    padding: 16,
    justifyContent: 'center',
    flex: 1,
  },
});
