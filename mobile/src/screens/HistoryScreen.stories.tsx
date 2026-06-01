import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import type { Meta, StoryObj } from '@storybook/react';
import HistoryScreen from './HistoryScreen';
import type { MainStackParamList } from '../navigation/types';
import { setHistoryMode } from '../services/__mocks__/historyService';

const Stack = createNativeStackNavigator<MainStackParamList>();

const meta: Meta<typeof HistoryScreen> = {
  title: 'Screens/HistoryScreen',
  component: HistoryScreen,
  decorators: [
    (Story) => (
      <NavigationContainer>
        <Stack.Navigator screenOptions={{ headerShown: false }}>
          <Stack.Screen name="History" component={Story} />
        </Stack.Navigator>
      </NavigationContainer>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof HistoryScreen>;

export const WithItems: Story = {
  decorators: [
    (Story) => {
      setHistoryMode('success');
      return <Story />;
    },
  ],
};

export const Empty: Story = {
  decorators: [
    (Story) => {
      setHistoryMode('empty');
      return <Story />;
    },
  ],
};

export const WithError: Story = {
  decorators: [
    (Story) => {
      setHistoryMode('error');
      return <Story />;
    },
  ],
};
