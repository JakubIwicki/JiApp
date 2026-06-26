import React, { useEffect, useMemo } from 'react';
import {
  NavigationContainer,
  DefaultTheme,
  DarkTheme,
} from '@react-navigation/native';
import type { Theme as NavTheme } from '@react-navigation/native';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import TrackPlayer, { Capability } from 'react-native-track-player';
import { KeyboardProvider } from 'react-native-keyboard-controller';
import AppNavigator from './src/navigation/AppNavigator';
import { ThemeProvider, useTheme } from './src/context/ThemeContext';
import './src/i18n';

const ThemedNavigationContainer: React.FC = () => {
  const { colors, isDark } = useTheme();

  const navTheme: NavTheme = useMemo(
    () => ({
      ...(isDark ? DarkTheme : DefaultTheme),
      colors: {
        ...(isDark ? DarkTheme : DefaultTheme).colors,
        background: colors.background,
        card: colors.surface,
        text: colors.textPrimary,
        border: colors.border,
        primary: colors.primary,
      },
    }),
    [colors, isDark],
  );

  return (
    <NavigationContainer theme={navTheme}>
      <AppNavigator />
    </NavigationContainer>
  );
};

const App: React.FC = () => {
  useEffect(() => {
    const setup = async () => {
      try {
        await TrackPlayer.setupPlayer({
          waitForBuffer: true,
        });
        await TrackPlayer.updateOptions({
          capabilities: [Capability.Play, Capability.Pause, Capability.Stop],
          compactCapabilities: [Capability.Play, Capability.Pause],
        });
      } catch {
        // Player may already be initialized
      }
    };
    setup();
    return () => {
      TrackPlayer.reset().catch(() => {});
    };
  }, []);

  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <KeyboardProvider>
        <SafeAreaProvider>
          <ThemeProvider>
            <ThemedNavigationContainer />
          </ThemeProvider>
        </SafeAreaProvider>
      </KeyboardProvider>
    </GestureHandlerRootView>
  );
};

export default App;
