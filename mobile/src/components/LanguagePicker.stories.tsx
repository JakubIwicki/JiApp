import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import LanguagePicker from './LanguagePicker';

const meta: Meta<typeof LanguagePicker> = {
  title: 'LanguagePicker',
  component: LanguagePicker,
  decorators: [
    (Story) => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof LanguagePicker>;

export const PolishSelected: Story = {};

export const EnglishSelected: Story = {};

const styles = StyleSheet.create({
  decorator: {
    padding: 16,
    justifyContent: 'center',
    flex: 1,
  },
});
