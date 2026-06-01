import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import type { Meta, StoryObj } from '@storybook/react';
import AppointmentDetailScreen from './AppointmentDetailScreen';
import type { SchedulerStackParamList } from '../types/navigation';

const Stack = createNativeStackNavigator<SchedulerStackParamList>();

const meta: Meta<typeof AppointmentDetailScreen> = {
  title: 'Screens/AppointmentDetail',
  component: AppointmentDetailScreen,
  decorators: [
    (Story) => (
      <NavigationContainer>
        <Stack.Navigator screenOptions={{ headerShown: false }}>
          <Stack.Screen name="AppointmentDetail" component={Story} />
        </Stack.Navigator>
      </NavigationContainer>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof AppointmentDetailScreen>;

export const Created: Story = {
  decorators: [
    (Story) => (
      <NavigationContainer>
        <Stack.Navigator screenOptions={{ headerShown: false }}>
          <Stack.Screen
            name="AppointmentDetail"
            component={Story}
            initialParams={{ appointmentId: 1 }}
          />
        </Stack.Navigator>
      </NavigationContainer>
    ),
  ],
  parameters: {
    docs: {
      description: {
        story:
          'Appointment in Created status with client info, service details, and action buttons (Mark Done, Cancel, Delete).',
      },
    },
  },
};

export const Done: Story = {
  decorators: [
    (Story) => (
      <NavigationContainer>
        <Stack.Navigator screenOptions={{ headerShown: false }}>
          <Stack.Screen
            name="AppointmentDetail"
            component={Story}
            initialParams={{ appointmentId: 3 }}
          />
        </Stack.Navigator>
      </NavigationContainer>
    ),
  ],
  parameters: {
    docs: {
      description: {
        story:
          'Appointment marked as Done. Only the Delete action is available.',
      },
    },
  },
};
