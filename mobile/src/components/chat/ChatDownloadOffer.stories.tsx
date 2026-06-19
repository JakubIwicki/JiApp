import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import ChatDownloadOffer from './ChatDownloadOffer';
import type { DownloadOfferData } from '../../types/chat';
import { colors } from '../../styles/theme';

const meta: Meta<typeof ChatDownloadOffer> = {
  title: 'ChatDownloadOffer',
  component: ChatDownloadOffer,
  decorators: [
    Story => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof ChatDownloadOffer>;

const offer: DownloadOfferData = {
  videoId: 'dQw4w9WgXcQ',
  videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
  title: 'Rick Astley - Never Gonna Give You Up',
  imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/maxresdefault.jpg',
};

export const Idle: Story = {
  args: { offer, status: 'idle', onConfirm: () => {} },
};

export const Downloading: Story = {
  args: { offer, status: 'downloading', onConfirm: () => {} },
};

export const Done: Story = {
  args: { offer, status: 'done', onConfirm: () => {} },
};

export const Error: Story = {
  args: { offer, status: 'error', onConfirm: () => {} },
};

const styles = StyleSheet.create({
  decorator: {
    flex: 1,
    backgroundColor: colors.background,
    padding: 8,
  },
});
