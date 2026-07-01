import React from 'react';
import { render, fireEvent } from '@testing-library/react-native';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { AuthContext } from '../../context/AuthContext';
import type { ModuleId } from '../../navigation/types';
import ModuleSelectionScreen from '../ModuleSelectionScreen';

const testMetrics = {
  insets: { top: 0, bottom: 0, left: 0, right: 0 },
  frame: { x: 0, y: 0, width: 390, height: 844 },
};

jest.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: { name?: string }) =>
      opts?.name ? `${key}:${opts.name}` : key,
  }),
}));

// Mock react-native-svg so the line glyphs render as host views in tests
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

const buildAuthValue = (
  modules: ModuleId[],
  displayName: string | null = 'Jakub',
) => ({
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

const renderScreen = (
  modules: ModuleId[],
  onSelectModule = jest.fn(),
  displayName: string | null = 'Jakub',
) =>
  render(
    <SafeAreaProvider initialMetrics={testMetrics}>
      <AuthContext.Provider value={buildAuthValue(modules, displayName)}>
        <ModuleSelectionScreen onSelectModule={onSelectModule} />
      </AuthContext.Provider>
    </SafeAreaProvider>,
  );

describe('ModuleSelectionScreen', () => {
  it('renders a card for every granted module', () => {
    // Arrange + Act
    const { getByTestId } = renderScreen(['YtDownloader', 'Scheduler']);

    // Assert
    expect(getByTestId('module-card-YtDownloader')).toBeTruthy();
    expect(getByTestId('module-card-Scheduler')).toBeTruthy();
  });

  it('renders only granted modules and hides ungranted ones', () => {
    // Arrange + Act: only Scheduler is granted
    const { getByTestId, queryByTestId } = renderScreen(['Scheduler']);

    // Assert
    expect(getByTestId('module-card-Scheduler')).toBeTruthy();
    expect(queryByTestId('module-card-YtDownloader')).toBeNull();
  });

  it('calls onSelectModule with the tapped module id', () => {
    // Arrange
    const onSelectModule = jest.fn();
    const { getByTestId } = renderScreen(
      ['YtDownloader', 'Scheduler'],
      onSelectModule,
    );

    // Precondition: not called before any interaction
    expect(onSelectModule).not.toHaveBeenCalled();

    // Act
    fireEvent.press(getByTestId('module-card-Scheduler'));

    // Assert
    expect(onSelectModule).toHaveBeenCalledWith('Scheduler');
  });

  it('shows a personalised greeting with the display name', () => {
    // Arrange + Act
    const { getByTestId } = renderScreen(['YtDownloader'], jest.fn(), 'Anna');

    // Assert
    expect(getByTestId('module-greeting').props.children).toBe(
      'modules.greeting:Anna',
    );
  });

  it('falls back to a generic greeting when no display name is set', () => {
    // Arrange + Act
    const { getByTestId } = renderScreen(['YtDownloader'], jest.fn(), null);

    // Assert
    expect(getByTestId('module-greeting').props.children).toBe(
      'modules.greetingFallback',
    );
  });
});
