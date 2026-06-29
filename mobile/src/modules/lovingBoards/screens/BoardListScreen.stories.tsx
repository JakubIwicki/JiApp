import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import type { Meta, StoryObj } from '@storybook/react';
import BoardListScreen from './BoardListScreen';
import { setBoardMode, setBoards } from '../services/__mocks__/boardService';
import { setItemMode } from '../services/__mocks__/itemService';
import type { Board } from '../types/api';
import type { LovingBoardsStackParamList } from '../../../navigation/types';

const Stack = createNativeStackNavigator<LovingBoardsStackParamList>();

const now = new Date().toISOString();

const sampleBoards: Board[] = [
  {
    id: 1,
    name: 'Weekly groceries',
    ownerUserId: 1,
    memberUserIds: [1, 2],
    createdAt: now,
    items: [
      {
        id: 1,
        boardId: 1,
        title: 'Milk',
        quantity: '2 liters',
        category: 'Dairy',
        note: null,
        assigneeUserId: 2,
        expiryDate: null,
        isRecurring: true,
        status: 'Needed',
        addedByUserId: 1,
        completedByUserId: null,
        createdAt: now,
        updatedAt: now,
        removedAt: null,
      },
      {
        id: 2,
        boardId: 1,
        title: 'Bread',
        quantity: '1 loaf',
        category: 'Bakery',
        note: null,
        assigneeUserId: null,
        expiryDate: null,
        isRecurring: false,
        status: 'Needed',
        addedByUserId: 2,
        completedByUserId: null,
        createdAt: now,
        updatedAt: now,
        removedAt: null,
      },
    ],
  },
  {
    id: 2,
    name: 'Home improvement',
    ownerUserId: 1,
    memberUserIds: [1],
    createdAt: now,
    items: [],
  },
];

const meta: Meta<typeof BoardListScreen> = {
  title: 'Screens/LovingBoards/BoardList',
  component: BoardListScreen,
  decorators: [
    Story => (
      <NavigationContainer>
        <Stack.Navigator screenOptions={{ headerShown: false }}>
          <Stack.Screen name="BoardList" component={Story} />
        </Stack.Navigator>
      </NavigationContainer>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof BoardListScreen>;

export const WithBoards: Story = {
  decorators: [
    Story => {
      setBoardMode('success');
      setBoards(sampleBoards);
      return <Story />;
    },
  ],
  parameters: {
    docs: {
      description: {
        story:
          'Several boards with members and item counts. Tap a board to navigate to its detail.',
      },
    },
  },
};

export const Empty: Story = {
  decorators: [
    Story => {
      setBoardMode('empty');
      setBoards([]);
      return <Story />;
    },
  ],
  parameters: {
    docs: {
      description: {
        story: 'No boards yet — empty state with FAB to create.',
      },
    },
  },
};

export const Loading: Story = {
  decorators: [
    Story => {
      // Keep it in a state that triggers loading — empty boards with initial load
      setBoardMode('empty');
      setBoards([]);
      return <Story />;
    },
  ],
  parameters: {
    docs: {
      description: {
        story: 'Initial load while the board list is being fetched.',
      },
    },
  },
};
