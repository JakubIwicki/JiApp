import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';
import type { Expense } from '../types/api';

interface ExpenseCardProps {
  expense: Expense;
  onPress?: (expense: Expense) => void;
}

const ExpenseCard: React.FC<ExpenseCardProps> = ({ expense, onPress }) => {
  const styles = useThemedStyles(makeStyles);
  const categoryLabel = expense.category;
  const note = expense.note?.trim();

  return (
    <View style={styles.card}>
      <View style={styles.leftBorder} />
      <View style={styles.content}>
        <View style={styles.topRow}>
          <Text style={styles.categoryText}>{categoryLabel}</Text>
          <Text style={styles.amountText}>
            -{expense.amount.amount.toFixed(0)} {expense.amount.currency}
          </Text>
        </View>
        {note ? <Text style={styles.noteText}>{note}</Text> : null}
      </View>
    </View>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    card: {
      flexDirection: 'row',
      backgroundColor: t.colors.surface,
      borderRadius: borderRadius.md,
      marginHorizontal: spacing.lg,
      marginVertical: spacing.xs,
      overflow: 'hidden',
    },
    leftBorder: {
      width: 4,
      backgroundColor: t.colors.warning,
    },
    content: {
      flex: 1,
      paddingVertical: spacing.sm,
      paddingHorizontal: spacing.md,
    },
    topRow: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
    },
    categoryText: {
      ...t.typography.bodySmall,
      fontWeight: '600',
      color: t.colors.textPrimary,
    },
    amountText: {
      ...t.typography.bodySmall,
      fontWeight: '700',
      color: t.colors.warning,
    },
    noteText: {
      ...t.typography.caption,
      color: t.colors.textTertiary,
      marginTop: 2,
    },
  });

export default ExpenseCard;
