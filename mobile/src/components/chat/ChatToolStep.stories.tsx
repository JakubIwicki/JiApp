import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import ChatToolStep from './ChatToolStep';
import type { ToolStep } from '../../types/chat';
import { colors } from '../../styles/theme';

const meta: Meta<typeof ChatToolStep> = {
  title: 'ChatToolStep',
  component: ChatToolStep,
  decorators: [
    Story => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof ChatToolStep>;

const runningStep: ToolStep = {
  tool: 'search_youtube',
  status: 'running',
};

const doneStep: ToolStep = {
  tool: 'search_youtube',
  status: 'done',
};

const runningOffer: ToolStep = {
  tool: 'offer_download',
  status: 'running',
};

const doneOffer: ToolStep = {
  tool: 'offer_download',
  status: 'done',
};

export const Running: Story = {
  args: { step: runningStep },
};

export const Done: Story = {
  args: { step: doneStep },
};

export const OfferRunning: Story = {
  args: { step: runningOffer },
};

export const OfferDone: Story = {
  args: { step: doneOffer },
};

const styles = StyleSheet.create({
  decorator: {
    flex: 1,
    backgroundColor: colors.background,
    padding: 16,
  },
});
