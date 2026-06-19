import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import ChatVideoResults from './ChatVideoResults';
import type { VideoItem } from '../../types/api';
import { colors } from '../../styles/theme';

const meta: Meta<typeof ChatVideoResults> = {
  title: 'ChatVideoResults',
  component: ChatVideoResults,
  decorators: [
    Story => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof ChatVideoResults>;

const videos: VideoItem[] = [
  {
    videoId: 'dQw4w9WgXcQ',
    title: 'Rick Astley - Never Gonna Give You Up',
    description: 'The official video for Never Gonna Give You Up',
    imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/maxresdefault.jpg',
    videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
    channelTitle: 'Rick Astley',
  },
  {
    videoId: '9bZkp7q19f0',
    title: 'PSY - GANGNAM STYLE',
    description: 'The global hit that took over the world',
    imageUrl: 'https://i.ytimg.com/vi/9bZkp7q19f0/maxresdefault.jpg',
    videoUrl: 'https://www.youtube.com/watch?v=9bZkp7q19f0',
    channelTitle: 'officialpsy',
  },
];

export const MultipleVideos: Story = {
  args: { videos, onSelect: () => {} },
};

export const SingleVideo: Story = {
  args: { videos: [videos[0]], onSelect: () => {} },
};

export const Empty: Story = {
  args: { videos: [], onSelect: () => {} },
};

const styles = StyleSheet.create({
  decorator: {
    flex: 1,
    backgroundColor: colors.background,
    padding: 8,
  },
});
