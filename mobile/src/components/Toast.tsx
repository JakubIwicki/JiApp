import React, { useCallback, useEffect, useMemo, useRef } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import Animated, {
  useSharedValue,
  withSpring,
  withTiming,
  useAnimatedStyle,
  runOnJS,
} from 'react-native-reanimated';
import { Gesture, GestureDetector } from 'react-native-gesture-handler';
import { useTheme, useThemedStyles } from '../context/ThemeContext';
import type { Theme } from '../styles/theme';
import { animation, borderRadius } from '../styles/theme';
import type { ToastType } from '../context/ToastContext';

interface ToastProps {
  type: ToastType;
  title: string;
  description?: string;
  persistent: boolean;
  onDismiss: () => void;
}

const ICONS: Record<ToastType, string> = {
  success: '✓',
  error: '✕',
  info: 'ℹ',
  warning: '⚠',
};

const Toast: React.FC<ToastProps> = ({
  type,
  title,
  description,
  persistent,
  onDismiss,
}) => {
  const translateY = useSharedValue(-100);
  const opacity = useSharedValue(0);
  const panX = useSharedValue(0);
  const dismissingRef = useRef(false);
  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);

  const BG_COLORS: Record<ToastType, string> = useMemo(
    () => ({
      success: colors.success,
      error: colors.error,
      info: colors.info,
      warning: colors.warning,
    }),
    [colors],
  );

  const animateOut = useCallback(
    (callback?: () => void) => {
      dismissingRef.current = true;
      translateY.value = withTiming(-100, { duration: 200 });
      opacity.value = withTiming(0, { duration: 200 }, () => {
        if (callback) {
          runOnJS(callback)();
        }
      });
    },
    [translateY, opacity],
  );

  useEffect(() => {
    translateY.value = withSpring(0, animation.spring.bouncy);
    opacity.value = withTiming(1, { duration: 300 });
  }, [translateY, opacity]);

  const panGesture = useMemo(
    () =>
      Gesture.Pan()
        .activeOffsetX([-5, 5])
        .onUpdate(event => {
          if (event.translationX < 0) {
            panX.value = event.translationX;
          }
        })
        .onEnd(event => {
          if (event.translationX < -50) {
            animateOut(() => onDismiss());
          } else {
            panX.value = withSpring(0);
          }
        }),
    [panX, onDismiss, animateOut],
  );

  const handleDismiss = () => {
    if (dismissingRef.current) return;
    animateOut(() => onDismiss());
  };

  const animatedStyle = useAnimatedStyle(() => ({
    transform: [{ translateY: translateY.value }, { translateX: panX.value }],
    opacity: opacity.value,
  }));

  const bgColor = BG_COLORS[type];
  const icon = ICONS[type];

  return (
    <GestureDetector gesture={panGesture}>
      <Animated.View
        style={[styles.container, { backgroundColor: bgColor }, animatedStyle]}
      >
        <Text style={styles.icon}>{icon}</Text>
        <View style={styles.content}>
          <Text style={styles.title} numberOfLines={1}>
            {title}
          </Text>
          {description ? (
            <Text style={styles.description} numberOfLines={1}>
              {description}
            </Text>
          ) : null}
        </View>
        {persistent ? (
          <Pressable
            onPress={handleDismiss}
            style={({ pressed }) => [
              styles.closeButton,
              pressed && { opacity: 0.7 },
            ]}
            hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
          >
            <Text style={styles.closeText}>{'✕'}</Text>
          </Pressable>
        ) : null}
      </Animated.View>
    </GestureDetector>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      flexDirection: 'row',
      alignItems: 'center',
      borderRadius: borderRadius.lg,
      paddingVertical: 12,
      paddingHorizontal: 16,
      gap: 10,
      minHeight: 44,
    },
    icon: {
      fontSize: 16,
      color: t.colors.textInverse,
      fontWeight: '600',
      width: 20,
      textAlign: 'center',
    },
    content: {
      flex: 1,
    },
    title: {
      fontSize: 14,
      color: t.colors.textInverse,
      fontWeight: '600',
    },
    description: {
      fontSize: 12,
      color: 'rgba(255,255,255,0.85)',
      marginTop: 2,
    },
    closeButton: {
      width: 20,
      height: 20,
      alignItems: 'center',
      justifyContent: 'center',
    },
    closeText: {
      fontSize: 14,
      color: t.colors.textInverse,
      fontWeight: '600',
    },
  });

export default Toast;
