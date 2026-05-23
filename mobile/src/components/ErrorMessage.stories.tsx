import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import ErrorMessage from './ErrorMessage';

const meta: Meta<typeof ErrorMessage> = {
  title: 'ErrorMessage',
  component: ErrorMessage,
  decorators: [
    (Story) => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof ErrorMessage>;

export const WithRetry: Story = {
  args: {
    message: 'Something went wrong while loading your data. Please try again.',
    onRetry: () => console.log('Retry pressed'),
  },
};

export const WithoutRetry: Story = {
  args: {
    message: 'Something went wrong while loading your data. Please try again.',
    onRetry: undefined,
  },
};

const styles = StyleSheet.create({
  decorator: {
    padding: 16,
    justifyContent: 'center',
    flex: 1,
  },
});
