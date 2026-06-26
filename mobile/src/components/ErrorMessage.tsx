import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useThemedStyles } from '../context/ThemeContext';
import type { Theme } from '../styles/theme';

interface ErrorMessageProps {
  message: string;
  onRetry?: () => void;
}

const ErrorMessage: React.FC<ErrorMessageProps> = ({ message, onRetry }) => {
  const { t } = useTranslation();
  const styles = useThemedStyles(makeStyles);

  return (
    <View style={styles.container} testID="error-message">
      <Text style={styles.icon}>!</Text>
      <Text style={styles.message}>{message}</Text>
      {onRetry && (
        <Pressable
          style={({ pressed }) => [
            styles.retryButton,
            pressed && { opacity: 0.7 },
          ]}
          onPress={onRetry}
          testID="error-retry-button"
          accessibilityRole="button"
        >
          <Text style={styles.retryText}>{t('common.retry')}</Text>
        </Pressable>
      )}
    </View>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      alignItems: 'center',
      justifyContent: 'center',
      padding: 32,
    },
    icon: {
      fontSize: 40,
      fontWeight: '700',
      color: t.colors.error,
      marginBottom: 12,
      width: 60,
      height: 60,
      lineHeight: 60,
      textAlign: 'center',
      borderRadius: 30,
      backgroundColor: t.colors.errorLight,
      overflow: 'hidden',
    },
    message: {
      fontSize: 14,
      color: t.colors.textSecondary,
      textAlign: 'center',
      marginBottom: 16,
      lineHeight: 20,
    },
    retryButton: {
      backgroundColor: t.colors.primary,
      borderRadius: 8,
      paddingHorizontal: 24,
      paddingVertical: 10,
    },
    retryText: {
      color: t.colors.surface,
      fontSize: 14,
      fontWeight: '600',
    },
  });

export default ErrorMessage;
