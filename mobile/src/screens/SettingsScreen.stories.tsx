import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import type { Meta, StoryObj } from '@storybook/react';
import SettingsScreen from './SettingsScreen';
import { AuthContext } from '../context/AuthContext';
import type { MainStackParamList, ModuleId } from '../navigation/types';

const Stack = createNativeStackNavigator<MainStackParamList>();

const availableModules: ModuleId[] = ['YtDownloader', 'Scheduler'];

const mockAuthValue = {
  token: 'mock-jwt-token',
  userId: 1,
  displayName: 'John Doe',
  username: 'johndoe',
  availableModules,
  isLoading: false,
  showWelcome: false,
  showFarewell: false,
  login: async () => {},
  register: async () => {},
  logout: async () => {},
  checkToken: async () => {},
  dismissWelcome: () => {},
  dismissFarewell: () => {},
  updateProfile: async () => {},
};

const meta: Meta<typeof SettingsScreen> = {
  title: 'Screens/SettingsScreen',
  component: SettingsScreen,
  decorators: [
    Story => (
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
