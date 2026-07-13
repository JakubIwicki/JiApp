import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import type { Meta, StoryObj } from '@storybook/react';
import RegisterScreen from './RegisterScreen';
import { AuthContext } from '../context/AuthContext';
import {
  register as mockRegister,
  withRegisterFailure,
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
  login: async () => {},
  register: async (
    username: string,
    email: string,
    password: string,
    displayName: string,
  ) => {
    await mockRegister(username, email, password, displayName);
  },
  logout: async () => {},
  checkToken: async () => {},
  dismissWelcome: () => {},
  dismissFarewell: () => {},
  updateProfile: async () => {},
};

const meta: Meta<typeof RegisterScreen> = {
  title: 'Screens/RegisterScreen',
  component: RegisterScreen,
  decorators: [
    Story => (
      <AuthContext.Provider value={mockAuthValue}>
        <NavigationContainer>
          <Stack.Navigator screenOptions={{ headerShown: false }}>
            <Stack.Screen name="Register" component={Story} />
          </Stack.Navigator>
        </NavigationContainer>
      </AuthContext.Provider>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof RegisterScreen>;

export const Default: Story = {};

export const WithError: Story = {
  decorators: [
    Story => {
      withRegisterFailure();
      return <Story />;
    },
  ],
  parameters: {
    docs: {
      description: {
        story:
          'Mock auth service configured to return errors. Fill in the form and click "Sign Up" to see the error message.',
      },
    },
  },
};
