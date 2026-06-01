import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import SummaryBar from './SummaryBar';

const meta: Meta<typeof SummaryBar> = {
  title: 'Scheduler/SummaryBar',
  component: SummaryBar,
  decorators: [
    (Story) => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof SummaryBar>;

export const Default: Story = {
  args: {
    saturdayTotal: { revenue: 480, expenses: 165, net: 315 },
    sundayTotal: { revenue: 390, expenses: 20, net: 370 },
  },
};

const styles = StyleSheet.create({
  decorator: {
    padding: 16,
    justifyContent: 'center',
    flex: 1,
  },
});
