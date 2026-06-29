import React, { useState } from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import SegmentedControl from './SegmentedControl';

const StatefulWrapper: React.FC<{
  options: { value: string; label: string }[];
  initialValue: string;
}> = ({ options, initialValue }) => {
  const [value, setValue] = useState(initialValue);
  return (
    <SegmentedControl options={options} value={value} onChange={setValue} />
  );
};

const meta: Meta<typeof SegmentedControl> = {
  title: 'SegmentedControl',
  component: SegmentedControl,
  decorators: [
    Story => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof SegmentedControl>;

export const TwoOptions: Story = {
  render: () => (
    <StatefulWrapper
      options={[
        { value: 'option1', label: 'Option 1' },
        { value: 'option2', label: 'Option 2' },
      ]}
      initialValue="option1"
    />
  ),
};

export const ThreeOptions: Story = {
  render: () => (
    <StatefulWrapper
      options={[
        { value: 'system', label: 'System' },
        { value: 'light', label: 'Light' },
        { value: 'dark', label: 'Dark' },
      ]}
      initialValue="system"
    />
  ),
};

const styles = StyleSheet.create({
  decorator: {
    padding: 16,
    justifyContent: 'center',
    flex: 1,
  },
});
