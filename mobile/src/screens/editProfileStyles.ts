import { StyleSheet } from 'react-native';
import { borderRadius, spacing } from '../styles/theme';
import type { Theme } from '../styles/theme';

export const makeEditProfileStyles = (t: Theme) =>
  StyleSheet.create({
    sectionBody: {
      paddingHorizontal: spacing.lg,
    },
    saveButton: {
      backgroundColor: t.colors.primary,
      borderRadius: borderRadius.sm,
      minHeight: 48,
      alignItems: 'center',
      justifyContent: 'center',
      paddingVertical: 14,
      paddingHorizontal: spacing.lg,
      marginTop: spacing.sm,
    },
    saveButtonDisabled: {
      opacity: 0.5,
    },
    saveButtonText: {
      color: t.colors.surface,
      fontSize: 16,
      fontWeight: '600',
    },
  });
