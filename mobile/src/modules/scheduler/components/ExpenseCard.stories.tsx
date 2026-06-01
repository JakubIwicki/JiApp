import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import ExpenseCard from './ExpenseCard';
import type { Expense } from '../types/api';

const expenseWithNote: Expense = {
  id: 1,
  boardId: 1,
  date: '2026-05-30',
  category: 'Fuel',
  amount: { amount: 120, currency: 'PLN' },
  note: 'Paliwo dojazd do salonu Warszawa-Krakow',
};

const expenseWithoutNote: Expense = {
  id: 2,
  boardId: 1,
  date: '2026-05-31',
  category: 'Supplies',
  amount: { amount: 89, currency: 'PLN' },
  note: undefined,
};

const meta: Meta<typeof ExpenseCard> = {
  title: 'Scheduler/ExpenseCard',
  component: ExpenseCard,
  decorators: [
    (Story) => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof ExpenseCard>;

export const Default: Story = {
  args: {
    expense: expenseWithNote,
    onPress: () => console.log('Pressed expense'),
  },
};

export const WithoutNote: Story = {
  args: {
    expense: expenseWithoutNote,
    onPress: () => console.log('Pressed expense'),
  },
};

const styles = StyleSheet.create({
  decorator: {
    padding: 16,
    justifyContent: 'center',
    flex: 1,
  },
});
