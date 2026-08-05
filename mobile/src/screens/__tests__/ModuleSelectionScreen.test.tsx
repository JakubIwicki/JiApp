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
    const { getByTestId } = renderScreen(['YtDownloader', 'Scheduler']);

    expect(getByTestId('module-card-YtDownloader')).toBeTruthy();
    expect(getByTestId('module-card-Scheduler')).toBeTruthy();
  });

  it('renders only granted modules and hides ungranted ones', () => {
    const { getByTestId, queryByTestId } = renderScreen(['Scheduler']);

    expect(getByTestId('module-card-Scheduler')).toBeTruthy();
    expect(queryByTestId('module-card-YtDownloader')).toBeNull();
  });

  it('calls onSelectModule with the tapped module id', () => {
    const onSelectModule = jest.fn();
    const { getByTestId } = renderScreen(
      ['YtDownloader', 'Scheduler'],
      onSelectModule,
    );

    expect(onSelectModule).not.toHaveBeenCalled();

    fireEvent.press(getByTestId('module-card-Scheduler'));

    expect(onSelectModule).toHaveBeenCalledWith('Scheduler');
  });

  it('shows a personalised greeting with the display name', () => {
    const { getByTestId } = renderScreen(['YtDownloader'], jest.fn(), 'Anna');

    expect(getByTestId('module-greeting').props.children).toBe(
      'modules.greeting:Anna',
    );
  });

  it('falls back to a generic greeting when no display name is set', () => {
    const { getByTestId } = renderScreen(['YtDownloader'], jest.fn(), null);

    expect(getByTestId('module-greeting').props.children).toBe(
      'modules.greetingFallback',
    );
  });

  it('does not render the settings gear when onOpenSettings is not provided', () => {
    const { queryByTestId } = renderScreen(['YtDownloader']);

    expect(queryByTestId('module-selection-settings')).toBeNull();
  });

  it('renders the settings gear when onOpenSettings is provided', () => {
    const onOpenSettings = jest.fn();
    const { getByTestId } = render(
      <SafeAreaProvider initialMetrics={testMetrics}>
        <AuthContext.Provider value={buildAuthValue(['YtDownloader'], 'Jakub')}>
          <ModuleSelectionScreen
            onSelectModule={jest.fn()}
            onOpenSettings={onOpenSettings}
          />
        </AuthContext.Provider>
      </SafeAreaProvider>,
    );

    const gear = getByTestId('module-selection-settings');
    expect(gear).toBeTruthy();
    expect(gear.props.accessibilityRole).toBe('button');
    expect(gear.props.accessibilityLabel).toBe('settings.title');

    expect(onOpenSettings).not.toHaveBeenCalled();

    fireEvent.press(gear);

    expect(onOpenSettings).toHaveBeenCalledTimes(1);
  });
});
