import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';

type PillVariant = 'default' | 'recurring' | 'warning' | 'error';

interface PillBadgeProps {
  readonly text: string;
  readonly variant?: PillVariant;
  readonly accessibilityLabel?: string;
}

const PillBadge: React.FC<PillBadgeProps> = ({
  text,
  variant = 'default',
  accessibilityLabel,
}) => {
  const styles = useThemedStyles(makeStyles);

  const bgStyle = (() => {
    switch (variant) {
      case 'recurring':
        return styles.recurringBg;
      case 'warning':
        return styles.warningBg;
      case 'error':
        return styles.errorBg;
      default:
        return styles.defaultBg;
    }
  })();

  const textStyle = (() => {
    switch (variant) {
      case 'recurring':
        return styles.recurringText;
      case 'warning':
        return styles.warningText;
      case 'error':
        return styles.errorText;
      default:
        return styles.defaultText;
    }
  })();

  return (
    <View
      style={[styles.pill, bgStyle]}
      accessibilityLabel={accessibilityLabel}
    >
      <Text style={[styles.pillText, textStyle]}>{text}</Text>
    </View>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    pill: {
      borderRadius: borderRadius.sm,
      paddingHorizontal: spacing.sm,
      paddingVertical: 2,
    },
    pillText: {
      ...t.typography.label,
    },
    defaultBg: {
      backgroundColor: t.colors.placeholder,
    },
    defaultText: {
      color: t.colors.textSecondary,
    },
    recurringBg: {
      backgroundColor: t.colors.primaryLight,
    },
    recurringText: {
      color: t.colors.primaryDark,
    },
    warningBg: {
      backgroundColor: t.colors.warning,
    },
    warningText: {
      color: t.colors.textInverse,
    },
    errorBg: {
      backgroundColor: t.colors.errorLight,
    },
    errorText: {
      color: t.colors.error,
    },
  });

export default React.memo(PillBadge);
