import React, { useCallback, useRef } from 'react';
import {
  ActivityIndicator,
  Animated,
  StyleSheet,
  Text,
  TouchableOpacity,
} from 'react-native';
import { animation, colors } from '../styles/theme';

interface ButtonProps {
  title: string;
  onPress: () => void;
  disabled?: boolean;
  loading?: boolean;
  variant?: 'primary' | 'outline';
}

const Button: React.FC<ButtonProps> = ({
  title,
  onPress,
  disabled = false,
  loading = false,
  variant = 'primary',
}) => {
  const isDisabled = disabled || loading;
  const scaleValue = useRef(new Animated.Value(1)).current;

  const handlePressIn = useCallback(() => {
    Animated.spring(scaleValue, {
      toValue: 0.96,
      ...animation.spring.bouncy,
    }).start();
  }, [scaleValue]);

  const handlePressOut = useCallback(() => {
    Animated.spring(scaleValue, {
      toValue: 1,
      ...animation.spring.bouncy,
    }).start();
  }, [scaleValue]);

  return (
    <TouchableOpacity
      onPress={onPress}
      disabled={isDisabled}
      onPressIn={handlePressIn}
      onPressOut={handlePressOut}
      activeOpacity={0.8}
      accessibilityRole="button"
      testID="button"
    >
      <Animated.View
        style={[
          styles.button,
          variant === 'outline' && styles.outlineButton,
          isDisabled && styles.disabled,
          { transform: [{ scale: scaleValue }] },
        ]}
      >
        {loading ? (
          <ActivityIndicator
            color={variant === 'outline' ? colors.primary : '#FFFFFF'}
            testID="button-loading"
            size="small"
          />
        ) : (
          <Text style={[styles.text, variant === 'outline' && styles.outlineText]}>{title}</Text>
        )}
      </Animated.View>
    </TouchableOpacity>
  );
};

const styles = StyleSheet.create({
  button: {
    width: '100%',
    height: 48,
    backgroundColor: colors.primary,
    borderRadius: 8,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 16,
  },
  disabled: {
    opacity: 0.5,
  },
  outlineButton: {
    backgroundColor: 'transparent',
    borderWidth: 1.5,
    borderColor: colors.primary,
  },
  outlineText: {
    color: colors.primary,
  },
  text: {
    color: colors.surface,
    fontSize: 16,
    fontWeight: '600',
  },
});

export default Button;
