import React, { useEffect } from 'react';
import { StyleSheet, Text } from 'react-native';
import Animated, { useSharedValue, withSpring, useAnimatedStyle } from 'react-native-reanimated';
import { animation, colors } from '../styles/theme';

interface SuccessCheckmarkProps {
  size?: number;
}

const SuccessCheckmark: React.FC<SuccessCheckmarkProps> = ({
  size = 64,
}) => {
  const scaleAnim = useSharedValue(0);

  useEffect(() => {
    scaleAnim.value = withSpring(1, animation.spring.bouncy);
  }, [scaleAnim]);

  const animatedStyle = useAnimatedStyle(() => ({
    transform: [{ scale: scaleAnim.value }],
  }));

  return (
    <Animated.View
      style={[
        styles.container,
        {
          width: size,
          height: size,
          borderRadius: size / 2,
        },
        animatedStyle,
      ]}
      testID="success-checkmark"
    >
      <Text style={[styles.checkmark, { fontSize: size * 0.5 }]}>✓</Text>
    </Animated.View>
  );
};

const styles = StyleSheet.create({
  container: {
    backgroundColor: colors.success,
    justifyContent: 'center',
    alignItems: 'center',
  },
  checkmark: {
    color: colors.textInverse,
    fontWeight: '700',
  },
});

export default SuccessCheckmark;
