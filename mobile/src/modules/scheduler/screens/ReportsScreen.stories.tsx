import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import type { Meta, StoryObj } from '@storybook/react';
import ReportsScreen from './ReportsScreen';
import { setReportMode } from '../services/__mocks__/reportService';
import type { SchedulerStackParamList } from '../types/navigation';

const Stack = createNativeStackNavigator<SchedulerStackParamList>();

const meta: Meta<typeof ReportsScreen> = {
  title: 'Screens/Reports',
  component: ReportsScreen,
  decorators: [
    (Story) => {
      setReportMode('success');
      return (
        <NavigationContainer>
          <Stack.Navigator screenOptions={{ headerShown: false }}>
            <Stack.Screen
              name="Reports"
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

type Story = StoryObj<typeof ReportsScreen>;

export const Revenue: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Revenue tab showing grouped report data. Use the Group chips (weekend, service, location, client) to re-group the data.',
      },
    },
  },
};

export const Clients: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Client analytics tab. Click the "Clients" tab to view client-level data sorted by visit count, total spent, or last visit.',
      },
    },
  },
};
