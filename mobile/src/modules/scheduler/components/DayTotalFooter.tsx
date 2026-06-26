import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';
import type { DayTotal } from '../types/api';

interface DayTotalFooterProps {
  dayTotal: DayTotal;
}

const DayTotalFooter: React.FC<DayTotalFooterProps> = ({ dayTotal }) => {
  const styles = useThemedStyles(makeStyles);
  return (
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
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      backgroundColor: t.colors.surface,
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
      ...t.typography.caption,
      color: t.colors.textSecondary,
    },
    value: {
      ...t.typography.caption,
      fontWeight: '600',
      color: t.colors.textPrimary,
    },
    expenseValue: {
      color: t.colors.warning,
    },
    divider: {
      height: StyleSheet.hairlineWidth,
      backgroundColor: t.colors.separator,
      marginVertical: 4,
    },
    netLabel: {
      fontWeight: '700',
    },
    netValue: {
      fontWeight: '700',
    },
    positive: {
      color: t.colors.success,
    },
    negative: {
      color: t.colors.error,
    },
  });

export default DayTotalFooter;
