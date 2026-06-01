import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import type { Meta, StoryObj } from '@storybook/react';
import ServiceEditScreen from './ServiceEditScreen';
import { setServiceMode } from '../services/__mocks__/serviceCatalogService';
import type { SchedulerStackParamList } from '../types/navigation';

const Stack = createNativeStackNavigator<SchedulerStackParamList>();

const meta: Meta<typeof ServiceEditScreen> = {
  title: 'Screens/ServiceEdit',
  component: ServiceEditScreen,
  decorators: [
    (Story) => (
      <NavigationContainer>
        <Stack.Navigator screenOptions={{ headerShown: false }}>
          <Stack.Screen
            name="ServiceEdit"
            component={Story}
            initialParams={{ serviceId: undefined, boardId: 1 }}
          />
        </Stack.Navigator>
      </NavigationContainer>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof ServiceEditScreen>;

export const Create: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Empty service creation form with name, category selector, duration, and price fields.',
      },
    },
  },
};

export const Edit: Story = {
  decorators: [
    (Story) => {
      setServiceMode('success');
      return (
        <NavigationContainer>
          <Stack.Navigator screenOptions={{ headerShown: false }}>
            <Stack.Screen
              name="ServiceEdit"
              component={Story}
              initialParams={{ serviceId: 1, boardId: 1 }}
            />
          </Stack.Navigator>
        </NavigationContainer>
      );
    },
  ],
  parameters: {
    docs: {
      description: {
        story:
          'Edit form pre-filled with existing service data (name, category, duration, price).',
      },
    },
  },
};
