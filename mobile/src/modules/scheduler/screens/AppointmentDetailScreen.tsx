import React, { useEffect, useState } from 'react';
import {
  View,
  Text,
  Pressable,
  ScrollView,
  Alert,
  StyleSheet,
} from 'react-native';
import { useNavigation, useRoute, RouteProp } from '@react-navigation/native';
import * as appointmentService from '../services/appointmentService';
import { useTheme, useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';
import type { Appointment } from '../types/api';
import type { SchedulerStackParamList } from '../types/navigation';

type DetailRoute = RouteProp<SchedulerStackParamList, 'AppointmentDetail'>;

const AppointmentDetailScreen: React.FC = () => {
  const navigation = useNavigation();
  const route = useRoute<DetailRoute>();
  const { appointmentId } = route.params;
  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);

  const STATUS_COLORS: Record<string, string> = {
    Created: colors.primary,
    Done: colors.success,
    Cancelled: colors.error,
  };

  const [appointment, setAppointment] = useState<Appointment | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    appointmentService
      .getAppointment(appointmentId)
      .then(setAppointment)
      .catch(() => Alert.alert('Error', 'Failed to load appointment'))
      .finally(() => setIsLoading(false));
  }, [appointmentId]);

  const handleMarkDone = async () => {
    try {
      await appointmentService.updateStatus(appointmentId, 'Done');
      setAppointment(prev => (prev ? { ...prev, status: 'Done' } : prev));
    } catch {
      Alert.alert('Error', 'Failed to update status');
    }
  };

  const handleCancel = async () => {
    Alert.alert('Cancel Appointment', 'Are you sure?', [
      { text: 'No', style: 'cancel' },
      {
        text: 'Yes',
        style: 'destructive',
        onPress: async () => {
          try {
            await appointmentService.updateStatus(appointmentId, 'Cancelled');
            setAppointment(prev =>
              prev ? { ...prev, status: 'Cancelled' } : prev,
            );
          } catch {
            Alert.alert('Error', 'Failed to cancel appointment');
          }
        },
      },
    ]);
  };

  const handleDelete = async () => {
    Alert.alert('Delete Appointment', 'This cannot be undone.', [
      { text: 'Cancel', style: 'cancel' },
      {
        text: 'Delete',
        style: 'destructive',
        onPress: async () => {
          try {
            await appointmentService.deleteAppointment(appointmentId);
            navigation.goBack();
          } catch {
            Alert.alert('Error', 'Failed to delete');
          }
        },
      },
    ]);
  };

  if (isLoading) {
    return (
      <View style={styles.center}>
        <Text style={styles.loadingText}>Loading…</Text>
      </View>
    );
  }

  if (!appointment) {
    return (
      <View style={styles.center}>
        <Text style={styles.errorText}>Appointment not found</Text>
      </View>
    );
  }

  const statusColor = STATUS_COLORS[appointment.status] || colors.textSecondary;

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      {/* Status badge */}
      <View style={[styles.statusBadge, { backgroundColor: statusColor }]}>
        <Text style={styles.statusText}>{appointment.status}</Text>
      </View>

      {/* Client info */}
      <Text style={styles.sectionTitle}>Client</Text>
      <View style={styles.card}>
        <Text style={styles.cardTitle}>{appointment.client.name}</Text>
        {appointment.client.phone ? (
          <Text style={styles.cardDetail}>{appointment.client.phone}</Text>
        ) : null}
      </View>

      {/* Service info */}
      <Text style={styles.sectionTitle}>Service</Text>
      <View style={styles.card}>
        <Text style={styles.cardTitle}>{appointment.service.name}</Text>
        <Text style={styles.cardDetail}>
          {appointment.service.category} | {appointment.service.baseDuration}{' '}
          min
        </Text>
      </View>

      {/* Time & Date */}
      <Text style={styles.sectionTitle}>When</Text>
      <View style={styles.card}>
        <Text style={styles.cardTitle}>
          {appointment.date} | {appointment.startTime} - {appointment.endTime}
        </Text>
      </View>

      {/* Price */}
      <Text style={styles.sectionTitle}>Price</Text>
      <View style={styles.card}>
        <Text style={styles.priceValue}>
          {appointment.price.amount.toFixed(0)} {appointment.price.currency}
        </Text>
      </View>

      {/* Location */}
      {appointment.location ? (
        <>
          <Text style={styles.sectionTitle}>Location</Text>
          <View style={styles.card}>
            <Text style={styles.cardTitle}>{appointment.location}</Text>
          </View>
        </>
      ) : null}

      {/* Description */}
      {appointment.description ? (
        <>
          <Text style={styles.sectionTitle}>Notes</Text>
          <View style={styles.card}>
            <Text style={styles.cardDetail}>{appointment.description}</Text>
          </View>
        </>
      ) : null}

      {/* Actions */}
      <View style={styles.actions}>
        {appointment.status === 'Created' ? (
          <>
            <Pressable
              style={({ pressed }) => [
                styles.doneButton,
                pressed && { opacity: 0.7 },
              ]}
              onPress={handleMarkDone}
            >
              <Text style={styles.actionButtonText}>Mark Done</Text>
            </Pressable>
            <Pressable
              style={({ pressed }) => [
                styles.cancelButton,
                pressed && { opacity: 0.7 },
              ]}
              onPress={handleCancel}
            >
              <Text style={styles.cancelButtonText}>Cancel</Text>
            </Pressable>
          </>
        ) : null}
        <Pressable
          style={({ pressed }) => [
            styles.deleteButton,
            pressed && { opacity: 0.7 },
          ]}
          onPress={handleDelete}
        >
          <Text style={styles.deleteButtonText}>Delete</Text>
        </Pressable>
      </View>
    </ScrollView>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      flex: 1,
      backgroundColor: t.colors.background,
    },
    content: {
      padding: spacing.lg,
      paddingBottom: spacing.xxl,
    },
    center: {
      flex: 1,
      alignItems: 'center',
      justifyContent: 'center',
      backgroundColor: t.colors.background,
    },
    loadingText: {
      ...t.typography.body,
      color: t.colors.textSecondary,
    },
    errorText: {
      ...t.typography.body,
      color: t.colors.error,
    },
    statusBadge: {
      alignSelf: 'flex-start',
      borderRadius: borderRadius.md,
      paddingHorizontal: spacing.md,
      paddingVertical: spacing.xs,
      marginBottom: spacing.lg,
    },
    statusText: {
      ...t.typography.caption,
      color: t.colors.textInverse,
      fontWeight: '700',
      textTransform: 'uppercase',
    },
    sectionTitle: {
      ...t.typography.label,
      color: t.colors.textSecondary,
      textTransform: 'uppercase',
      letterSpacing: 0.5,
      marginBottom: spacing.sm,
      marginTop: spacing.md,
    },
    card: {
      backgroundColor: t.colors.surface,
      borderRadius: borderRadius.lg,
      padding: spacing.lg,
    },
    cardTitle: {
      ...t.typography.body,
      color: t.colors.textPrimary,
      fontWeight: '600',
    },
    cardDetail: {
      ...t.typography.bodySmall,
      color: t.colors.textSecondary,
      marginTop: 4,
    },
    priceValue: {
      ...t.typography.heading,
      color: t.colors.success,
    },
    actions: {
      marginTop: spacing.xl,
      gap: spacing.md,
    },
    doneButton: {
      backgroundColor: t.colors.success,
      borderRadius: borderRadius.lg,
      paddingVertical: 14,
      alignItems: 'center',
    },
    cancelButton: {
      backgroundColor: t.colors.surface,
      borderRadius: borderRadius.lg,
      paddingVertical: 14,
      alignItems: 'center',
      borderWidth: 1,
      borderColor: t.colors.error,
    },
    deleteButton: {
      alignItems: 'center',
      paddingVertical: 14,
    },
    actionButtonText: {
      ...t.typography.body,
      color: t.colors.textInverse,
      fontWeight: '700',
    },
    cancelButtonText: {
      ...t.typography.body,
      color: t.colors.error,
      fontWeight: '600',
    },
    deleteButtonText: {
      ...t.typography.body,
      color: t.colors.error,
    },
  });

export default AppointmentDetailScreen;
