import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import HistorySection from './HistorySection';
import HistoryItem from './HistoryItem';

const meta: Meta<typeof HistorySection> = {
  title: 'HistorySection',
  component: HistorySection,
  decorators: [
    (Story) => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof HistorySection>;

const searchItems = [
  { id: 1, searchText: 'never gonna give you up', searchedAt: '2026-05-20T10:00:00Z' },
  { id: 2, searchText: 'rick astley', searchedAt: '2026-05-19T08:30:00Z' },
];

const downloadItems = [
  {
    id: 1,
    videoTitle: 'Rick Astley - Never Gonna Give You Up',
    videoDescription: 'The official video.',
    videoId: 'dQw4w9WgXcQ',
    videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
    imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/maxresdefault.jpg',
    downloadedAt: '2026-05-20T10:00:00Z',
  },
];

export const EmptySearches: Story = {
  args: {
    title: 'Searches',
    items: [],
    emptyText: 'No searches yet',
    renderItem: () => null,
    keyExtractor: () => '',
  },
};

export const WithSearchItems: Story = {
  args: {
    title: 'Search History',
    items: searchItems,
    emptyText: 'No searches yet',
    renderItem: (item: any) => <HistoryItem type="search" item={item} />,
    keyExtractor: (item: any) => String(item.id),
  },
};

export const WithDownloadItems: Story = {
  args: {
    title: 'Download History',
    items: downloadItems,
    emptyText: 'No downloads yet',
    renderItem: (item: any) => <HistoryItem type="download" item={item} />,
    keyExtractor: (item: any) => String(item.id),
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
