import React from 'react';
import { View, Text, Pressable, StyleSheet } from 'react-native';
import { colors, typography, spacing, borderRadius } from '../../../styles/theme';
import type { Appointment } from '../types/api';

interface AppointmentCardProps {
  appointment: Appointment;
  onPress: (appointment: Appointment) => void;
}

const CATEGORY_COLORS: Record<string, string> = {
  MensHaircut: colors.success,
  WomensHaircut: colors.error,
  WomensStyling: colors.error,
  Coloring: colors.error,
  Treatment: colors.info,
  Other: colors.textSecondary,
};

const STATUS_BADGES: Record<string, { label: string; bg: string; fg: string }> = {
  Created: { label: 'New', bg: colors.primaryLight, fg: colors.primary },
  Done: { label: 'Done', bg: colors.successLight, fg: colors.success },
  Cancelled: { label: 'Cancelled', bg: colors.errorLight, fg: colors.error },
};

function formatTime(time: string): string {
  // time is "HH:mm" or "HH:mm:ss"
  return time.substring(0, 5);
}

const AppointmentCard: React.FC<AppointmentCardProps> = ({ appointment, onPress }) => {
  const categoryColor =
    CATEGORY_COLORS[appointment.service.category] || colors.textSecondary;
  const statusBadge = STATUS_BADGES[appointment.status] || STATUS_BADGES.Created;
  const isCancelled = appointment.status === 'Cancelled';

  return (
    <Pressable
      style={({ pressed }) => [styles.card, isCancelled && styles.cardCancelled, pressed && { opacity: 0.7 }]}
      onPress={() => onPress(appointment)}
      accessibilityRole="button"
      accessibilityLabel={`${appointment.client.name}, ${appointment.service.name}, ${appointment.startTime}`}
    >
      <View style={styles.timeColumn}>
        <Text style={styles.timeText}>{formatTime(appointment.startTime)}</Text>
        <View style={styles.durationChip}>
          <Text style={styles.durationText}>
            {formatTime(appointment.startTime)}-{formatTime(appointment.endTime)}
          </Text>
        </View>
      </View>

      <View style={[styles.verticalDivider, isCancelled && styles.dividerCancelled]} />

      <View style={styles.content}>
        <View style={styles.headerRow}>
          <Text
            style={[styles.clientName, isCancelled && styles.textCancelled]}
            numberOfLines={1}
          >
            {appointment.client.name}
          </Text>
          {statusBadge && (
            <View style={[styles.badge, { backgroundColor: statusBadge.bg }]}>
              <Text style={[styles.badgeText, { color: statusBadge.fg }]}>
                {statusBadge.label}
              </Text>
            </View>
          )}
        </View>

        <View style={styles.serviceRow}>
          <View style={[styles.categoryDot, { backgroundColor: categoryColor }]} />
          <Text
            style={[styles.serviceName, isCancelled && styles.textCancelled]}
            numberOfLines={1}
          >
            {appointment.service.name}
          </Text>
        </View>

        <View style={styles.footerRow}>
          <Text style={styles.priceText}>
            {appointment.price.amount.toFixed(0)} {appointment.price.currency}
          </Text>
          {appointment.location ? (
            <Text style={styles.locationText} numberOfLines={1}>
              {appointment.location}
            </Text>
          ) : null}
        </View>
      </View>
    </Pressable>
  );
};

const styles = StyleSheet.create({
  card: {
    flexDirection: 'row',
    backgroundColor: colors.surface,
    borderRadius: borderRadius.md,
    padding: spacing.md,
    marginHorizontal: spacing.lg,
    marginVertical: spacing.xs,
    borderLeftWidth: 3,
    borderLeftColor: colors.primary,
  },
  cardCancelled: {
    opacity: 0.55,
    borderLeftColor: colors.textTertiary,
  },
  timeColumn: {
    alignItems: 'center',
    justifyContent: 'flex-start',
    width: 52,
    paddingTop: 2,
  },
  timeText: {
    ...typography.caption,
    fontWeight: '700',
    color: colors.textPrimary,
  },
  durationChip: {
    backgroundColor: colors.primaryLight,
    borderRadius: borderRadius.sm,
    paddingHorizontal: 4,
    paddingVertical: 1,
    marginTop: 4,
  },
  durationText: {
    fontSize: 10,
    color: colors.textSecondary,
  },
  verticalDivider: {
    width: 1,
    backgroundColor: colors.separator,
    marginHorizontal: spacing.md,
  },
  dividerCancelled: {
    backgroundColor: colors.border,
  },
  content: {
    flex: 1,
  },
  headerRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 4,
  },
  clientName: {
    ...typography.body,
    fontWeight: '600',
    color: colors.textPrimary,
    flex: 1,
    marginRight: spacing.sm,
  },
  badge: {
    borderRadius: borderRadius.sm,
    paddingHorizontal: 6,
    paddingVertical: 2,
  },
  badgeText: {
    fontSize: 11,
    fontWeight: '600',
  },
  serviceRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 4,
  },
  categoryDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
    marginRight: 6,
  },
  serviceName: {
    ...typography.bodySmall,
    color: colors.textDescription,
    flex: 1,
  },
  footerRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  priceText: {
    ...typography.caption,
    fontWeight: '600',
    color: colors.textPrimary,
  },
  locationText: {
    ...typography.caption,
    color: colors.textTertiary,
    flex: 1,
    textAlign: 'right',
    marginLeft: spacing.sm,
  },
  textCancelled: {
    textDecorationLine: 'line-through',
  },
});

export default AppointmentCard;
