/**
 * @format
 */

import React from 'react';
import { render } from '@testing-library/react-native';

jest.mock('react-native-track-player', () => ({
  default: {
    setupPlayer: jest.fn().mockResolvedValue(undefined),
    updateOptions: jest.fn().mockResolvedValue(undefined),
    reset: jest.fn().mockResolvedValue(undefined),
    add: jest.fn().mockResolvedValue(undefined),
    play: jest.fn().mockResolvedValue(undefined),
    pause: jest.fn().mockResolvedValue(undefined),
    addEventListener: jest.fn().mockReturnValue({ remove: jest.fn() }),
  },
  Capability: { Play: 0, Pause: 1, Stop: 2 },
  State: { Playing: 3, Paused: 2, Stopped: 1, Ready: 0 },
  Event: { PlaybackProgressUpdated: 'playback-progress-updated' },
}));

beforeAll(() => {
  jest.useFakeTimers();
});

afterAll(() => {
  jest.useRealTimers();
});

test('renders without crashing', () => {
  const App = require('../App').default;
  const { unmount } = render(<App />);
  unmount();
});
