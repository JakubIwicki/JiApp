import React, { useEffect } from 'react';
import { StyleSheet, Text } from 'react-native';
import Animated, {
  useSharedValue,
  withSpring,
  useAnimatedStyle,
} from 'react-native-reanimated';
import { useThemedStyles } from '../context/ThemeContext';
import type { Theme } from '../styles/theme';
import { animation } from '../styles/theme';

interface SuccessCheckmarkProps {
  size?: number;
}

const SuccessCheckmark: React.FC<SuccessCheckmarkProps> = ({ size = 64 }) => {
  const scaleAnim = useSharedValue(0);
  const styles = useThemedStyles(makeStyles);

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

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      backgroundColor: t.colors.success,
      justifyContent: 'center',
      alignItems: 'center',
    },
    checkmark: {
      color: t.colors.textInverse,
      fontWeight: '700',
    },
  });

export default SuccessCheckmark;
