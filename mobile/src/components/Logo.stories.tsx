import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import Logo from './Logo';

const meta: Meta<typeof Logo> = {
  title: 'Logo',
  component: Logo,
  decorators: [
    (Story) => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof Logo>;

export const Default: Story = {
  args: {},
};

export const Large: Story = {
  args: { size: 120 },
};

export const ExtraLarge: Story = {
  args: { size: 160 },
};

const styles = StyleSheet.create({
  decorator: {
    padding: 16,
    justifyContent: 'center',
    flex: 1,
    backgroundColor: '#F5F0EB',
  },
});
