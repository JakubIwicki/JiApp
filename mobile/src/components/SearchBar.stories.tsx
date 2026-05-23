import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import SearchBar from './SearchBar';

const meta: Meta<typeof SearchBar> = {
  title: 'SearchBar',
  component: SearchBar,
  decorators: [
    (Story) => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof SearchBar>;

export const Empty: Story = {
  args: {
    onSearch: (query: string) => console.log('Search:', query),
    initialValue: '',
  },
};

export const WithText: Story = {
  args: {
    onSearch: (query: string) => console.log('Search:', query),
    initialValue: 'React Native',
  },
};

export const WithClearButton: Story = {
  args: {
    onSearch: (query: string) => console.log('Search:', query),
    initialValue: 'TypeScript',
  },
};

const styles = StyleSheet.create({
  decorator: {
    padding: 16,
    flex: 1,
    backgroundColor: '#F2F2F7',
  },
});
