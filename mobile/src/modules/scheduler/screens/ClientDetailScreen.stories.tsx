import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import type { Meta, StoryObj } from '@storybook/react';
import ClientDetailScreen from './ClientDetailScreen';
import type { SchedulerStackParamList } from '../types/navigation';

const Stack = createNativeStackNavigator<SchedulerStackParamList>();

const meta: Meta<typeof ClientDetailScreen> = {
  title: 'Screens/ClientDetail',
  component: ClientDetailScreen,
  decorators: [
    (Story) => (
      <NavigationContainer>
        <Stack.Navigator screenOptions={{ headerShown: false }}>
          <Stack.Screen
            name="ClientDetail"
            component={Story}
            initialParams={{ clientId: 1 }}
          />
        </Stack.Navigator>
      </NavigationContainer>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof ClientDetailScreen>;

export const WithHistory: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Client detail screen showing client info header and appointment history list with status badges.',
      },
    },
  },
};
