import React from 'react';
import {
  ActivityIndicator,
  StyleProp,
  StyleSheet,
  Text,
  View,
  ViewStyle,
} from 'react-native';
import { useTheme, useThemedStyles } from '../context/ThemeContext';
import type { Theme } from '../styles/theme';
import { spacing } from '../styles/theme';

interface LoadingSpinnerProps {
  size?: 'small' | 'large';
  color?: string;
  style?: StyleProp<ViewStyle>;
  text?: string;
}

const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({
  size = 'large',
  color: colorProp,
  style,
  text,
}) => {
  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);
  const color = colorProp ?? colors.primary;

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

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      flex: 1,
      justifyContent: 'center',
      alignItems: 'center',
    },
    text: {
      ...t.typography.bodySmall,
      color: t.colors.textSecondary,
      marginTop: spacing.md,
    },
  });

export default LoadingSpinner;
