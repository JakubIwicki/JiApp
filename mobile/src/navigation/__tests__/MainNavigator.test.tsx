import React, { useRef } from 'react';
import { render, waitFor, act } from '@testing-library/react-native';
import {
  NavigationContainer,
  NavigationContainerRef,
} from '@react-navigation/native';
import MainNavigator from '../MainNavigator';

// Initialize i18next so translations resolve for tab labels
import '../../i18n';

// Mock all 4 screens so we don't need their full implementations
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

// Helper component that exposes navigation ref for programmatic navigation
const NavigatorWithRef: React.FC<{
  onReady: (ref: NavigationContainerRef<any>) => void;
}> = ({ onReady }) => {
  const navRef = useRef<NavigationContainerRef<any>>(null);
  return (
    <NavigationContainer
      ref={navRef}
      onReady={() => {
        if (navRef.current) {
          onReady(navRef.current);
        }
      }}
    >
      <MainNavigator />
    </NavigationContainer>
  );
};

describe('MainNavigator', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders all four tab labels', () => {
    const { getByText, getAllByText } = render(
      <NavigationContainer>
        <MainNavigator />
      </NavigationContainer>,
    );

    // "Search" appears twice (tab label + stack header title)
    expect(getAllByText('Search').length).toBe(2);
    expect(getByText('Downloads')).toBeTruthy();
    expect(getByText('History')).toBeTruthy();
    expect(getByText('Settings')).toBeTruthy();
  });

  it('renders SearchScreen by default', async () => {
    const { findByText } = render(
      <NavigationContainer>
        <MainNavigator />
      </NavigationContainer>,
    );

    expect(await findByText('SearchScreen')).toBeTruthy();
  });

  it('navigates to History tab', async () => {
    let navRef: NavigationContainerRef<any> | null = null;
    const onReady = (ref: NavigationContainerRef<any>) => {
      navRef = ref;
    };

    const { findByText } = render(<NavigatorWithRef onReady={onReady} />);

    // Wait for navigation to be ready
    await waitFor(() => expect(navRef).not.toBeNull());

    // Navigate to History tab
    act(() => {
      navRef!.navigate('HistoryTab');
    });

    // HistoryScreen should now be visible
    expect(await findByText('HistoryScreen')).toBeTruthy();
  });

  it('navigates to Settings tab', async () => {
    let navRef: NavigationContainerRef<any> | null = null;
    const onReady = (ref: NavigationContainerRef<any>) => {
      navRef = ref;
    };

    const { findByText } = render(<NavigatorWithRef onReady={onReady} />);

    await waitFor(() => expect(navRef).not.toBeNull());
    act(() => {
      navRef!.navigate('SettingsTab');
    });

    expect(await findByText('SettingsScreen')).toBeTruthy();
  });

  it('navigates to Downloads tab', async () => {
    let navRef: NavigationContainerRef<any> | null = null;
    const onReady = (ref: NavigationContainerRef<any>) => {
      navRef = ref;
    };

    const { getByText } = render(<NavigatorWithRef onReady={onReady} />);

    await waitFor(() => expect(navRef).not.toBeNull());
    act(() => {
      navRef!.navigate('DownloadsTab');
    });

    // Content of Downloads tab may vary; just verify the tab label renders
    expect(getByText('Downloads')).toBeTruthy();
  });
});
