import React from 'react';
import {
  render,
  screen,
  waitFor,
  fireEvent,
} from '@testing-library/react-native';
import { View, Text, Pressable } from 'react-native';

// useColorScheme is auto-mocked by @react-native/jest-preset to return 'light'.
// We override it per test via the mock function.

import { ThemeProvider, useTheme } from '../ThemeContext';
import {
  claudeLight,
  claudeDark,
  lavenderLight,
  lavenderDark,
} from '../../styles/theme';
import * as storageService from '../../services/storageService';

// Test component that reads context
const TestConsumer: React.FC = () => {
  const { colors, isDark } = useTheme();

  return (
    <View>
      <View testID="isDark">
        <Text>{String(isDark)}</Text>
      </View>
      <View testID="background">
        <Text>{colors.background}</Text>
      </View>
      <View testID="surface">
        <Text>{colors.surface}</Text>
      </View>
      <View testID="textPrimary">
        <Text>{colors.textPrimary}</Text>
      </View>
    </View>
  );
};

// Test component that exposes themeMode and a button to call setThemeMode
const TestConsumerWithControls: React.FC = () => {
  const { colors, isDark, themeMode, setThemeMode } = useTheme();

  return (
    <View>
      <View testID="isDark">
        <Text>{String(isDark)}</Text>
      </View>
      <View testID="background">
        <Text>{colors.background}</Text>
      </View>
      <View testID="themeMode">
        <Text>{themeMode}</Text>
      </View>
      <Pressable
        testID="set-dark-mode"
        onPress={() => setThemeMode('dark')}
        accessibilityRole="button"
      >
        <Text>Set Dark</Text>
      </Pressable>
    </View>
  );
};

describe('ThemeContext', () => {
  afterEach(() => {
    // Reset useColorScheme mock back to default
    jest.restoreAllMocks();
  });

  it('provides light colors when system color scheme is light', () => {
    jest
      .spyOn(
        require('react-native/Libraries/Utilities/useColorScheme'),
        'default',
      )
      .mockReturnValue('light');

    render(
      <ThemeProvider>
        <TestConsumer />
      </ThemeProvider>,
    );

    expect(screen.getByTestId('isDark')).toBeDefined();
    expect(screen.getByText(String(false))).toBeDefined();
    expect(screen.getByText(claudeLight.background)).toBeDefined();
    expect(screen.getByText(claudeLight.surface)).toBeDefined();
    expect(screen.getByText(claudeLight.textPrimary)).toBeDefined();
  });

  it('provides dark colors when system color scheme is dark', () => {
    jest
      .spyOn(
        require('react-native/Libraries/Utilities/useColorScheme'),
        'default',
      )
      .mockReturnValue('dark');

    render(
      <ThemeProvider>
        <TestConsumer />
      </ThemeProvider>,
    );

    expect(screen.getByText(String(true))).toBeDefined();
    expect(screen.getByText(claudeDark.background)).toBeDefined();
    expect(screen.getByText(claudeDark.surface)).toBeDefined();
    expect(screen.getByText(claudeDark.textPrimary)).toBeDefined();
  });

  it('defaults to light when useColorScheme returns null', () => {
    jest
      .spyOn(
        require('react-native/Libraries/Utilities/useColorScheme'),
        'default',
      )
      .mockReturnValue(null);

    render(
      <ThemeProvider>
        <TestConsumer />
      </ThemeProvider>,
    );

    expect(screen.getByText(String(false))).toBeDefined();
    expect(screen.getByText(claudeLight.background)).toBeDefined();
  });

  it('defaults to light when useColorScheme returns undefined', () => {
    jest
      .spyOn(
        require('react-native/Libraries/Utilities/useColorScheme'),
        'default',
      )
      .mockReturnValue(undefined);

    render(
      <ThemeProvider>
        <TestConsumer />
      </ThemeProvider>,
    );

    expect(screen.getByText(String(false))).toBeDefined();
    expect(screen.getByText(claudeLight.background)).toBeDefined();
  });

  it('dark palette has different values from light palette', () => {
    expect(lavenderDark.background).not.toBe(lavenderLight.background);
    expect(lavenderDark.surface).not.toBe(lavenderLight.surface);
    expect(lavenderDark.textPrimary).not.toBe(lavenderLight.textPrimary);
  });

  it('manual dark mode overrides a light system scheme', async () => {
    jest.spyOn(storageService, 'getThemeMode').mockResolvedValue('dark');
    jest
      .spyOn(
        require('react-native/Libraries/Utilities/useColorScheme'),
        'default',
      )
      .mockReturnValue('light');

    render(
      <ThemeProvider>
        <TestConsumer />
      </ThemeProvider>,
    );

    await waitFor(() => {
      expect(screen.getByText(String(true))).toBeDefined();
    });
    expect(screen.getByText(claudeDark.background)).toBeDefined();
  });

  it('manual light mode overrides a dark system scheme', async () => {
    jest.spyOn(storageService, 'getThemeMode').mockResolvedValue('light');
    jest
      .spyOn(
        require('react-native/Libraries/Utilities/useColorScheme'),
        'default',
      )
      .mockReturnValue('dark');

    render(
      <ThemeProvider>
        <TestConsumer />
      </ThemeProvider>,
    );

    await waitFor(() => {
      expect(screen.getByText(String(false))).toBeDefined();
    });
    expect(screen.getByText(claudeLight.background)).toBeDefined();
  });

  it('system mode follows the OS (dark)', async () => {
    jest.spyOn(storageService, 'getThemeMode').mockResolvedValue('system');
    jest
      .spyOn(
        require('react-native/Libraries/Utilities/useColorScheme'),
        'default',
      )
      .mockReturnValue('dark');

    render(
      <ThemeProvider>
        <TestConsumer />
      </ThemeProvider>,
    );

    await waitFor(() => {
      expect(screen.getByText(String(true))).toBeDefined();
    });
  });

  it('setThemeMode persists the choice', async () => {
    const saveSpy = jest
      .spyOn(storageService, 'saveThemeMode')
      .mockResolvedValue(undefined);

    render(
      <ThemeProvider>
        <TestConsumerWithControls />
      </ThemeProvider>,
    );

    fireEvent.press(screen.getByTestId('set-dark-mode'));

    await waitFor(() => {
      expect(saveSpy).toHaveBeenCalledWith('dark');
    });
  });
});
