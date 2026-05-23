import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import VideoCard from './VideoCard';

const meta: Meta<typeof VideoCard> = {
  title: 'VideoCard',
  component: VideoCard,
  decorators: [
    (Story) => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof VideoCard>;

const defaultVideo = {
  videoId: 'dQw4w9WgXcQ',
  title: 'Rick Astley - Never Gonna Give You Up (Official Music Video)',
  description:
    'The official video for "Never Gonna Give You Up" by Rick Astley. This classic 80s hit has become a beloved internet meme over the years.',
  imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/maxresdefault.jpg',
  videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
};

export const Default: Story = {
  args: {
    video: defaultVideo,
    onPress: (video) => console.log('Pressed:', video.title),
  },
};

export const LongTitle: Story = {
  args: {
    video: {
      ...defaultVideo,
      title:
        'This is an extremely long video title that should definitely be truncated with an ellipsis after one line because it is way too long to fit on screen',
      description: 'Short description.',
    },
    onPress: (video) => console.log('Pressed:', video.title),
  },
};

export const MissingThumbnail: Story = {
  args: {
    video: {
      ...defaultVideo,
      imageUrl: '',
    },
    onPress: (video) => console.log('Pressed:', video.title),
  },
};

export const NoDescription: Story = {
  args: {
    video: {
      ...defaultVideo,
      description: '',
    },
    onPress: (video) => console.log('Pressed:', video.title),
  },
};

const styles = StyleSheet.create({
  decorator: {
    padding: 8,
    flex: 1,
    backgroundColor: '#F2F2F7',
    justifyContent: 'center',
  },
});
