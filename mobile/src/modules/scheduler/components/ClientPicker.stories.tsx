import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import ClientPicker from './ClientPicker';
import type { Client } from '../types/api';

const mockClients: Client[] = [
  { id: 1, name: 'Anna Kowalska', phone: '+48 601 111 222' },
  { id: 2, name: 'Marta Zielinska', phone: '+48 602 222 333' },
  { id: 3, name: 'Piotr Nowak' },
  { id: 4, name: 'Katarzyna Adamczyk', phone: '+48 603 333 444' },
  { id: 5, name: 'Michal Lewandowski', phone: '+48 604 555 666' },
  { id: 6, name: 'Joanna Wisniewska' },
];

const meta: Meta<typeof ClientPicker> = {
  title: 'Scheduler/ClientPicker',
  component: ClientPicker,
  decorators: [
    (Story) => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof ClientPicker>;

export const WithClients: Story = {
  args: {
    clients: mockClients,
    selectedClientId: undefined,
    onSelect: (client) => console.log('Selected client:', client.name),
    onCreateNew: async (name) => {
      console.log('Creating new client:', name);
      return 99;
    },
    isLoading: false,
  },
};

export const Empty: Story = {
  args: {
    clients: [],
    selectedClientId: undefined,
    onSelect: (client) => console.log('Selected client:', client.name),
    onCreateNew: async (name) => {
      console.log('Creating new client:', name);
      return 99;
    },
    isLoading: false,
  },
};

export const Loading: Story = {
  args: {
    clients: [],
    selectedClientId: undefined,
    onSelect: (client) => console.log('Selected client:', client.name),
    onCreateNew: async (name) => {
      console.log('Creating new client:', name);
      return 99;
    },
    isLoading: true,
  },
};

const styles = StyleSheet.create({
  decorator: {
    padding: 16,
    justifyContent: 'center',
    flex: 1,
  },
});
