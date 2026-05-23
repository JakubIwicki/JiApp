import React from 'react';
import { render } from '@testing-library/react-native';

// Mock useAuth
jest.mock('../../hooks/useAuth', () => ({
  __esModule: true,
  default: () => ({
    displayName: 'Test User',
    username: 'testuser',
    userId: 1,
    logout: jest.fn(),
  }),
}));

// Mock react-i18next
jest.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

// Mock @react-navigation/native useNavigation
jest.mock('@react-navigation/native', () => ({
  useNavigation: () => ({
    setOptions: jest.fn(),
  }),
}));

// Mock LanguagePicker
jest.mock('../../components/LanguagePicker', () => {
  const { View } = require('react-native');
  return () => <View testID="language-picker" />;
});

import SettingsScreen from '../SettingsScreen';

describe('SettingsScreen', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders display name', () => {
    const { getByText } = render(<SettingsScreen />);
    expect(getByText('Test User')).toBeTruthy();
  });

  it('shows username from context, not userId', () => {
    const { getByText, queryByText } = render(<SettingsScreen />);
    // Should show the username string, not the numeric userId
    expect(getByText('testuser')).toBeTruthy();
    // Should NOT show userId as a number in the username field
    // '1' could appear elsewhere, but the label 'settings.username' should map to 'testuser'
    expect(queryByText(String(1))).toBeNull();
  });

  it('shows language picker', () => {
    const { getByTestId } = render(<SettingsScreen />);
    expect(getByTestId('language-picker')).toBeTruthy();
  });

  it('shows logout button', () => {
    const { getByTestId } = render(<SettingsScreen />);
    expect(getByTestId('logout-button')).toBeTruthy();
  });
});
