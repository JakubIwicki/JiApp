import React, { useCallback, useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import Animated, {
  useSharedValue,
  withTiming,
  useAnimatedStyle,
} from 'react-native-reanimated';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemedStyles } from '../context/ThemeContext';
import type { Theme } from '../styles/theme';
import { animation, borderRadius, spacing, zIndexScale } from '../styles/theme';

interface Props {
  visible: boolean;
  onTimeout: () => void;
}

const ConnectionFailureOverlay: React.FC<Props> = ({ visible, onTimeout }) => {
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();
  const styles = useThemedStyles(makeStyles);
  const bgOpacity = useSharedValue(0);
  const textOpacity = useSharedValue(0);
  const textSlide = useSharedValue(12);
  const buttonOpacity = useSharedValue(0);

  const completedRef = useRef(false);
  const onTimeoutRef = useRef(onTimeout);
  onTimeoutRef.current = onTimeout;

  const handleTimeout = useCallback(() => {
    if (!completedRef.current) {
      completedRef.current = true;
      onTimeoutRef.current();
    }
  }, []);

  useEffect(() => {
    if (!visible) {
      completedRef.current = false;
      return;
    }

    completedRef.current = false;
    const timeouts: ReturnType<typeof setTimeout>[] = [];

    const schedule = (fn: () => void, delayMs: number) => {
      const id = setTimeout(fn, delayMs);
      timeouts.push(id);
    };

    // Background fade in
    schedule(() => {
      bgOpacity.value = withTiming(1, { duration: 300 });
    }, 0);
    // Text slide up + fade in
    schedule(() => {
      textOpacity.value = withTiming(1, { duration: 400 });
      textSlide.value = withTiming(0, { duration: 400 });
    }, 300);
    // Button fade in (slightly delayed so user sees the message first)
    schedule(() => {
      buttonOpacity.value = withTiming(1, { duration: 300 });
    }, 1500);

    return () => {
      for (let i = 0; i < timeouts.length; i++) {
        clearTimeout(timeouts[i]);
      }
    };
  }, [
    visible,
    bgOpacity,
    textOpacity,
    textSlide,
    buttonOpacity,
    handleTimeout,
  ]);

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

  if (!visible) {
    return null;
  }

  return (
    <View style={styles.container} testID="connection-error-overlay">
      <Animated.View style={[styles.background, bgAnimatedStyle]} />
      <View style={[styles.content, { paddingTop: insets.top }]}>
        <Text style={styles.warningIcon} testID="warning-icon">
          {'⚠'}
        </Text>
        <Animated.Text style={[styles.title, textAnimatedStyle]}>
          {t('connectionError.title')}
        </Animated.Text>
        <Animated.Text style={[styles.message, textAnimatedStyle]}>
          {t('connectionError.message')}
        </Animated.Text>
        <Animated.View style={buttonAnimatedStyle}>
          <Pressable
            style={styles.button}
            onPress={handleTimeout}
            testID="close-app-button"
          >
            <Text style={styles.buttonText}>
              {t('connectionError.closeApp')}
            </Text>
          </Pressable>
        </Animated.View>
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
      zIndex: zIndexScale.overlay + 10,
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
    },
    message: {
      fontSize: t.typography.bodySmall.fontSize,
      color: t.colors.textTertiary,
      textAlign: 'center',
      marginBottom: spacing.xl,
    },
    button: {
      backgroundColor: t.colors.error,
      borderRadius: borderRadius.md,
      paddingHorizontal: 24,
      paddingVertical: 12,
      minWidth: 160,
      alignItems: 'center',
    },
    buttonText: {
      color: t.colors.textInverse,
      fontSize: t.typography.body.fontSize,
      fontWeight: '600',
    },
  });

export default ConnectionFailureOverlay;
