import React, { useCallback, useRef } from 'react';
import {
  Animated,
  type GestureResponderEvent,
  StyleSheet,
  TouchableOpacity,
} from 'react-native';
import type { BottomTabBarButtonProps } from '@react-navigation/bottom-tabs';
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
  const scaleValue = useRef(new Animated.Value(1)).current;

  const handlePressIn = useCallback(
    (e: GestureResponderEvent) => {
      Animated.spring(scaleValue, {
        toValue: 0.96,
        ...animation.spring.bouncy,
      }).start();
      onPressIn?.(e);
    },
    [scaleValue, onPressIn],
  );

  const handlePressOut = useCallback(
    (e: GestureResponderEvent) => {
      Animated.spring(scaleValue, {
        toValue: 1,
        ...animation.spring.bouncy,
      }).start();
      onPressOut?.(e);
    },
    [scaleValue, onPressOut],
  );

  return (
    <TouchableOpacity
      onPress={onPress ?? undefined}
      onLongPress={onLongPress ?? undefined}
      onPressIn={handlePressIn}
      onPressOut={handlePressOut}
      activeOpacity={0.8}
      accessibilityLabel={accessibilityLabel}
      accessibilityRole={accessibilityRole ?? 'button'}
      accessibilityState={accessibilityState}
      testID={testID}
      style={[styles.container, style]}
    >
      <Animated.View
        style={[
          styles.inner,
          { transform: [{ scale: scaleValue }] },
        ]}
      >
        {children}
      </Animated.View>
    </TouchableOpacity>
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
