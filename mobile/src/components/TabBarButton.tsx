import React, { useCallback } from 'react';
import {
  type GestureResponderEvent,
  Pressable,
  StyleSheet,
} from 'react-native';
import Animated, { useSharedValue, withSpring, useAnimatedStyle } from 'react-native-reanimated';
import type { BottomTabBarButtonProps } from '../navigation/bottomTabs';
import { animation } from '../styles/theme';

const TabBarButton: React.FC<BottomTabBarButtonProps> = ({
  children,
  onPress,
  onLongPress,
  onPressIn,
  onPressOut,
  accessibilityLabel,
  accessibilityRole,
  accessibilityState,
  testID,
  style,
}) => {
  const scaleValue = useSharedValue(1);

  const handlePressIn = useCallback(
    (e: GestureResponderEvent) => {
      scaleValue.value = withSpring(0.96, animation.spring.bouncy);
      onPressIn?.(e);
    },
    [scaleValue, onPressIn],
  );

  const handlePressOut = useCallback(
    (e: GestureResponderEvent) => {
      scaleValue.value = withSpring(1, animation.spring.bouncy);
      onPressOut?.(e);
    },
    [scaleValue, onPressOut],
  );

  const animatedStyle = useAnimatedStyle(() => ({
    transform: [{ scale: scaleValue.value }],
  }));

  return (
    <Pressable
      onPress={onPress ?? undefined}
      onLongPress={onLongPress ?? undefined}
      onPressIn={handlePressIn}
      onPressOut={handlePressOut}
      accessibilityLabel={accessibilityLabel}
      accessibilityRole={accessibilityRole ?? 'button'}
      accessibilityState={accessibilityState}
      testID={testID}
      style={({ pressed }) => [
        styles.container,
        style,
        pressed && { opacity: 0.8 },
      ]}
    >
      <Animated.View
        style={[
          styles.inner,
          animatedStyle,
        ]}
      >
        {children}
      </Animated.View>
    </Pressable>
  );
};

const styles = StyleSheet.create({
  container: {
    overflow: 'hidden',
  },
  inner: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
});

export default TabBarButton;
