import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import type { Meta, StoryObj } from '@storybook/react';
import WeekendGridScreen from './WeekendGridScreen';
import { setAppointmentMode } from '../services/__mocks__/appointmentService';
import { setExpenseMode } from '../services/__mocks__/expenseService';
import type { SchedulerStackParamList } from '../types/navigation';

const Stack = createNativeStackNavigator<SchedulerStackParamList>();

const meta: Meta<typeof WeekendGridScreen> = {
  title: 'Screens/WeekendGrid',
  component: WeekendGridScreen,
  decorators: [
    (Story) => (
      <NavigationContainer>
        <Stack.Navigator screenOptions={{ headerShown: false }}>
          <Stack.Screen name="WeekendGrid" component={Story} />
        </Stack.Navigator>
      </NavigationContainer>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof WeekendGridScreen>;

export const WithData: Story = {
  decorators: [
    (Story) => {
      setAppointmentMode('success');
      setExpenseMode('success');
      return <Story />;
    },
  ],
  parameters: {
    docs: {
      description: {
        story:
          'Full weekend grid with appointments and expenses on Saturday and Sunday.',
      },
    },
  },
};

export const Empty: Story = {
  decorators: [
    (Story) => {
      setAppointmentMode('empty');
      setExpenseMode('empty');
      return <Story />;
    },
  ],
  parameters: {
    docs: {
      description: {
        story: 'Weekend with no appointments or expenses scheduled.',
      },
    },
  },
};
