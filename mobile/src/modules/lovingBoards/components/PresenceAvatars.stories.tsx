import React from 'react';
import { View } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import PresenceAvatars from './PresenceAvatars';
import { ThemeProvider } from '../../../context/ThemeContext';

const Wrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <ThemeProvider>
    <View style={{ padding: 20, backgroundColor: '#F5F0EB' }}>{children}</View>
  </ThemeProvider>
);

const meta: Meta<typeof PresenceAvatars> = {
  title: 'Modules/LovingBoards/PresenceAvatars',
  component: PresenceAvatars,
  decorators: [
    Story => (
      <Wrapper>
        <Story />
      </Wrapper>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof PresenceAvatars>;

export const SingleUser: Story = {
  args: { userIds: [42] },
};

export const TwoUsers: Story = {
  args: { userIds: [7, 93] },
};

export const MaxVisible: Story = {
  args: { userIds: [5, 12, 28] },
};

export const Overflow: Story = {
  args: { userIds: [1, 10, 100, 77, 31] },
};

export const Empty: Story = {
  args: { userIds: [] },
};
