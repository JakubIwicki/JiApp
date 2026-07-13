import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import type { Meta, StoryObj } from '@storybook/react';
import LoginScreen from './LoginScreen';
import { AuthContext } from '../context/AuthContext';
import {
  login as mockLogin,
  withLoginFailure,
} from '../services/__mocks__/authService';
import type { AuthStackParamList } from '../navigation/types';

const Stack = createNativeStackNavigator<AuthStackParamList>();

const mockAuthValue = {
  token: null,
  userId: null,
  displayName: null,
  username: null,
  roles: [],
  permissions: [],
  availableModules: [],
  isLoading: false,
  showWelcome: false,
  showFarewell: false,
  isAdmin: false,
  login: async (username: string, password: string) => {
    await mockLogin(username, password);
  },
  register: async () => {},
  logout: async () => {},
  checkToken: async () => {},
  dismissWelcome: () => {},
  dismissFarewell: () => {},
  updateProfile: async () => {},
};

const meta: Meta<typeof LoginScreen> = {
  title: 'Screens/LoginScreen',
  component: LoginScreen,
  decorators: [
    Story => (
      <AuthContext.Provider value={mockAuthValue}>
        <NavigationContainer>
          <Stack.Navigator screenOptions={{ headerShown: false }}>
            <Stack.Screen name="Login" component={Story} />
          </Stack.Navigator>
        </NavigationContainer>
      </AuthContext.Provider>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof LoginScreen>;

export const Default: Story = {};

export const WithError: Story = {
  decorators: [
    Story => {
      withLoginFailure(new Error('Invalid credentials'));
      return <Story />;
    },
  ],
  parameters: {
    docs: {
      description: {
        story:
          'Mock auth service configured to return errors. Fill in the form and click "Log In" to see the error message.',
      },
    },
  },
};
