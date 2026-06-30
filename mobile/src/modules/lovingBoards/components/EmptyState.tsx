import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing } from '../../../styles/theme';

interface EmptyStateProps {
  readonly emoji: string;
  readonly title: string;
  readonly subtitle?: string;
  readonly actionLabel?: string;
  readonly onAction?: () => void;
  readonly testID?: string;
}

const MIN_TOUCH = 44;

const EmptyState: React.FC<EmptyStateProps> = ({
  emoji,
  title,
  subtitle,
  actionLabel,
  onAction,
  testID,
}) => {
  const styles = useThemedStyles(makeStyles);

  return (
    <View style={styles.container} testID={testID}>
      <Text style={styles.emoji}>{emoji}</Text>
      <Text style={styles.title}>{title}</Text>
      {subtitle && <Text style={styles.subtitle}>{subtitle}</Text>}
      {actionLabel && onAction && (
        <Pressable
          style={({ pressed }) => [styles.actionBtn, pressed && styles.pressed]}
          onPress={onAction}
          accessibilityRole="button"
          accessibilityLabel={actionLabel}
          testID={`${testID}-action`}
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
      alignItems: 'center',
      paddingVertical: spacing.xxl,
      paddingHorizontal: spacing.xl,
    },
    emoji: {
      fontSize: 40,
      marginBottom: spacing.md,
    },
    title: {
      ...t.typography.body,
      color: t.colors.textSecondary,
      textAlign: 'center',
    },
    subtitle: {
      ...t.typography.caption,
      color: t.colors.textTertiary,
      textAlign: 'center',
      marginTop: spacing.xs,
    },
    actionBtn: {
      marginTop: spacing.lg,
      minHeight: MIN_TOUCH,
      minWidth: MIN_TOUCH,
      paddingHorizontal: spacing.lg,
      paddingVertical: spacing.sm,
      borderRadius: 8,
      borderWidth: 1,
      borderColor: t.colors.border,
      alignItems: 'center',
      justifyContent: 'center',
    },
    actionText: {
      ...t.typography.link,
      color: t.colors.primary,
    },
    pressed: {
      opacity: 0.7,
    },
  });

export default EmptyState;
