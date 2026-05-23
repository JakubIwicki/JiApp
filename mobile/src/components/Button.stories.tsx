import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import Button from './Button';

const meta: Meta<typeof Button> = {
  title: 'Button',
  component: Button,
  decorators: [
    (Story) => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof Button>;

export const Default: Story = {
  args: {
    title: 'Log In',
    onPress: () => console.log('Pressed'),
  },
};

export const Disabled: Story = {
  args: {
    title: 'Log In',
    onPress: () => console.log('Pressed'),
    disabled: true,
  },
};

export const Loading: Story = {
  args: {
    title: 'Log In',
    onPress: () => console.log('Pressed'),
    loading: true,
  },
};

const styles = StyleSheet.create({
  decorator: {
    padding: 16,
    justifyContent: 'center',
    flex: 1,
  },
});
