import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import type { Meta, StoryObj } from '@storybook/react';
import BoardMembersScreen from './BoardMembersScreen';
import { setBoardMode, setBoards } from '../services/__mocks__/boardService';
import type { Board } from '../types/api';
import type { LovingBoardsStackParamList } from '../../../navigation/types';

const Stack = createNativeStackNavigator<LovingBoardsStackParamList>();

const now = new Date().toISOString();

const sampleBoard: Board = {
  id: 1,
  name: 'Weekly groceries',
  ownerUserId: 1,
  memberUserIds: [1, 2, 3],
  createdAt: now,
  items: [],
};

const meta: Meta<typeof BoardMembersScreen> = {
  title: 'Screens/LovingBoards/BoardMembers',
  component: BoardMembersScreen,
  decorators: [
    Story => (
      <NavigationContainer>
        <Stack.Navigator screenOptions={{ headerShown: false }}>
          <Stack.Screen
            name="BoardMembers"
            component={Story}
            initialParams={{ boardId: 1 }}
          />
        </Stack.Navigator>
      </NavigationContainer>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof BoardMembersScreen>;

export const WithMembers: Story = {
  decorators: [
    Story => {
      setBoardMode('success');
      setBoards([sampleBoard]);
      return <Story />;
    },
  ],
  parameters: {
    docs: {
      description: {
        story:
          'Shows current members with owner label. Add/remove via ID input.',
      },
    },
  },
};
