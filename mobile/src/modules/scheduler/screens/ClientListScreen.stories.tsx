import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import type { Meta, StoryObj } from '@storybook/react';
import ClientListScreen from './ClientListScreen';
import { setClientMode } from '../services/__mocks__/clientService';
import type { SchedulerStackParamList } from '../types/navigation';

const Stack = createNativeStackNavigator<SchedulerStackParamList>();

const meta: Meta<typeof ClientListScreen> = {
  title: 'Screens/ClientList',
  component: ClientListScreen,
  decorators: [
    (Story) => (
      <NavigationContainer>
        <Stack.Navigator screenOptions={{ headerShown: false }}>
          <Stack.Screen name="ClientList" component={Story} />
        </Stack.Navigator>
      </NavigationContainer>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof ClientListScreen>;

export const WithClients: Story = {
  decorators: [
    (Story) => {
      setClientMode('success');
      return <Story />;
    },
  ],
  parameters: {
    docs: {
      description: {
        story:
          'Client list with alphabetically grouped sections. Shows search bar and FAB to add new client.',
      },
    },
  },
};

export const Empty: Story = {
  decorators: [
    (Story) => {
      setClientMode('empty');
      return <Story />;
    },
  ],
  parameters: {
    docs: {
      description: {
        story: 'Empty client list with no clients yet. Shows empty state message and FAB.',
      },
    },
  },
};
