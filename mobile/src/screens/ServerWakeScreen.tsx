import React, { useCallback, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ActivityIndicator,
  BackHandler,
  Pressable,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import Animated, {
  useSharedValue,
  withTiming,
  useAnimatedStyle,
} from 'react-native-reanimated';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import useKeepAwake from '../hooks/useKeepAwake';
import useServerWake from '../hooks/useServerWake';
import { animation, borderRadius, spacing, zIndexScale } from '../styles/theme';
import type { Theme } from '../styles/theme';
import { useThemedStyles, useTheme } from '../context/ThemeContext';

interface Props {
  onComplete: () => void;
}

const ServerWakeScreen: React.FC<Props> = ({ onComplete }) => {
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();
  const styles = useThemedStyles(makeStyles);
  const { colors } = useTheme();

  const { phase, retry } = useServerWake(onComplete);
  const isWakingOrPolling = phase === 'waking' || phase === 'polling';
  useKeepAwake(isWakingOrPolling);

  const bgOpacity = useSharedValue(0);
  const textOpacity = useSharedValue(0);
  const textSlide = useSharedValue(12);
  const buttonOpacity = useSharedValue(0);

  // Animation sequence on mount
  useEffect(() => {
    const timeouts: ReturnType<typeof setTimeout>[] = [];

    const schedule = (fn: () => void, delayMs: number) => {
      const id = setTimeout(fn, delayMs);
      timeouts.push(id);
    };

    schedule(() => {
      bgOpacity.value = withTiming(1, { duration: animation.duration.normal });
    }, 0);
    schedule(() => {
      textOpacity.value = withTiming(1, { duration: animation.duration.slow });
      textSlide.value = withTiming(0, { duration: animation.duration.slow });
    }, animation.duration.normal);

    return () => {
      for (let i = 0; i < timeouts.length; i++) {
        clearTimeout(timeouts[i]);
      }
    };
  }, [bgOpacity, textOpacity, textSlide]);

  // Animate buttons in when unavailable
  useEffect(() => {
    if (phase === 'unavailable') {
      buttonOpacity.value = withTiming(1, {
        duration: animation.duration.normal,
      });
    }
  }, [phase, buttonOpacity]);

  const handleCloseApp = useCallback(() => {
    BackHandler.exitApp();
  }, []);

  const bgAnimatedStyle = useAnimatedStyle(() => ({
    opacity: bgOpacity.value,
  }));

  const textAnimatedStyle = useAnimatedStyle(() => ({
    opacity: textOpacity.value,
    transform: [{ translateY: textSlide.value }],
  }));

  const buttonAnimatedStyle = useAnimatedStyle(() => ({
    opacity: buttonOpacity.value,
  }));

  return (
    <View style={styles.container} testID="server-wake-screen">
      <Animated.View style={[styles.background, bgAnimatedStyle]} />
      <View style={[styles.content, { paddingTop: insets.top }]}>
        {isWakingOrPolling ? (
          <>
            <ActivityIndicator
              size="large"
              color={colors.primary}
              testID="wake-spinner"
            />
            <Animated.Text style={[styles.title, textAnimatedStyle]}>
              {t('wake.title')}
            </Animated.Text>
            <Animated.Text style={[styles.message, textAnimatedStyle]}>
              {t('wake.message')}
            </Animated.Text>
          </>
        ) : (
          <>
            <Text style={styles.warningIcon} testID="warning-icon">
              {'⚠'}
            </Text>
            <Animated.Text style={[styles.title, textAnimatedStyle]}>
              {t('wake.unavailable')}
            </Animated.Text>
            <Animated.Text style={[styles.message, textAnimatedStyle]}>
              {t('wake.unavailableMessage')}
            </Animated.Text>
            <Animated.View style={[styles.buttonGroup, buttonAnimatedStyle]}>
              <Pressable
                style={({ pressed }) => [
                  styles.button,
                  styles.retryButton,
                  pressed && { opacity: 0.7 },
                ]}
                onPress={retry}
                accessibilityRole="button"
                accessibilityLabel={t('wake.retry')}
                testID="wake-retry-button"
              >
                <Text style={styles.buttonText}>{t('wake.retry')}</Text>
              </Pressable>
              <Pressable
                style={({ pressed }) => [
                  styles.button,
                  styles.closeButton,
                  pressed && { opacity: 0.7 },
                ]}
                onPress={handleCloseApp}
                accessibilityRole="button"
                accessibilityLabel={t('wake.closeApp')}
                testID="wake-close-button"
              >
                <Text style={styles.closeButtonText}>{t('wake.closeApp')}</Text>
              </Pressable>
            </Animated.View>
          </>
        )}
      </View>
    </View>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      position: 'absolute',
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      zIndex: zIndexScale.overlay,
    },
    background: {
      position: 'absolute',
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      backgroundColor: t.colors.background,
    },
    content: {
      flex: 1,
      justifyContent: 'center',
      alignItems: 'center',
      paddingHorizontal: spacing.xl,
    },
    warningIcon: {
      fontSize: 48,
      textAlign: 'center',
      marginBottom: spacing.lg,
    },
    title: {
      fontSize: t.typography.heading.fontSize,
      fontWeight: t.typography.heading.fontWeight,
      color: t.colors.textPrimary,
      textAlign: 'center',
      marginBottom: spacing.sm,
      marginTop: spacing.lg,
    },
    message: {
      fontSize: t.typography.bodySmall.fontSize,
      color: t.colors.textTertiary,
      textAlign: 'center',
      marginBottom: spacing.xl,
      marginTop: spacing.sm,
    },
    buttonGroup: {
      gap: spacing.md,
      alignItems: 'center',
    },
    button: {
      borderRadius: borderRadius.md,
      paddingHorizontal: 24,
      paddingVertical: 12,
      minWidth: 200,
      alignItems: 'center',
      minHeight: 44,
      justifyContent: 'center',
    },
    retryButton: {
      backgroundColor: t.colors.primary,
    },
    closeButton: {
      backgroundColor: t.colors.surface,
      borderWidth: 1,
      borderColor: t.colors.border,
    },
    buttonText: {
      color: t.colors.textInverse,
      fontSize: t.typography.body.fontSize,
      fontWeight: '600',
    },
    closeButtonText: {
      color: t.colors.textPrimary,
      fontSize: t.typography.body.fontSize,
      fontWeight: '600',
    },
  });

export default ServerWakeScreen;
