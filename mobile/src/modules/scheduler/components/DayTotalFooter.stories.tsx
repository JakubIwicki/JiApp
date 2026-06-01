import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import DayTotalFooter from './DayTotalFooter';
import type { DayTotal } from '../types/api';

const meta: Meta<typeof DayTotalFooter> = {
  title: 'Scheduler/DayTotalFooter',
  component: DayTotalFooter,
  decorators: [
    (Story) => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof DayTotalFooter>;

export const PositiveNet: Story = {
  args: {
    dayTotal: {
      revenue: 870,
      expenses: 185,
      net: 685,
    },
  },
};

export const BreakEven: Story = {
  args: {
    dayTotal: {
      revenue: 300,
      expenses: 300,
      net: 0,
    },
  },
};

const styles = StyleSheet.create({
  decorator: {
    padding: 16,
    justifyContent: 'center',
    flex: 1,
  },
});
