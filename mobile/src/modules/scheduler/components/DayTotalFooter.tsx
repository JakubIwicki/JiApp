import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { colors, typography, spacing, borderRadius } from '../../../styles/theme';
import type { DayTotal } from '../types/api';

interface DayTotalFooterProps {
  dayTotal: DayTotal;
}

const DayTotalFooter: React.FC<DayTotalFooterProps> = ({ dayTotal }) => (
  <View style={styles.container}>
    <View style={styles.row}>
      <Text style={styles.label}>Revenue</Text>
      <Text style={styles.value}>{dayTotal.revenue.toFixed(0)} PLN</Text>
    </View>
    <View style={styles.row}>
      <Text style={styles.label}>Expenses</Text>
      <Text style={[styles.value, styles.expenseValue]}>
        -{dayTotal.expenses.toFixed(0)} PLN
      </Text>
    </View>
    <View style={styles.divider} />
    <View style={styles.row}>
      <Text style={[styles.label, styles.netLabel]}>Net</Text>
      <Text
        style={[
          styles.value,
          styles.netValue,
          dayTotal.net >= 0 ? styles.positive : styles.negative,
        ]}
      >
        {dayTotal.net.toFixed(0)} PLN
      </Text>
    </View>
  </View>
);

const styles = StyleSheet.create({
  container: {
    backgroundColor: colors.surface,
    borderRadius: borderRadius.md,
    padding: spacing.md,
    marginHorizontal: spacing.lg,
    marginVertical: spacing.xs,
  },
  row: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 4,
  },
  label: {
    ...typography.caption,
    color: colors.textSecondary,
  },
  value: {
    ...typography.caption,
    fontWeight: '600',
    color: colors.textPrimary,
  },
  expenseValue: {
    color: colors.warning,
  },
  divider: {
    height: StyleSheet.hairlineWidth,
    backgroundColor: colors.separator,
    marginVertical: 4,
  },
  netLabel: {
    fontWeight: '700',
  },
  netValue: {
    fontWeight: '700',
  },
  positive: {
    color: colors.success,
  },
  negative: {
    color: colors.error,
  },
});

export default DayTotalFooter;
