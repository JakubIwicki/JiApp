import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import ChatBubble from './ChatBubble';
import type { ChatMessage } from '../../types/chat';
import { colors } from '../../styles/theme';

const meta: Meta<typeof ChatBubble> = {
  title: 'ChatBubble',
  component: ChatBubble,
  decorators: [
    Story => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof ChatBubble>;

const assistantMsg: ChatMessage = {
  id: 'msg-1',
  role: 'assistant',
  text: 'I found a great lofi playlist for you.',
};

const userMsg: ChatMessage = {
  id: 'msg-2',
  role: 'user',
  text: 'Thanks, that looks perfect!',
};

const streamingMsg: ChatMessage = {
  id: 'msg-3',
  role: 'assistant',
  text: 'Let me search for',
  pending: true,
};

const typingMsg: ChatMessage = {
  id: 'msg-4',
  role: 'assistant',
  text: '',
  pending: true,
  toolSteps: [],
};

export const Assistant: Story = {
  args: { message: assistantMsg },
};

export const User: Story = {
  args: { message: userMsg },
};

export const Streaming: Story = {
  args: { message: streamingMsg },
};

export const Typing: Story = {
  args: { message: typingMsg },
};

export const LongMessage: Story = {
  args: {
    message: {
      id: 'msg-5',
      role: 'assistant',
      text: 'This is a very long message that demonstrates how the chat bubble handles multi-line content gracefully, wrapping naturally within the 82% max width constraint while maintaining the asymmetric corner radii.',
    },
  },
};

const styles = StyleSheet.create({
  decorator: {
    flex: 1,
    backgroundColor: colors.background,
    padding: 8,
  },
});
