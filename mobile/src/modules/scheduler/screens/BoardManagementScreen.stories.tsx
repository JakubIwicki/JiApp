import React from 'react';
import type { Meta, StoryObj } from '@storybook/react';
import BoardManagementScreen from './BoardManagementScreen';
import { BoardContext } from '../../../context/BoardContext';
import type { Board } from '../types/api';

const board = (id: number, name: string, members: number[] = []): Board => ({
  id,
  name,
  memberUserIds: members,
  createdAt: '2026-01-01T00:00:00.000Z',
});

interface MockBoardOptions {
  boards: Board[];
  isLoading?: boolean;
}

const buildBoardValue = ({ boards, isLoading = false }: MockBoardOptions) => ({
  boards,
  selectedBoardId: boards[0]?.id ?? null,
  isLoading,
  error: null,
  switchBoard: async () => {},
  loadBoards: async () => {},
  createBoard: async () => board(99, 'New Board'),
  deleteBoard: async () => {},
  addMember: async () => {},
  removeMember: async () => {},
});

const withBoard = (options: MockBoardOptions) => (Story: React.FC) =>
  (
    <BoardContext.Provider value={buildBoardValue(options)}>
      <Story />
    </BoardContext.Provider>
  );

const meta: Meta<typeof BoardManagementScreen> = {
  title: 'Screens/BoardManagement',
  component: BoardManagementScreen,
};

export default meta;

type Story = StoryObj<typeof BoardManagementScreen>;

export const WithBoards: Story = {
  decorators: [
    withBoard({
      boards: [
        board(1, 'Downtown Salon', [10, 20, 30]),
        board(2, 'Weekend Pop-up', [10]),
      ],
    }),
  ],
  parameters: {
    docs: {
      description: {
        story:
          'Several boards with members. Tap a board to manage its members.',
      },
    },
  },
};

export const Empty: Story = {
  decorators: [withBoard({ boards: [] })],
  parameters: {
    docs: {
      description: {
        story: 'No boards yet — only the create form and an empty state.',
      },
    },
  },
};

export const Loading: Story = {
  decorators: [withBoard({ boards: [], isLoading: true })],
  parameters: {
    docs: {
      description: {
        story: 'Initial load while the board list is being fetched.',
      },
    },
  },
};
