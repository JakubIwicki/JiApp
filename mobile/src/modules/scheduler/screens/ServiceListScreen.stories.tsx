import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import type { Meta, StoryObj } from '@storybook/react';
import ServiceListScreen from './ServiceListScreen';
import { setServiceMode } from '../services/__mocks__/serviceCatalogService';
import type { SchedulerStackParamList } from '../types/navigation';

const Stack = createNativeStackNavigator<SchedulerStackParamList>();

const meta: Meta<typeof ServiceListScreen> = {
  title: 'Screens/ServiceList',
  component: ServiceListScreen,
  decorators: [
    (Story) => {
      setServiceMode('success');
      return (
        <NavigationContainer>
          <Stack.Navigator screenOptions={{ headerShown: false }}>
            <Stack.Screen name="ServiceList" component={Story} />
          </Stack.Navigator>
        </NavigationContainer>
      );
    },
  ],
};

export default meta;

type Story = StoryObj<typeof ServiceListScreen>;

export const WithServices: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Service list grouped by category (MensHaircut, WomensHaircut, WomensStyling, Coloring, Treatment). Shows service name, duration, and price.',
      },
    },
  },
};
