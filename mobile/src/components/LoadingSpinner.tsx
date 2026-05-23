import React from 'react';
import {ActivityIndicator, StyleProp, StyleSheet, Text, View, ViewStyle} from 'react-native';
import { colors, spacing, typography } from '../styles/theme';

interface LoadingSpinnerProps {
  size?: 'small' | 'large';
  color?: string;
  style?: StyleProp<ViewStyle>;
  text?: string;
}

const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({
  size = 'large',
  color = colors.primary,
  style,
  text,
}) => {
  return (
    <View style={[styles.container, style]} testID="loading-spinner">
      <ActivityIndicator size={size} color={color} testID="loading-indicator" />
      {text && (
        <Text style={styles.text} testID="loading-text">
          {text}
        </Text>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  text: {
    ...typography.bodySmall,
    color: colors.textSecondary,
    marginTop: spacing.md,
  },
});

export default LoadingSpinner;
