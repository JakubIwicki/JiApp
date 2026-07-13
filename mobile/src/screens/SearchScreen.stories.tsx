import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import type { Meta, StoryObj } from '@storybook/react';
import SearchScreen from './SearchScreen';
import type { MainStackParamList } from '../navigation/types';
import {
  withSearchResults,
  withEmptySearchResults,
  withSearchFailure,
} from '../services/__mocks__/searchService';

const Stack = createNativeStackNavigator<MainStackParamList>();

const meta: Meta<typeof SearchScreen> = {
  title: 'Screens/SearchScreen',
  component: SearchScreen,
  decorators: [
    Story => (
      <NavigationContainer>
        <Stack.Navigator screenOptions={{ headerShown: false }}>
          <Stack.Screen name="Search" component={Story} />
        </Stack.Navigator>
      </NavigationContainer>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof SearchScreen>;

export const WithResults: Story = {
  decorators: [
    Story => {
      withSearchResults();
      return <Story />;
    },
  ],
  parameters: {
    docs: {
      description: {
        story:
          'Shows recent searches (loaded on mount). Type a query in the search bar to see mock video results.',
      },
    },
  },
};

export const EmptyResults: Story = {
  decorators: [
    Story => {
      withEmptySearchResults();
      return <Story />;
    },
  ],
  parameters: {
    docs: {
      description: {
        story:
          'Mock configured to return empty results. Type a query to see the "no results" empty state.',
      },
    },
  },
};

export const WithError: Story = {
  decorators: [
    Story => {
      withSearchFailure();
      return <Story />;
    },
  ],
  parameters: {
    docs: {
      description: {
        story:
          'Mock service in error mode. Recent searches will silently fail (hidden). Type a query to see the error message with retry button.',
      },
    },
  },
};
