import React, { useEffect, useRef } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';

interface SnackbarProps {
  readonly message: string;
  readonly actionLabel?: string;
  readonly onAction?: () => void;
  readonly onDismiss: () => void;
  readonly durationMs?: number;
}

const AUTO_DISMISS_MS = 5000;

const Snackbar: React.FC<SnackbarProps> = ({
  message,
  actionLabel,
  onAction,
  onDismiss,
  durationMs = AUTO_DISMISS_MS,
}) => {
  const styles = useThemedStyles(makeStyles);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    timerRef.current = setTimeout(() => {
      onDismiss();
    }, durationMs);

    return () => {
      if (timerRef.current) clearTimeout(timerRef.current);
    };
  }, [durationMs, onDismiss]);

  const handleAction = () => {
    if (timerRef.current) clearTimeout(timerRef.current);
    onAction?.();
  };

  return (
    <View style={styles.container}>
      <Text style={styles.message} numberOfLines={2}>
        {message}
      </Text>
      {actionLabel && onAction && (
        <Pressable
          style={({ pressed }) => [styles.actionBtn, pressed && styles.pressed]}
          onPress={handleAction}
          accessibilityRole="button"
          accessibilityLabel={actionLabel}
          testID="snackbar-action"
        >
          <Text style={styles.actionText}>{actionLabel}</Text>
        </Pressable>
      )}
    </View>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      backgroundColor: '#1A1A1A',
      borderRadius: borderRadius.md,
      paddingHorizontal: spacing.lg,
      paddingVertical: spacing.md,
      marginHorizontal: spacing.lg,
      marginBottom: spacing.sm,
      boxShadow: '0 2px 8px rgba(0,0,0,0.25)',
    },
    message: {
      flex: 1,
      color: '#FFFFFF',
      fontSize: 13,
      fontWeight: '500',
      marginRight: spacing.md,
    },
    actionBtn: {
      minHeight: 44,
      minWidth: 44,
      paddingHorizontal: spacing.sm,
      alignItems: 'center',
      justifyContent: 'center',
    },
    actionText: {
      color: t.colors.info,
      fontSize: 13,
      fontWeight: '800',
      textTransform: 'uppercase',
    },
    pressed: {
      opacity: 0.7,
    },
  });

export default Snackbar;
