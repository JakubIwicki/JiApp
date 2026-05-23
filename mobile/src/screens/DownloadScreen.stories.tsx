import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createStackNavigator } from '@react-navigation/stack';
import type { Meta, StoryObj } from '@storybook/react';
import DownloadScreen from './DownloadScreen';
import type { MainStackParamList } from '../navigation/types';
import { setDownloadMode } from '../services/__mocks__/downloadService';

const Stack = createStackNavigator<MainStackParamList>();

const mockVideo = {
  videoId: 'dQw4w9WgXcQ',
  title: 'Rick Astley - Never Gonna Give You Up',
  description:
    "The official video for Rick Astley's classic hit. Never gonna give you up, never gonna let you down, never gonna run around and desert you.",
  imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg',
  videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
};

const meta: Meta<typeof DownloadScreen> = {
  title: 'Screens/DownloadScreen',
  component: DownloadScreen,
  decorators: [
    (Story) => (
      <NavigationContainer>
        <Stack.Navigator screenOptions={{ headerShown: false }}>
          <Stack.Screen
            name="Download"
            component={Story}
            initialParams={mockVideo}
          />
        </Stack.Navigator>
      </NavigationContainer>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof DownloadScreen>;

export const Ready: Story = {
  decorators: [
    (Story) => {
      setDownloadMode('success');
      return <Story />;
    },
  ],
  parameters: {
    docs: {
      description: {
        story:
          'Shows video info and a "Download MP3" button. Click the button to see the download success state.',
      },
    },
  },
};

export const Error: Story = {
  decorators: [
    (Story) => {
      setDownloadMode('error');
      return <Story />;
    },
  ],
  parameters: {
    docs: {
      description: {
        story:
          'Mock download service configured to fail. Click "Download MP3" to see the error message with a retry button.',
      },
    },
  },
};
