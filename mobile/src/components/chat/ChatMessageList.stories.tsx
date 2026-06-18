import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import ChatMessageList from './ChatMessageList';
import type { ChatMessage } from '../../types/chat';
import type { VideoItem } from '../../types/api';
import { colors } from '../../styles/theme';

const meta: Meta<typeof ChatMessageList> = {
  title: 'ChatMessageList',
  component: ChatMessageList,
  decorators: [
    Story => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof ChatMessageList>;

const sampleVideo: VideoItem = {
  videoId: 'dQw4w9WgXcQ',
  title: 'Rick Astley - Never Gonna Give You Up',
  description: 'Official video',
  imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/default.jpg',
  videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
  channelTitle: 'Rick Astley',
};

const chatMessages: ChatMessage[] = [
  {
    id: 'msg-1',
    role: 'user',
    text: 'Find me a chill lofi track',
  },
  {
    id: 'msg-2',
    role: 'assistant',
    text: '',
    pending: false,
    toolSteps: [{ tool: 'search_youtube', status: 'done' }],
    videos: [sampleVideo],
    offer: {
      videoId: 'dQw4w9WgXcQ',
      videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
      title: 'Rick Astley - Never Gonna Give You Up',
      imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/default.jpg',
    },
  },
];

export const WithConversation: Story = {
  args: {
    messages: chatMessages,
    onSelectVideo: () => {},
    onConfirmDownload: () => {},
  },
};

export const Empty: Story = {
  args: {
    messages: [],
  },
};

const styles = StyleSheet.create({
  decorator: {
    flex: 1,
    backgroundColor: colors.background,
    padding: 8,
  },
});
