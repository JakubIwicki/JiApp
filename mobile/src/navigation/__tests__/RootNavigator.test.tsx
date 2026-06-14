import React from 'react';
import { render, waitFor, fireEvent } from '@testing-library/react-native';
import { NavigationContainer } from '@react-navigation/native';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { AuthContext } from '../../context/AuthContext';
import type { ModuleId } from '../types';
import RootNavigator from '../RootNavigator';

// --- storageService mock (selected_module persistence) ---
let mockSelectedModule: ModuleId | null = null;
const mockSaveSelectedModule = jest.fn();

jest.mock('../../services/storageService', () => ({
  getSelectedModule: () => Promise.resolve(mockSelectedModule),
  saveSelectedModule: (...args: unknown[]) => {
    mockSaveSelectedModule(...args);
    return Promise.resolve();
  },
}));

// --- module navigators replaced with simple markers ---
jest.mock('../MainNavigator', () => {
  const ReactMock = require('react');
  const { Text } = require('react-native');
  return {
    __esModule: true,
    default: () => ReactMock.createElement(Text, null, 'YtDownloaderModule'),
  };
});

jest.mock('../SchedulerNavigator', () => {
  const ReactMock = require('react');
  const { Text } = require('react-native');
  return {
    __esModule: true,
    default: () => ReactMock.createElement(Text, null, 'SchedulerModule'),
  };
});

jest.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: { name?: string }) =>
      opts?.name ? `${key}:${opts.name}` : key,
  }),
}));

// react-native-svg used by ModuleSelectionScreen glyphs
jest.mock('react-native-svg', () => {
  const ReactMock = require('react');
  const MockSvg = ({ children, testID, ...props }: Record<string, unknown>) =>
    ReactMock.createElement('View', { testID, ...props }, children);
  const MockShape = (props: Record<string, unknown>) =>
    ReactMock.createElement('View', props);
  return {
    __esModule: true,
    default: MockSvg,
    Svg: MockSvg,
    Circle: MockShape,
    Line: MockShape,
    Path: MockShape,
    Polyline: MockShape,
    Rect: MockShape,
  };
});

const buildAuthValue = (modules: ModuleId[]) => ({
  token: 'mock-token',
  userId: 1,
  displayName: 'Jakub',
  username: 'johndoe',
  availableModules: modules,
  isLoading: false,
  showWelcome: false,
  showFarewell: false,
  login: async () => {},
  register: async () => {},
  logout: async () => {},
  checkToken: async () => {},
  dismissWelcome: () => {},
  dismissFarewell: () => {},
});

const testMetrics = {
  insets: { top: 0, bottom: 0, left: 0, right: 0 },
  frame: { x: 0, y: 0, width: 390, height: 844 },
};

const renderRoot = (modules: ModuleId[]) =>
  render(
    <SafeAreaProvider initialMetrics={testMetrics}>
      <AuthContext.Provider value={buildAuthValue(modules)}>
        <NavigationContainer>
          <RootNavigator />
        </NavigationContainer>
      </AuthContext.Provider>
    </SafeAreaProvider>,
  );

describe('RootNavigator', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockSelectedModule = null;
  });

  it('skips the picker and lands directly in the only granted module', async () => {
    // Arrange: a single granted module
    const { findByText, queryByTestId } = renderRoot(['Scheduler']);

    // Assert: the Scheduler module renders, picker is absent
    expect(await findByText('SchedulerModule')).toBeTruthy();
    expect(queryByTestId('module-selection-screen')).toBeNull();
  });

  it('shows the picker on a fresh login with more than one module', async () => {
    // Arrange: two granted modules, no persisted choice
    const { findByTestId } = renderRoot(['YtDownloader', 'Scheduler']);

    // Assert
    expect(await findByTestId('module-selection-screen')).toBeTruthy();
  });

  it('opens the persisted module directly when it is still granted', async () => {
    // Arrange: persisted choice that is still granted
    mockSelectedModule = 'Scheduler';
    const { findByText, queryByTestId } = renderRoot([
      'YtDownloader',
      'Scheduler',
    ]);

    // Assert: jumps straight into the persisted module
    expect(await findByText('SchedulerModule')).toBeTruthy();
    expect(queryByTestId('module-selection-screen')).toBeNull();
  });

  it('ignores a persisted module that is no longer granted', async () => {
    // Arrange: persisted YtDownloader but only Scheduler is now granted
    mockSelectedModule = 'YtDownloader';
    const { findByText, queryByText } = renderRoot(['Scheduler']);

    // Assert: lands in the only granted module, not the stale persisted one
    expect(await findByText('SchedulerModule')).toBeTruthy();
    expect(queryByText('YtDownloaderModule')).toBeNull();
  });

  it('shows the picker when a persisted module is absent and >1 are granted', async () => {
    // Arrange: no persisted choice, two granted modules
    mockSelectedModule = null;
    const { findByTestId } = renderRoot(['YtDownloader', 'Scheduler']);

    // Assert
    expect(await findByTestId('module-selection-screen')).toBeTruthy();
  });

  it('persists and navigates to the chosen module from the picker', async () => {
    // Arrange
    const { findByTestId, getByTestId, findByText } = renderRoot([
      'YtDownloader',
      'Scheduler',
    ]);
    await findByTestId('module-selection-screen');

    // Act: choose YtDownloader
    fireEvent.press(getByTestId('module-card-YtDownloader'));

    // Assert: persisted + navigated
    await waitFor(() => {
      expect(mockSaveSelectedModule).toHaveBeenCalledWith('YtDownloader');
    });
    expect(await findByText('YtDownloaderModule')).toBeTruthy();
  });
});
