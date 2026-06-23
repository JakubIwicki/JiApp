import { StyleSheet } from 'react-native';
import { borderRadius, colors, spacing } from '../styles/theme';

const editProfileStyles = StyleSheet.create({
  saveButton: {
    backgroundColor: colors.primary,
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
    color: colors.surface,
    fontSize: 16,
    fontWeight: '600',
  },
});

export default editProfileStyles;
