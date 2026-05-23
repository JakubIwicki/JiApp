import React from 'react';
import { render, waitFor } from '@testing-library/react-native';
import { NavigationContainer } from '@react-navigation/native';
import AppNavigator from '../AppNavigator';

// Mock the screens to simplify testing
jest.mock('../../screens/LoginScreen', () => {
  const React = require('react');
  const { Text } = require('react-native');
  return {
    __esModule: true,
    default: () => React.createElement(Text, null, 'LoginScreen'),
  };
});

jest.mock('../../screens/RegisterScreen', () => {
  const React = require('react');
  const { Text } = require('react-native');
  return {
    __esModule: true,
    default: () => React.createElement(Text, null, 'RegisterScreen'),
  };
});

jest.mock('../../screens/SearchScreen', () => {
  const React = require('react');
  const { Text } = require('react-native');
  return {
    __esModule: true,
    default: () => React.createElement(Text, null, 'SearchScreen'),
  };
});

jest.mock('../../screens/DownloadScreen', () => {
  const React = require('react');
  const { Text } = require('react-native');
  return {
    __esModule: true,
    default: () => React.createElement(Text, null, 'DownloadScreen'),
  };
});

jest.mock('../../screens/HistoryScreen', () => {
  const React = require('react');
  const { Text } = require('react-native');
  return {
    __esModule: true,
    default: () => React.createElement(Text, null, 'HistoryScreen'),
  };
});

jest.mock('../../screens/SettingsScreen', () => {
  const React = require('react');
  const { Text } = require('react-native');
  return {
    __esModule: true,
    default: () => React.createElement(Text, null, 'SettingsScreen'),
  };
});

// Mock storageService
let mockGetTokenImpl: () => Promise<string | null> = () =>
  Promise.resolve(null);
let mockCheckTokenImpl: () => Promise<unknown> = () =>
  Promise.reject(new Error('no token'));

jest.mock('../../services/storageService', () => ({
  getToken: () => mockGetTokenImpl(),
  saveToken: jest.fn(() => Promise.resolve()),
  clearToken: jest.fn(() => Promise.resolve()),
  saveUserId: jest.fn(() => Promise.resolve()),
  clearUserId: jest.fn(() => Promise.resolve()),
  saveDisplayName: jest.fn(() => Promise.resolve()),
  clearDisplayName: jest.fn(() => Promise.resolve()),
  saveUsername: jest.fn(() => Promise.resolve()),
  getUsername: jest.fn(() => Promise.resolve(null)),
  clearUsername: jest.fn(() => Promise.resolve()),
  clearCredentials: jest.fn(() => Promise.resolve()),
  getUserId: jest.fn(() => Promise.resolve(null)),
  getDisplayName: jest.fn(() => Promise.resolve(null)),
  saveCredentials: jest.fn(() => Promise.resolve()),
  getCredentials: jest.fn(() => Promise.resolve(null)),
}));

jest.mock('../../services/authService', () => ({
  login: jest.fn(),
  register: jest.fn(),
  checkToken: () => mockCheckTokenImpl(),
}));

describe('AppNavigator', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    // Default: no token
    mockGetTokenImpl = () => Promise.resolve(null);
  });

  it('renders AuthNavigator (LoginScreen) when token null', async () => {
    const { findByText } = render(
      <NavigationContainer>
        <AppNavigator />
      </NavigationContainer>,
    );
    expect(await findByText('LoginScreen')).toBeTruthy();
  });

  it('renders loading spinner when isLoading', async () => {
    // Make getToken never resolve so isLoading stays true
    mockGetTokenImpl = () => new Promise(() => {});

    const { findByTestId } = render(
      <NavigationContainer>
        <AppNavigator />
      </NavigationContainer>,
    );
    expect(await findByTestId('loading-screen')).toBeTruthy();
  });

  it('renders MainNavigator (SearchScreen) when token present', async () => {
    mockGetTokenImpl = () => Promise.resolve('valid-token');
    mockCheckTokenImpl = () =>
      Promise.resolve({
        id: 1,
        displayName: 'Test User',
        token: 'valid-token',
      });

    const { findByText } = render(
      <NavigationContainer>
        <AppNavigator />
      </NavigationContainer>,
    );
    expect(await findByText('SearchScreen')).toBeTruthy();
  });
});
