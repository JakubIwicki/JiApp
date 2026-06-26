import React from 'react';
import { View, Text, FlatList, StyleSheet } from 'react-native';
import AppointmentCard from './AppointmentCard';
import ExpenseCard from './ExpenseCard';
import DayTotalFooter from './DayTotalFooter';
import { useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';
import type { Appointment, Expense, DayTotal } from '../types/api';

interface DayColumnProps {
  label: string;
  date: string;
  appointments: Appointment[];
  expenses: Expense[];
  dayTotal: DayTotal;
  onAppointmentPress: (appointment: Appointment) => void;
  isToday?: boolean;
}

const DayColumn: React.FC<DayColumnProps> = ({
  label,
  date,
  appointments,
  expenses,
  dayTotal,
  onAppointmentPress,
  isToday,
}) => {
  const styles = useThemedStyles(makeStyles);
  const dayAppointments = appointments.filter(a => a.date === date);
  const dayExpenses = expenses.filter(e => e.date === date);

  return (
    <View style={styles.container}>
      <View style={[styles.header, isToday && styles.headerToday]}>
        <Text style={[styles.headerLabel, isToday && styles.headerLabelToday]}>
          {label}
        </Text>
        {isToday ? <Text style={styles.todayDot}>•</Text> : null}
      </View>

      {dayAppointments.length === 0 && dayExpenses.length === 0 ? (
        <View style={styles.emptyContainer}>
          <Text style={styles.emptyText}>No items</Text>
        </View>
      ) : (
        <View style={styles.list}>
          {dayAppointments.map(appt => (
            <AppointmentCard
              key={`appt-${appt.id}`}
              appointment={appt}
              onPress={onAppointmentPress}
            />
          ))}
          {dayExpenses.map(exp => (
            <ExpenseCard key={`exp-${exp.id}`} expense={exp} />
          ))}
        </View>
      )}

      <DayTotalFooter dayTotal={dayTotal} />
    </View>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      flex: 1,
      marginHorizontal: 4,
    },
    header: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'center',
      paddingVertical: spacing.sm,
      backgroundColor: t.colors.surface,
      borderRadius: borderRadius.md,
      marginBottom: spacing.sm,
    },
    headerToday: {
      backgroundColor: t.colors.primaryLight,
    },
    headerLabel: {
      ...t.typography.body,
      fontWeight: '700',
      color: t.colors.textPrimary,
    },
    headerLabelToday: {
      color: t.colors.primary,
    },
    todayDot: {
      fontSize: 18,
      color: t.colors.primary,
      marginLeft: 4,
    },
    emptyContainer: {
      paddingVertical: spacing.xl,
      alignItems: 'center',
    },
    emptyText: {
      ...t.typography.caption,
      color: t.colors.textTertiary,
    },
    list: {
      marginBottom: spacing.sm,
    },
  });

export default DayColumn;
