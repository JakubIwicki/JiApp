import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import type { Meta, StoryObj } from '@storybook/react';
import CreateAppointmentScreen from './CreateAppointmentScreen';
import { withAppointments } from '../services/__mocks__/appointmentService';
import { withClients } from '../services/__mocks__/clientService';
import { withServices } from '../services/__mocks__/serviceCatalogService';
import type { SchedulerStackParamList } from '../types/navigation';

const Stack = createNativeStackNavigator<SchedulerStackParamList>();

const meta: Meta<typeof CreateAppointmentScreen> = {
  title: 'Screens/CreateAppointment',
  component: CreateAppointmentScreen,
  decorators: [
    Story => {
      withAppointments();
      withClients();
      withServices();
      return (
        <NavigationContainer>
          <Stack.Navigator screenOptions={{ headerShown: false }}>
            <Stack.Screen
              name="CreateAppointment"
              component={Story}
              initialParams={{ boardId: 1 }}
            />
          </Stack.Navigator>
        </NavigationContainer>
      );
    },
  ],
};

export default meta;

type Story = StoryObj<typeof CreateAppointmentScreen>;

export const Default: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Empty appointment creation form with client picker, category selector, service list, time inputs, and optional fields.',
      },
    },
  },
};
