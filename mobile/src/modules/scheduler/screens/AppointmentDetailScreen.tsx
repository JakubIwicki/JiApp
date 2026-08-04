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
import { useTranslation } from 'react-i18next';
import * as appointmentService from '../services/appointmentService';
import { useTheme, useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';
import type { Appointment } from '../types/api';
import type { SchedulerStackParamList } from '../types/navigation';

type DetailRoute = RouteProp<SchedulerStackParamList, 'AppointmentDetail'>;

const STATUS_LABELS: Record<string, string> = {
  Created: 'scheduler.status.created',
  Done: 'scheduler.status.done',
  Cancelled: 'scheduler.status.cancelled',
};

const CATEGORY_LABELS: Record<string, string> = {
  MensHaircut: 'scheduler.category.mensHaircut',
  WomensHaircut: 'scheduler.category.womensHaircut',
  WomensStyling: 'scheduler.category.womensStyling',
  Coloring: 'scheduler.category.coloring',
  Treatment: 'scheduler.category.treatment',
  Other: 'scheduler.category.other',
};

const AppointmentDetailScreen: React.FC = () => {
  const navigation = useNavigation();
  const route = useRoute<DetailRoute>();
  const { appointmentId } = route.params;
  const { t } = useTranslation();
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
      .catch(() =>
        Alert.alert(
          t('scheduler.error'),
          t('scheduler.appointmentDetail.loadError'),
        ),
      )
      .finally(() => setIsLoading(false));
  }, [appointmentId, t]);

  const handleMarkDone = async () => {
    try {
      await appointmentService.updateStatus(appointmentId, 'Done');
      setAppointment(prev => (prev ? { ...prev, status: 'Done' } : prev));
    } catch {
      Alert.alert(
        t('scheduler.error'),
        t('scheduler.appointmentDetail.updateError'),
      );
    }
  };

  const handleCancel = async () => {
    Alert.alert(
      t('scheduler.appointmentDetail.cancelTitle'),
      t('scheduler.appointmentDetail.cancelConfirm'),
      [
        { text: t('scheduler.appointmentDetail.no'), style: 'cancel' },
        {
          text: t('scheduler.appointmentDetail.yes'),
          style: 'destructive',
          onPress: async () => {
            try {
              await appointmentService.updateStatus(appointmentId, 'Cancelled');
              setAppointment(prev =>
                prev ? { ...prev, status: 'Cancelled' } : prev,
              );
            } catch {
              Alert.alert(
                t('scheduler.error'),
                t('scheduler.appointmentDetail.cancelFailed'),
              );
            }
          },
        },
      ],
    );
  };

  const handleDelete = async () => {
    Alert.alert(
      t('scheduler.appointmentDetail.deleteTitle'),
      t('scheduler.appointmentDetail.deleteConfirm'),
      [
        { text: t('common.cancel'), style: 'cancel' },
        {
          text: t('scheduler.appointmentDetail.delete'),
          style: 'destructive',
          onPress: async () => {
            try {
              await appointmentService.deleteAppointment(appointmentId);
              navigation.goBack();
            } catch {
              Alert.alert(
                t('scheduler.error'),
                t('scheduler.appointmentDetail.deleteFailed'),
              );
            }
          },
        },
      ],
    );
  };

  if (isLoading) {
    return (
      <View style={styles.center}>
        <Text style={styles.loadingText}>{t('common.loading')}</Text>
      </View>
    );
  }

  if (!appointment) {
    return (
      <View style={styles.center}>
        <Text style={styles.errorText}>
          {t('scheduler.appointmentDetail.notFound')}
        </Text>
      </View>
    );
  }

  const statusColor = STATUS_COLORS[appointment.status] || colors.textSecondary;
  const statusLabel = t(
    STATUS_LABELS[appointment.status] ?? appointment.status,
  );

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      {/* Status badge */}
      <View style={[styles.statusBadge, { backgroundColor: statusColor }]}>
        <Text style={styles.statusText}>{statusLabel}</Text>
      </View>

      {/* Client info */}
      <Text style={styles.sectionTitle}>
        {t('scheduler.appointmentDetail.client')}
      </Text>
      <View style={styles.card}>
        <Text style={styles.cardTitle}>{appointment.client.name}</Text>
        {appointment.client.phone ? (
          <Text style={styles.cardDetail}>{appointment.client.phone}</Text>
        ) : null}
      </View>

      {/* Service info */}
      <Text style={styles.sectionTitle}>
        {t('scheduler.appointmentDetail.service')}
      </Text>
      <View style={styles.card}>
        <Text style={styles.cardTitle}>{appointment.service.name}</Text>
        <Text style={styles.cardDetail}>
          {t('scheduler.appointmentDetail.serviceDetail', {
            category: t(
              CATEGORY_LABELS[appointment.service.category] ??
                appointment.service.category,
            ),
            duration: appointment.service.baseDuration,
          })}
        </Text>
      </View>

      {/* Time & Date */}
      <Text style={styles.sectionTitle}>
        {t('scheduler.appointmentDetail.when')}
      </Text>
      <View style={styles.card}>
        <Text style={styles.cardTitle}>
          {appointment.date} | {appointment.startTime} - {appointment.endTime}
        </Text>
      </View>

      {/* Price */}
      <Text style={styles.sectionTitle}>
        {t('scheduler.appointmentDetail.price')}
      </Text>
      <View style={styles.card}>
        <Text style={styles.priceValue}>
          {appointment.price.amount.toFixed(0)} {appointment.price.currency}
        </Text>
      </View>

      {/* Location */}
      {appointment.location ? (
        <>
          <Text style={styles.sectionTitle}>
            {t('scheduler.appointmentDetail.location')}
          </Text>
          <View style={styles.card}>
            <Text style={styles.cardTitle}>{appointment.location}</Text>
          </View>
        </>
      ) : null}

      {/* Description */}
      {appointment.description ? (
        <>
          <Text style={styles.sectionTitle}>
            {t('scheduler.appointmentDetail.notes')}
          </Text>
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
              <Text style={styles.actionButtonText}>
                {t('scheduler.appointmentDetail.markDone')}
              </Text>
            </Pressable>
            <Pressable
              style={({ pressed }) => [
                styles.cancelButton,
                pressed && { opacity: 0.7 },
              ]}
              onPress={handleCancel}
            >
              <Text style={styles.cancelButtonText}>{t('common.cancel')}</Text>
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
          <Text style={styles.deleteButtonText}>
            {t('scheduler.appointmentDetail.delete')}
          </Text>
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
