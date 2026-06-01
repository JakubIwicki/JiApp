import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import WeekendNavigator from './WeekendNavigator';

const meta: Meta<typeof WeekendNavigator> = {
  title: 'Scheduler/WeekendNavigator',
  component: WeekendNavigator,
  decorators: [
    (Story) => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof WeekendNavigator>;

export const Default: Story = {
  args: {
    weekLabel: '2026-05-30 / 2026-05-31',
    onPrevious: () => console.log('Previous weekend'),
    onNext: () => console.log('Next weekend'),
    onToday: () => console.log('Go to today'),
  },
};

const styles = StyleSheet.create({
  decorator: {
    padding: 16,
    justifyContent: 'center',
    flex: 1,
  },
});
