import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import HistoryItem from './HistoryItem';

const meta: Meta<typeof HistoryItem> = {
  title: 'HistoryItem',
  component: HistoryItem,
  decorators: [
    (Story) => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof HistoryItem>;

const searchItem = {
  id: 1,
  searchText: 'never gonna give you up',
  searchedAt: '2026-05-20T10:00:00Z',
};

const downloadItem = {
  id: 1,
  videoTitle: 'Rick Astley - Never Gonna Give You Up',
  videoDescription:
    'The official video for "Never Gonna Give You Up" by Rick Astley.',
  videoId: 'dQw4w9WgXcQ',
  videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
  imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/maxresdefault.jpg',
  downloadedAt: '2026-05-20T10:00:00Z',
};

export const SearchType: Story = {
  args: {
    type: 'search',
    item: searchItem,
  },
};

export const DownloadType: Story = {
  args: {
    type: 'download',
    item: downloadItem,
    onPress: (item) => console.log('Pressed:', item.videoTitle),
  },
};

export const DownloadTypeMissingThumbnail: Story = {
  args: {
    type: 'download',
    item: { ...downloadItem, imageUrl: '' },
    onPress: (item) => console.log('Pressed:', item.videoTitle),
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
