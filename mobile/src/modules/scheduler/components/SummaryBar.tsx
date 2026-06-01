import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { colors, typography, spacing, borderRadius } from '../../../styles/theme';
import type { DayTotal } from '../types/api';

interface SummaryBarProps {
  saturdayTotal: DayTotal;
  sundayTotal: DayTotal;
}

function formatCurrency(amount: number): string {
  const abs = Math.abs(amount);
  const prefix = amount < 0 ? '-' : '';
  return `${prefix}${abs.toFixed(0)} PLN`;
}

const SummaryBar: React.FC<SummaryBarProps> = ({ saturdayTotal, sundayTotal }) => {
  const total = {
    appointments: 0, // We don't compute this here; derived from props
    revenue: saturdayTotal.revenue + sundayTotal.revenue,
    expenses: saturdayTotal.expenses + sundayTotal.expenses,
    net: saturdayTotal.net + sundayTotal.net,
  };

  return (
    <View style={styles.container}>
      <View style={styles.row}>
        <SummaryCell
          label="Revenue"
          value={formatCurrency(total.revenue)}
          color={colors.success}
        />
        <SummaryCell
          label="Expenses"
          value={formatCurrency(total.expenses)}
          color={colors.warning}
        />
      </View>
      <View style={styles.row}>
        <SummaryCell
          label="Net Profit"
          value={formatCurrency(total.net)}
          color={total.net >= 0 ? colors.success : colors.error}
        />
        <SummaryCell
          label="Weekend"
          value={`${saturdayTotal.revenue.toFixed(0)} / ${sundayTotal.revenue.toFixed(0)}`}
          color={colors.textSecondary}
        />
      </View>
    </View>
  );
};

interface SummaryCellProps {
  label: string;
  value: string;
  color: string;
}

const SummaryCell: React.FC<SummaryCellProps> = ({ label, value, color }) => (
  <View style={styles.cell}>
    <Text style={styles.cellLabel}>{label}</Text>
    <Text style={[styles.cellValue, { color }]}>{value}</Text>
  </View>
);

const styles = StyleSheet.create({
  container: {
    backgroundColor: colors.surface,
    marginHorizontal: spacing.lg,
    marginVertical: spacing.sm,
    borderRadius: borderRadius.lg,
    padding: spacing.md,
  },
  row: {
    flexDirection: 'row',
    marginBottom: spacing.sm,
  },
  cell: {
    flex: 1,
    alignItems: 'center',
    paddingVertical: spacing.xs,
  },
  cellLabel: {
    ...typography.caption,
    color: colors.textSecondary,
    marginBottom: 2,
  },
  cellValue: {
    ...typography.body,
    fontWeight: '700',
  },
});

export default SummaryBar;
