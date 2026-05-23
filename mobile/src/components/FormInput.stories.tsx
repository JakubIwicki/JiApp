import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import FormInput from './FormInput';

const meta: Meta<typeof FormInput> = {
  title: 'FormInput',
  component: FormInput,
  decorators: [
    (Story) => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof FormInput>;

export const Default: Story = {
  args: {
    value: '',
    onChangeText: () => {},
    placeholder: 'Username',
  },
};

export const Secure: Story = {
  args: {
    value: '',
    onChangeText: () => {},
    placeholder: 'Password',
    secureTextEntry: true,
  },
};

export const WithError: Story = {
  args: {
    value: 'invalid',
    onChangeText: () => {},
    placeholder: 'Username',
    error: 'Username is required',
  },
};

export const WithLabel: Story = {
  args: {
    value: '',
    onChangeText: () => {},
    placeholder: 'Enter your name',
    label: 'Username',
  },
};

const styles = StyleSheet.create({
  decorator: {
    padding: 16,
    justifyContent: 'center',
    flex: 1,
  },
});
