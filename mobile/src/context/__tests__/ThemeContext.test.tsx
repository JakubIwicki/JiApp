import React from 'react';
import { render, screen } from '@testing-library/react-native';
import { View, Text } from 'react-native';

// useColorScheme is auto-mocked by @react-native/jest-preset to return 'light'.
// We override it per test via the mock function.

import { ThemeProvider, useTheme } from '../ThemeContext';
import { colorsLight, colorsDark } from '../../styles/theme';

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

describe('ThemeContext', () => {
  afterEach(() => {
    // Reset useColorScheme mock back to default
    jest.restoreAllMocks();
  });

  it('provides light colors when system color scheme is light', () => {
    jest.spyOn(
      require('react-native/Libraries/Utilities/useColorScheme'),
      'default',
    ).mockReturnValue('light');

    render(
      <ThemeProvider>
        <TestConsumer />
      </ThemeProvider>,
    );

    expect(screen.getByTestId('isDark')).toBeDefined();
    expect(screen.getByText(String(false))).toBeDefined();
    expect(screen.getByText(colorsLight.background)).toBeDefined();
    expect(screen.getByText(colorsLight.surface)).toBeDefined();
    expect(screen.getByText(colorsLight.textPrimary)).toBeDefined();
  });

  it('provides dark colors when system color scheme is dark', () => {
    jest.spyOn(
      require('react-native/Libraries/Utilities/useColorScheme'),
      'default',
    ).mockReturnValue('dark');

    render(
      <ThemeProvider>
        <TestConsumer />
      </ThemeProvider>,
    );

    expect(screen.getByText(String(true))).toBeDefined();
    expect(screen.getByText(colorsDark.background)).toBeDefined();
    expect(screen.getByText(colorsDark.surface)).toBeDefined();
    expect(screen.getByText(colorsDark.textPrimary)).toBeDefined();
  });

  it('defaults to light when useColorScheme returns null', () => {
    jest.spyOn(
      require('react-native/Libraries/Utilities/useColorScheme'),
      'default',
    ).mockReturnValue(null);

    render(
      <ThemeProvider>
        <TestConsumer />
      </ThemeProvider>,
    );

    expect(screen.getByText(String(false))).toBeDefined();
    expect(screen.getByText(colorsLight.background)).toBeDefined();
  });

  it('defaults to light when useColorScheme returns undefined', () => {
    jest.spyOn(
      require('react-native/Libraries/Utilities/useColorScheme'),
      'default',
    ).mockReturnValue(undefined);

    render(
      <ThemeProvider>
        <TestConsumer />
      </ThemeProvider>,
    );

    expect(screen.getByText(String(false))).toBeDefined();
    expect(screen.getByText(colorsLight.background)).toBeDefined();
  });

  it('dark palette has different values from light palette', () => {
    expect(colorsDark.background).not.toBe(colorsLight.background);
    expect(colorsDark.surface).not.toBe(colorsLight.surface);
    expect(colorsDark.textPrimary).not.toBe(colorsLight.textPrimary);
  });
});
