import React, { useRef } from 'react';
import { render, waitFor, act } from '@testing-library/react-native';
import {
  NavigationContainer,
  NavigationContainerRef,
} from '@react-navigation/native';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import AuthNavigator from '../AuthNavigator';
import type { AuthStackParamList } from '../types';

type AuthNavRef = NavigationContainerRef<AuthStackParamList>;

const screenMock = (label: string) => {
  const ReactMock = require('react');
  const { Text } = require('react-native');
  return {
    __esModule: true,
    default: () => ReactMock.createElement(Text, null, label),
  };
};

jest.mock('../../screens/LoginScreen', () => screenMock('LoginScreen'));
jest.mock('../../screens/RegisterScreen', () => screenMock('RegisterScreen'));

const testMetrics = {
  insets: { top: 0, bottom: 0, left: 0, right: 0 },
  frame: { x: 0, y: 0, width: 390, height: 844 },
};

const NavigatorWithRef: React.FC<{
  onReady: (ref: AuthNavRef) => void;
}> = ({ onReady }) => {
  const navRef = useRef<AuthNavRef>(null);
  return (
    <SafeAreaProvider initialMetrics={testMetrics}>
      <NavigationContainer
        ref={navRef}
        onReady={() => {
          if (navRef.current) onReady(navRef.current);
        }}
      >
        <AuthNavigator />
      </NavigationContainer>
    </SafeAreaProvider>
  );
};

describe('AuthNavigator', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders LoginScreen by default', async () => {
    const { findByText } = render(
      <SafeAreaProvider initialMetrics={testMetrics}>
        <NavigationContainer>
          <AuthNavigator />
        </NavigationContainer>
      </SafeAreaProvider>,
    );

    expect(await findByText('LoginScreen')).toBeTruthy();
  });

  it('renders RegisterScreen when navigated', async () => {
    let navRef: AuthNavRef | null = null;
    const { findByText } = render(
      <NavigatorWithRef
        onReady={ref => {
          navRef = ref;
        }}
      />,
    );

    await waitFor(() => expect(navRef).not.toBeNull());

    act(() => {
      navRef!.navigate('Register');
    });

    expect(await findByText('RegisterScreen')).toBeTruthy();
  });

  it('navigates back from Register to Login', async () => {
    let navRef: AuthNavRef | null = null;
    const { findByText } = render(
      <NavigatorWithRef
        onReady={ref => {
          navRef = ref;
        }}
      />,
    );

    await waitFor(() => expect(navRef).not.toBeNull());

    act(() => {
      navRef!.navigate('Register');
    });
    expect(await findByText('RegisterScreen')).toBeTruthy();

    act(() => {
      navRef!.goBack();
    });
    expect(await findByText('LoginScreen')).toBeTruthy();
  });

  it('has correct screen component names', async () => {
    let navRef: AuthNavRef | null = null;
    const { findByText } = render(
      <NavigatorWithRef
        onReady={ref => {
          navRef = ref;
        }}
      />,
    );

    await waitFor(() => expect(navRef).not.toBeNull());

    // Login is the default
    expect(await findByText('LoginScreen')).toBeTruthy();

    // Navigate to Register
    act(() => {
      navRef!.navigate('Register');
    });
    expect(await findByText('RegisterScreen')).toBeTruthy();

    // Navigate back to Login
    act(() => {
      navRef!.goBack();
    });
    expect(await findByText('LoginScreen')).toBeTruthy();
  });
});
