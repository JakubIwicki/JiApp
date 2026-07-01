import React from 'react';
import type { Meta, StoryObj } from '@storybook/react';
import ModuleSelectionScreen from './ModuleSelectionScreen';
import { AuthContext } from '../context/AuthContext';
import type { ModuleId } from '../navigation/types';

interface MockAuthOptions {
  modules: ModuleId[];
  displayName: string | null;
}

const buildAuthValue = ({ modules, displayName }: MockAuthOptions) => ({
  token: 'mock-token',
  userId: 1,
  displayName,
  username: 'johndoe',
  roles: [],
  permissions: [],
  availableModules: modules,
  isLoading: false,
  showWelcome: false,
  showFarewell: false,
  isAdmin: false,
  login: async () => {},
  register: async () => {},
  logout: async () => {},
  checkToken: async () => {},
  dismissWelcome: () => {},
  dismissFarewell: () => {},
  updateProfile: async () => {},
});

const withAuth = (options: MockAuthOptions) => (Story: React.FC) =>
  (
    <AuthContext.Provider value={buildAuthValue(options)}>
      <Story />
    </AuthContext.Provider>
  );

const meta: Meta<typeof ModuleSelectionScreen> = {
  title: 'Screens/ModuleSelection',
  component: ModuleSelectionScreen,
  args: {
    onSelectModule: () => {},
  },
};

export default meta;

type Story = StoryObj<typeof ModuleSelectionScreen>;

export const BothModules: Story = {
  decorators: [
    withAuth({ modules: ['YtDownloader', 'Scheduler'], displayName: 'Jakub' }),
  ],
  parameters: {
    docs: {
      description: {
        story:
          'User granted both modules. Two tactile cards with a staggered entrance.',
      },
    },
  },
};

export const SingleModule: Story = {
  decorators: [withAuth({ modules: ['Scheduler'], displayName: 'Anna' })],
  parameters: {
    docs: {
      description: {
        story: 'User granted only the Appointments module — a single card.',
      },
    },
  },
};

export const LongDisplayName: Story = {
  decorators: [
    withAuth({
      modules: ['YtDownloader', 'Scheduler'],
      displayName: 'Aleksandra-Katarzyna Brzęczyszczykiewicz',
    }),
  ],
  parameters: {
    docs: {
      description: {
        story: 'Long display name wraps gracefully in the greeting header.',
      },
    },
  },
};

export const NoDisplayName: Story = {
  decorators: [
    withAuth({ modules: ['YtDownloader', 'Scheduler'], displayName: null }),
  ],
  parameters: {
    docs: {
      description: {
        story: 'Falls back to a generic greeting when no display name is set.',
      },
    },
  },
};
