import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import ChatInputBar from './ChatInputBar';
import { colors } from '../../styles/theme';

const meta: Meta<typeof ChatInputBar> = {
  title: 'ChatInputBar',
  component: ChatInputBar,
  decorators: [
    Story => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof ChatInputBar>;

export const Default: Story = {
  args: { onSend: () => {} },
};

export const Disabled: Story = {
  args: { onSend: () => {}, disabled: true },
};

const styles = StyleSheet.create({
  decorator: {
    flex: 1,
    backgroundColor: colors.background,
    justifyContent: 'flex-end',
    padding: 8,
  },
});
