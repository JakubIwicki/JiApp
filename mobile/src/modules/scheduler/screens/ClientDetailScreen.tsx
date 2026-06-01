import React, { useEffect, useState, useCallback } from 'react';
import { View, Text, FlatList, StyleSheet } from 'react-native';
import { useRoute, RouteProp } from '@react-navigation/native';
import * as clientService from '../services/clientService';
import type { ClientWithAppointments } from '../services/clientService';
import { colors, typography, spacing, borderRadius } from '../../../styles/theme';
import type { SchedulerStackParamList } from '../types/navigation';

type DetailRoute = RouteProp<SchedulerStackParamList, 'ClientDetail'>;

const STATUS_BADGES: Record<string, { bg: string; fg: string }> = {
  Created: { bg: colors.primaryLight, fg: colors.primary },
  Done: { bg: colors.successLight, fg: colors.success },
  Cancelled: { bg: colors.errorLight, fg: colors.error },
};

const AppointmentRow: React.FC<{
  item: {
    id: number;
    date: string;
    startTime: string;
    endTime: string;
    serviceName: string;
    status: string;
  };
}> = ({ item }) => {
  const badge = STATUS_BADGES[item.status] || STATUS_BADGES.Created;
  return (
    <View style={styles.historyItem}>
      <View style={styles.historyLeft}>
        <Text style={styles.historyDate}>{item.date}</Text>
        <Text style={styles.historyTime}>
          {item.startTime} - {item.endTime}
        </Text>
      </View>
      <View style={styles.historyCenter}>
        <Text style={styles.historyService}>{item.serviceName}</Text>
      </View>
      <View style={[styles.statusBadge, { backgroundColor: badge.bg }]}>
        <Text style={[styles.statusText, { color: badge.fg }]}>
          {item.status}
        </Text>
      </View>
    </View>
  );
};

const ClientDetailScreen: React.FC = () => {
  const route = useRoute<DetailRoute>();
  const { clientId } = route.params;

  const [data, setData] = useState<ClientWithAppointments | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const renderAppointmentItem = useCallback(
    ({ item }: { item: ClientWithAppointments['appointments'][number] }) => (
      <AppointmentRow item={item} />
    ),
    [],
  );

  useEffect(() => {
    clientService
      .getClient(clientId)
      .then(setData)
      .finally(() => setIsLoading(false));
  }, [clientId]);

  if (isLoading) {
    return (
      <View style={styles.center}>
        <Text style={styles.loadingText}>Loading…</Text>
      </View>
    );
  }

  if (!data) {
    return (
      <View style={styles.center}>
        <Text style={styles.errorText}>Client not found</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      {/* Client info header */}
      <View style={styles.header}>
        <Text style={styles.name}>{data.name}</Text>
        {data.phone ? <Text style={styles.phone}>{data.phone}</Text> : null}
        {data.notes ? <Text style={styles.notes}>{data.notes}</Text> : null}
      </View>

      <Text style={styles.sectionTitle}>
        Appointment History ({data.appointments.length})
      </Text>

      <FlatList
        data={data.appointments}
        keyExtractor={(item) => String(item.id)}
        contentContainerStyle={styles.list}
        renderItem={renderAppointmentItem}
        ListEmptyComponent={
          <View style={styles.center}>
            <Text style={styles.emptyText}>No appointments yet</Text>
          </View>
        }
      />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  header: {
    backgroundColor: colors.surface,
    padding: spacing.lg,
    marginBottom: spacing.sm,
  },
  name: {
    ...typography.heading,
    color: colors.textPrimary,
  },
  phone: {
    ...typography.body,
    color: colors.textSecondary,
    marginTop: 4,
  },
  notes: {
    ...typography.bodySmall,
    color: colors.textTertiary,
    marginTop: spacing.sm,
    fontStyle: 'italic',
  },
  sectionTitle: {
    ...typography.label,
    color: colors.textSecondary,
    textTransform: 'uppercase',
    letterSpacing: 0.5,
    paddingHorizontal: spacing.lg,
    marginBottom: spacing.sm,
  },
  list: {
    paddingBottom: spacing.xxl,
  },
  historyItem: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: colors.surface,
    paddingHorizontal: spacing.lg,
    paddingVertical: 12,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.separator,
  },
  historyLeft: {
    width: 90,
  },
  historyDate: {
    ...typography.caption,
    fontWeight: '600',
    color: colors.textPrimary,
  },
  historyTime: {
    ...typography.caption,
    color: colors.textTertiary,
    fontSize: 11,
  },
  historyCenter: {
    flex: 1,
    paddingHorizontal: spacing.sm,
  },
  historyService: {
    ...typography.bodySmall,
    color: colors.textPrimary,
  },
  statusBadge: {
    borderRadius: borderRadius.sm,
    paddingHorizontal: 6,
    paddingVertical: 2,
  },
  statusText: {
    fontSize: 11,
    fontWeight: '600',
  },
  center: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: spacing.xxl,
  },
  loadingText: {
    ...typography.body,
    color: colors.textSecondary,
  },
  errorText: {
    ...typography.body,
    color: colors.error,
  },
  emptyText: {
    ...typography.body,
    color: colors.textTertiary,
  },
});

export default ClientDetailScreen;
