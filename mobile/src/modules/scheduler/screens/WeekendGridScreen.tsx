import React, { useCallback } from 'react';
import { View, Text, ScrollView, Pressable, StyleSheet, ActivityIndicator } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import WeekendNavigator from '../components/WeekendNavigator';
import SummaryBar from '../components/SummaryBar';
import DayColumn from '../components/DayColumn';
import useWeekendGrid from '../hooks/useWeekendGrid';
import { useBoard } from '../hooks/useBoard';
import BoardSelector from '../components/BoardSelector';
import { colors, typography, spacing, borderRadius } from '../../../styles/theme';
import type { Appointment } from '../types/api';
import type { SchedulerStackParamList } from '../types/navigation';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

type NavigationProp = NativeStackNavigationProp<SchedulerStackParamList>;

const WeekendGridScreen: React.FC = () => {
  const navigation = useNavigation<NavigationProp>();
  const { selectedBoardId, isLoading: boardLoading } = useBoard();
  const boardId = selectedBoardId ?? 0;
  const {
    saturday,
    sunday,
    appointments,
    expenses,
    dayTotals,
    goToPreviousWeekend,
    goToNextWeekend,
    goToToday,
    refresh,
    isLoading,
  } = useWeekendGrid(boardId);

  const isSaturdayToday = saturday === getTodayString();
  const isSundayToday = sunday === getTodayString();

  const handleAppointmentPress = useCallback(
    (appointment: Appointment) => {
      navigation.navigate('AppointmentDetail', { appointmentId: appointment.id });
    },
    [navigation],
  );

  const handleCreateAppointment = useCallback(() => {
    if (selectedBoardId === null) return;
    navigation.navigate('CreateAppointment', { boardId: selectedBoardId });
  }, [navigation, selectedBoardId]);

  // Loading state while boards are being fetched
  if (boardLoading) {
    return (
      <View style={styles.center}>
        <ActivityIndicator size="large" color={colors.primary} />
      </View>
    );
  }

  // No board available
  if (selectedBoardId === null) {
    return (
      <View style={styles.center}>
        <Text style={styles.emptyTitle}>No board selected</Text>
        <Text style={styles.emptySubtitle}>Select or create a board to get started</Text>
        <BoardSelector />
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <WeekendNavigator
        weekLabel={`${saturday} / ${sunday}`}
        onPrevious={goToPreviousWeekend}
        onNext={goToNextWeekend}
        onToday={goToToday}
      />

      <SummaryBar
        saturdayTotal={dayTotals.saturday}
        sundayTotal={dayTotals.sunday}
      />

      {/* Header pills */}
      <View style={styles.pillRow}>
        <Pressable
          style={({ pressed }) => [styles.pill, pressed && { opacity: 0.7 }]}
          onPress={() => navigation.navigate('ClientList', { boardId: selectedBoardId })}
        >
          <Text style={styles.pillText}>Clients</Text>
        </Pressable>
        <Pressable
          style={({ pressed }) => [styles.pill, pressed && { opacity: 0.7 }]}
          onPress={() => navigation.navigate('ServiceList')}
        >
          <Text style={styles.pillText}>Services</Text>
        </Pressable>
        <Pressable
          style={({ pressed }) => [styles.pill, pressed && { opacity: 0.7 }]}
          onPress={() => navigation.navigate('Reports', { boardId: selectedBoardId })}
        >
          <Text style={styles.pillText}>Reports</Text>
        </Pressable>
      </View>

      <ScrollView style={styles.scrollArea} contentContainerStyle={styles.scrollContent}>
        <View style={styles.columnsContainer}>
          <DayColumn
            label="Saturday"
            date={saturday}
            appointments={appointments.appointments}
            expenses={expenses.expenses}
            dayTotal={dayTotals.saturday}
            onAppointmentPress={handleAppointmentPress}
            isToday={isSaturdayToday}
          />
          <DayColumn
            label="Sunday"
            date={sunday}
            appointments={appointments.appointments}
            expenses={expenses.expenses}
            dayTotal={dayTotals.sunday}
            onAppointmentPress={handleAppointmentPress}
            isToday={isSundayToday}
          />
        </View>
      </ScrollView>

      {/* FAB */}
      <Pressable
        style={({ pressed }) => [styles.fab, pressed && { opacity: 0.7 }]}
        onPress={handleCreateAppointment}
        accessibilityLabel="Create appointment"
        accessibilityRole="button"
      >
        <Text style={styles.fabText}>+</Text>
      </Pressable>
    </View>
  );
};

function getTodayString(): string {
  const d = new Date();
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  pillRow: {
    flexDirection: 'row',
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.sm,
    gap: spacing.sm,
  },
  pill: {
    backgroundColor: colors.surface,
    borderRadius: borderRadius.xl,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.sm,
    borderWidth: 1,
    borderColor: colors.border,
  },
  pillText: {
    ...typography.caption,
    color: colors.primary,
    fontWeight: '600',
  },
  scrollArea: {
    flex: 1,
  },
  scrollContent: {
    paddingVertical: spacing.sm,
  },
  columnsContainer: {
    flexDirection: 'row',
    paddingHorizontal: spacing.md,
  },
  fab: {
    position: 'absolute',
    right: spacing.xl,
    bottom: spacing.xl,
    width: 56,
    height: 56,
    borderRadius: 28,
    backgroundColor: colors.primary,
    alignItems: 'center',
    justifyContent: 'center',
    boxShadow: '0 2px 4px rgba(43,33,24,0.25)',
  },
  fabText: {
    fontSize: 28,
    color: colors.textInverse,
    lineHeight: 30,
  },
  center: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.background,
  },
  emptyTitle: {
    ...typography.body,
    color: colors.textPrimary,
    fontWeight: '600',
    marginBottom: spacing.xs,
  },
  emptySubtitle: {
    ...typography.bodySmall,
    color: colors.textTertiary,
    marginBottom: spacing.lg,
  },
});

export default WeekendGridScreen;
