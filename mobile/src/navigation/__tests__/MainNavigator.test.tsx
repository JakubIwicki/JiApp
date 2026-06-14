import React, { useRef } from 'react';
import { render, waitFor, act } from '@testing-library/react-native';
import {
  NavigationContainer,
  NavigationContainerRef,
} from '@react-navigation/native';
import { SafeAreaProvider } from 'react-native-safe-area-context';
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

jest.mock('../../screens/DownloadsScreen', () => {
  const React = require('react');
  const { Text } = require('react-native');
  return {
    __esModule: true,
    default: () => React.createElement(Text, null, 'DownloadsScreen'),
  };
});

// Helper component that exposes navigation ref for programmatic navigation
const testMetrics = {
  insets: { top: 0, bottom: 0, left: 0, right: 0 },
  frame: { x: 0, y: 0, width: 390, height: 844 },
};

const NavigatorWithRef: React.FC<{
  onReady: (ref: NavigationContainerRef<any>) => void;
}> = ({ onReady }) => {
  const navRef = useRef<NavigationContainerRef<any>>(null);
  return (
    <SafeAreaProvider initialMetrics={testMetrics}>
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
    </SafeAreaProvider>
  );
};

describe('MainNavigator', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders all four tab labels', () => {
    const { getByText, getAllByText } = render(
      <SafeAreaProvider initialMetrics={testMetrics}>
        <NavigationContainer>
          <MainNavigator />
        </NavigationContainer>
      </SafeAreaProvider>,
    );

    // "Search" appears as tab label (native-stack may not render header title in test env)
    expect(getAllByText('Search').length).toBeGreaterThanOrEqual(1);
    expect(getByText('Downloads')).toBeTruthy();
    expect(getByText('History')).toBeTruthy();
    expect(getByText('Settings')).toBeTruthy();
  });

  it('renders SearchScreen by default', async () => {
    const { findByText } = render(
      <SafeAreaProvider initialMetrics={testMetrics}>
        <NavigationContainer>
          <MainNavigator />
        </NavigationContainer>
      </SafeAreaProvider>,
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

    const { getAllByText } = render(<NavigatorWithRef onReady={onReady} />);

    await waitFor(() => expect(navRef).not.toBeNull());
    act(() => {
      navRef!.navigate('DownloadsTab');
    });

    // Content of Downloads tab may vary; just verify the tab label renders
    expect(getAllByText('Downloads').length).toBeGreaterThanOrEqual(1);
  });

  it('renders DownloadsScreen in DownloadsTab', async () => {
    let navRef: NavigationContainerRef<any> | null = null;
    const { findByText } = render(
      <NavigatorWithRef
        onReady={ref => {
          navRef = ref;
        }}
      />,
    );

    await waitFor(() => expect(navRef).not.toBeNull());
    act(() => {
      navRef!.navigate('DownloadsTab');
    });

    expect(await findByText('DownloadsScreen')).toBeTruthy();
  });

  it('navigates from Search to Download with video params', async () => {
    let navRef: NavigationContainerRef<any> | null = null;
    const { findByText } = render(
      <NavigatorWithRef
        onReady={ref => {
          navRef = ref;
        }}
      />,
    );

    await waitFor(() => expect(navRef).not.toBeNull());

    act(() => {
      navRef!.navigate('Download', {
        videoId: 'test-123',
        title: 'Test Video',
        description: 'Test description',
        imageUrl: 'https://example.com/thumb.jpg',
        videoUrl: 'https://example.com/video.mp4',
        channelTitle: 'Test Channel',
      });
    });

    expect(await findByText('DownloadScreen')).toBeTruthy();
  });
});
