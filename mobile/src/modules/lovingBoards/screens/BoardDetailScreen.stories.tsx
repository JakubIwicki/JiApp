import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import type { Meta, StoryObj } from '@storybook/react';
import BoardDetailScreen from './BoardDetailScreen';
import { setBoardMode, setBoards } from '../services/__mocks__/boardService';
import { setItemMode } from '../services/__mocks__/itemService';
import type { Board } from '../types/api';
import type { LovingBoardsStackParamList } from '../../../navigation/types';

const Stack = createNativeStackNavigator<LovingBoardsStackParamList>();

const now = new Date().toISOString();
const tomorrow = new Date(Date.now() + 86400000).toISOString().split('T')[0];
const yesterday = new Date(Date.now() - 86400000).toISOString().split('T')[0];

const sampleBoard: Board = {
  id: 1,
  name: 'Weekly groceries',
  ownerUserId: 1,
  memberUserIds: [1, 2, 3],
  createdAt: now,
  items: [
    {
      id: 1,
      boardId: 1,
      title: 'Milk',
      quantity: '2 liters',
      category: 'Dairy',
      note: 'Lactose-free preferred',
      assigneeUserId: 2,
      expiryDate: `${tomorrow}T00:00:00.000Z`,
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
      title: 'Cheese',
      quantity: '200g',
      category: 'Dairy',
      note: null,
      assigneeUserId: null,
      expiryDate: `${yesterday}T00:00:00.000Z`,
      isRecurring: false,
      status: 'Needed',
      addedByUserId: 2,
      completedByUserId: null,
      createdAt: now,
      updatedAt: now,
      removedAt: null,
    },
    {
      id: 3,
      boardId: 1,
      title: 'Bread',
      quantity: '1 loaf',
      category: 'Bakery',
      note: null,
      assigneeUserId: 1,
      expiryDate: null,
      isRecurring: false,
      status: 'Needed',
      addedByUserId: 1,
      completedByUserId: null,
      createdAt: now,
      updatedAt: now,
      removedAt: null,
    },
    {
      id: 4,
      boardId: 1,
      title: 'Paper towels',
      quantity: '3 rolls',
      category: null,
      note: null,
      assigneeUserId: null,
      expiryDate: null,
      isRecurring: false,
      status: 'Needed',
      addedByUserId: 3,
      completedByUserId: null,
      createdAt: now,
      updatedAt: now,
      removedAt: null,
    },
    {
      id: 5,
      boardId: 1,
      title: 'Eggs',
      quantity: '12 pcs',
      category: 'Dairy',
      note: null,
      assigneeUserId: 3,
      expiryDate: null,
      isRecurring: true,
      status: 'Completed',
      addedByUserId: 1,
      completedByUserId: 2,
      createdAt: now,
      updatedAt: now,
      removedAt: null,
    },
    {
      id: 6,
      boardId: 1,
      title: 'Butter',
      quantity: '250g',
      category: 'Dairy',
      note: null,
      assigneeUserId: null,
      expiryDate: null,
      isRecurring: false,
      status: 'Completed',
      addedByUserId: 2,
      completedByUserId: 3,
      createdAt: now,
      updatedAt: now,
      removedAt: null,
    },
  ],
};

const emptyBoard: Board = {
  id: 2,
  name: 'Empty board',
  ownerUserId: 1,
  memberUserIds: [1],
  createdAt: now,
  items: [],
};

const meta: Meta<typeof BoardDetailScreen> = {
  title: 'Screens/LovingBoards/BoardDetail',
  component: BoardDetailScreen,
  decorators: [
    Story => (
      <NavigationContainer>
        <Stack.Navigator screenOptions={{ headerShown: false }}>
          <Stack.Screen
            name="BoardDetail"
            component={Story}
            initialParams={{ boardId: 1 }}
          />
        </Stack.Navigator>
      </NavigationContainer>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof BoardDetailScreen>;

export const WithItems: Story = {
  decorators: [
    Story => {
      setBoardMode('success');
      setItemMode('success');
      setBoards([sampleBoard]);
      return <Story />;
    },
  ],
  parameters: {
    docs: {
      description: {
        story:
          'Full board with items grouped by category (Dairy, Bakery), uncategorized items, recurring badges, due dates, and a collapsed Done section.',
      },
    },
  },
};

export const Empty: Story = {
  decorators: [
    Story => {
      setBoardMode('success');
      setItemMode('empty');
      setBoards([emptyBoard]);
      return <Story />;
    },
  ],
  parameters: {
    docs: {
      description: {
        story: 'Board with no items — empty state prompting to add first item.',
      },
    },
  },
};
