import React, { useEffect, useRef } from 'react';
import { Animated, StyleSheet, Text } from 'react-native';
import { colors } from '../styles/theme';

interface SuccessCheckmarkProps {
  visible: boolean;
  size?: number;
}

const SuccessCheckmark: React.FC<SuccessCheckmarkProps> = ({
  visible,
  size = 64,
}) => {
  const scaleAnim = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    if (visible) {
      scaleAnim.setValue(0);
      Animated.spring(scaleAnim, {
        toValue: 1,
        tension: 200,
        friction: 12,
        useNativeDriver: true,
      }).start();
    }
  }, [visible, scaleAnim]);

  if (!visible) {
    return null;
  }

  return (
    <Animated.View
      style={[
        styles.container,
        {
          width: size,
          height: size,
          borderRadius: size / 2,
          transform: [{ scale: scaleAnim }],
        },
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
