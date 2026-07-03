import React from 'react';
import { render, fireEvent } from '@testing-library/react-native';

// Mock react-i18next
jest.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

// Mock react-native-svg for TabIcon's settings gear
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
  };
});

// Mock @react-navigation/native useNavigation
const mockNavigate = jest.fn();
jest.mock('@react-navigation/native', () => ({
  useNavigation: () => ({
    navigate: mockNavigate,
  }),
  CompositeNavigationProp: undefined as unknown,
}));

import { SwitchModuleButton, SettingsButton } from '../HeaderNavButtons';

describe('SwitchModuleButton', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders the switch module label', () => {
    const { getByText } = render(<SwitchModuleButton />);
    expect(getByText('modules.switch')).toBeTruthy();
  });

  it('has correct accessibility role and label', () => {
    const { getByTestId } = render(<SwitchModuleButton />);
    const btn = getByTestId('header-switch-module');
    expect(btn.props.accessibilityRole).toBe('button');
    expect(btn.props.accessibilityLabel).toBe('modules.switch');
  });

  it('navigates to ModuleSelection on press', () => {
    const { getByTestId } = render(<SwitchModuleButton />);

    // Precondition: not navigated yet
    expect(mockNavigate).not.toHaveBeenCalled();

    fireEvent.press(getByTestId('header-switch-module'));

    expect(mockNavigate).toHaveBeenCalledWith('ModuleSelection');
  });
});

describe('SettingsButton', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders the settings gear icon', () => {
    const { getByTestId } = render(<SettingsButton />);
    expect(getByTestId('header-settings')).toBeTruthy();
  });

  it('has correct accessibility role and label', () => {
    const { getByTestId } = render(<SettingsButton />);
    const btn = getByTestId('header-settings');
    expect(btn.props.accessibilityRole).toBe('button');
    expect(btn.props.accessibilityLabel).toBe('settings.title');
  });

  it('navigates to Settings on press', () => {
    const { getByTestId } = render(<SettingsButton />);

    // Precondition: not navigated yet
    expect(mockNavigate).not.toHaveBeenCalled();

    fireEvent.press(getByTestId('header-settings'));

    expect(mockNavigate).toHaveBeenCalledWith('Settings');
  });
});
