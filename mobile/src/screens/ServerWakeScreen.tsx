import React, { useCallback, useEffect, useRef, useState } from 'react';
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
import { API_BASE_URL, WAKE_API_URL } from '../config';
import {
  animation,
  borderRadius,
  colors,
  spacing,
  typography,
  zIndexScale,
} from '../styles/theme';

const WAKE_POLL_INTERVAL = 3000;
const WAKE_POLL_TIMEOUT = 10000;
const WAKE_TOTAL_TIMEOUT = 120000;

interface Props {
  onComplete: () => void;
}

const ServerWakeScreen: React.FC<Props> = ({ onComplete }) => {
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();

  const [phase, setPhase] = useState<'waking' | 'polling' | 'unavailable'>(
    'waking',
  );
  const [retryCount, setRetryCount] = useState(0);
  const activeRef = useRef(true);
  const onCompleteRef = useRef(onComplete);
  onCompleteRef.current = onComplete;

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

  // Wake-up + health polling — re-runs when retryCount changes
  useEffect(() => {
    activeRef.current = true;
    const startedAt = Date.now();
    let pollTimer: ReturnType<typeof setInterval> | null = null;
    let pollAborted = false;

    const pollHealth = async (): Promise<void> => {
      const healthUrl = `${API_BASE_URL.replace(/\/api\/v1\/?$/, '')}/health`;
      try {
        const controller = new AbortController();
        const timeoutId = setTimeout(
          () => controller.abort(),
          WAKE_POLL_TIMEOUT,
        );

        const response = await fetch(healthUrl, {
          method: 'GET',
          signal: controller.signal,
        });
        clearTimeout(timeoutId);

        if (response.ok && activeRef.current) {
          if (pollTimer) clearInterval(pollTimer);
          if (activeRef.current) {
            onCompleteRef.current();
          }
        }
      } catch {
        // Poll failed — will retry on next interval
      }
    };

    const startWake = async () => {
      try {
        await fetch(WAKE_API_URL + '/start', { method: 'POST' });
      } catch {
        // Wake API call may fail (Lambda cold start) — continue polling anyway
      }

      if (!activeRef.current || pollAborted) return;

      setPhase('polling');
      pollHealth();
      pollTimer = setInterval(() => {
        if (pollAborted) return;

        if (Date.now() - startedAt >= WAKE_TOTAL_TIMEOUT) {
          clearInterval(pollTimer);
          if (activeRef.current && !pollAborted) {
            setPhase('unavailable');
          }
          return;
        }

        pollHealth();
      }, WAKE_POLL_INTERVAL);
    };

    startWake();

    return () => {
      pollAborted = true;
      if (pollTimer) clearInterval(pollTimer);
    };
  }, [retryCount]);

  // Animate buttons in when unavailable
  useEffect(() => {
    if (phase === 'unavailable') {
      buttonOpacity.value = withTiming(1, {
        duration: animation.duration.normal,
      });
    }
  }, [phase, buttonOpacity]);

  const handleRetry = useCallback(() => {
    setPhase('waking');
    setRetryCount(c => c + 1);
  }, []);

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

  const isWakingOrPolling = phase === 'waking' || phase === 'polling';

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
                onPress={handleRetry}
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

const styles = StyleSheet.create({
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
    backgroundColor: colors.background,
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
    fontSize: typography.heading.fontSize,
    fontWeight: typography.heading.fontWeight,
    color: colors.textPrimary,
    textAlign: 'center',
    marginBottom: spacing.sm,
    marginTop: spacing.lg,
  },
  message: {
    fontSize: typography.bodySmall.fontSize,
    color: colors.textTertiary,
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
    backgroundColor: colors.primary,
  },
  closeButton: {
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
  },
  buttonText: {
    color: colors.textInverse,
    fontSize: typography.body.fontSize,
    fontWeight: '600',
  },
  closeButtonText: {
    color: colors.textPrimary,
    fontSize: typography.body.fontSize,
    fontWeight: '600',
  },
});

export default ServerWakeScreen;
