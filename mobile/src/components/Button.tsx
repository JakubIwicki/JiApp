import React, { useCallback } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text } from 'react-native';
import Animated, {
  useSharedValue,
  withSpring,
  useAnimatedStyle,
} from 'react-native-reanimated';
import { useTheme, useThemedStyles } from '../context/ThemeContext';
import type { Theme } from '../styles/theme';
import { animation } from '../styles/theme';

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
  const scaleValue = useSharedValue(1);
  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);

  const handlePressIn = useCallback(() => {
    scaleValue.value = withSpring(0.96, animation.spring.bouncy);
  }, [scaleValue]);

  const handlePressOut = useCallback(() => {
    scaleValue.value = withSpring(1, animation.spring.bouncy);
  }, [scaleValue]);

  const animatedStyle = useAnimatedStyle(() => ({
    transform: [{ scale: scaleValue.value }],
  }));

  return (
    <Pressable
      onPress={onPress}
      disabled={isDisabled}
      onPressIn={handlePressIn}
      onPressOut={handlePressOut}
      style={({ pressed }) => pressed && { opacity: 0.8 }}
      accessibilityRole="button"
      testID="button"
    >
      <Animated.View
        style={[
          styles.button,
          variant === 'outline' && styles.outlineButton,
          isDisabled && styles.disabled,
          animatedStyle,
        ]}
      >
        {loading ? (
          <ActivityIndicator
            color={variant === 'outline' ? colors.primary : colors.textInverse}
            testID="button-loading"
            size="small"
          />
        ) : (
          <Text
            style={[styles.text, variant === 'outline' && styles.outlineText]}
          >
            {title}
          </Text>
        )}
      </Animated.View>
    </Pressable>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    button: {
      width: '100%',
      height: 48,
      backgroundColor: t.colors.primary,
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
      borderColor: t.colors.primary,
    },
    outlineText: {
      color: t.colors.primary,
    },
    text: {
      color: t.colors.surface,
      fontSize: 16,
      fontWeight: '600',
    },
  });

export default Button;
