import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createStackNavigator } from '@react-navigation/stack';
import type { Meta, StoryObj } from '@storybook/react';
import SettingsScreen from './SettingsScreen';
import { AuthContext } from '../context/AuthContext';
import type { MainStackParamList } from '../navigation/types';

const Stack = createStackNavigator<MainStackParamList>();

const mockAuthValue = {
  token: 'mock-jwt-token',
  userId: 1,
  displayName: 'John Doe',
  username: 'johndoe',
  isLoading: false,
  login: async () => {},
  register: async () => {},
  logout: async () => {},
  checkToken: async () => {},
};

const meta: Meta<typeof SettingsScreen> = {
  title: 'Screens/SettingsScreen',
  component: SettingsScreen,
  decorators: [
    (Story) => (
      <AuthContext.Provider value={mockAuthValue}>
        <NavigationContainer>
          <Stack.Navigator screenOptions={{ headerShown: false }}>
            <Stack.Screen name="Settings" component={Story} />
          </Stack.Navigator>
        </NavigationContainer>
      </AuthContext.Provider>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof SettingsScreen>;

export const Default: Story = {};
