import React, { useEffect, useState, useCallback } from 'react';
import {
  View,
  Text,
  Pressable,
  FlatList,
  StyleSheet,
} from 'react-native';
import { useRoute, RouteProp } from '@react-navigation/native';
import useReports from '../hooks/useReports';
import { colors, typography, spacing, borderRadius } from '../../../styles/theme';
import type { SchedulerStackParamList } from '../types/navigation';
import type { RevenueReport, ClientReportItem } from '../types/api';

type ReportsRoute = RouteProp<SchedulerStackParamList, 'Reports'>;

type Tab = 'revenue' | 'clients';

const RevenueRow: React.FC<{ item: RevenueReport }> = ({ item }) => (
  <View style={styles.reportRow}>
    <View style={styles.reportLeft}>
      <Text style={styles.reportKey}>{item.groupKey}</Text>
      <Text style={styles.reportCount}>
        {item.appointmentCount} visits
      </Text>
    </View>
    <View style={styles.reportRight}>
      <Text style={styles.reportRevenue}>
        {item.revenue.toFixed(0)} PLN
      </Text>
      <Text style={styles.reportExpenses}>
        -{item.expenses.toFixed(0)} PLN
      </Text>
      <Text
        style={[
          styles.reportNet,
          item.net >= 0 ? styles.positive : styles.negative,
        ]}
      >
        {item.net.toFixed(0)} PLN
      </Text>
    </View>
  </View>
);

const ClientReportRow: React.FC<{ item: ClientReportItem }> = ({ item }) => (
  <View style={styles.reportRow}>
    <View style={styles.reportLeft}>
      <Text style={styles.reportKey}>{item.client.name}</Text>
      <Text style={styles.reportCount}>
        {item.visitCount} visits | Last:{' '}
        {item.lastVisit || 'N/A'}
      </Text>
    </View>
    <View style={styles.reportRight}>
      <Text style={styles.reportRevenue}>
        {item.totalSpent.toFixed(0)} PLN
      </Text>
      <Text style={styles.reportExpenses}>
        avg {item.averagePerVisit.toFixed(0)} PLN
      </Text>
    </View>
  </View>
);

const GROUP_BY_OPTIONS = ['weekend', 'service', 'location', 'client'] as const;
const SORT_BY_OPTIONS = ['visitCount', 'totalSpent', 'lastVisit'] as const;

const ReportsScreen: React.FC = () => {
  const route = useRoute<ReportsRoute>();
  const { boardId } = route.params;
  const reports = useReports();

  const [activeTab, setActiveTab] = useState<Tab>('revenue');
  const [groupBy, setGroupBy] = useState<string>('weekend');
  const [sortBy, setSortBy] = useState<string>('visitCount');

  useEffect(() => {
    if (activeTab === 'revenue') {
      reports.fetchRevenueReport(boardId, '2026-01-01', '2026-12-31', groupBy);
    } else {
      reports.fetchClientReport(boardId, sortBy);
    }
  }, [activeTab, groupBy, sortBy, boardId, reports.fetchRevenueReport, reports.fetchClientReport]);

  const renderRevenueItem = useCallback(
    ({ item }: { item: RevenueReport }) => <RevenueRow item={item} />,
    [],
  );

  const renderClientReportItem = useCallback(
    ({ item }: { item: ClientReportItem }) => <ClientReportRow item={item} />,
    [],
  );

  return (
    <View style={styles.container}>
      {/* Tabs */}
      <View style={styles.tabRow}>
        <Pressable
          style={({ pressed }) => [styles.tab, activeTab === 'revenue' && styles.tabActive, pressed && { opacity: 0.7 }]}
          onPress={() => setActiveTab('revenue')}
        >
          <Text
            style={[
              styles.tabText,
              activeTab === 'revenue' && styles.tabTextActive,
            ]}
          >
            Revenue
          </Text>
        </Pressable>
        <Pressable
          style={({ pressed }) => [styles.tab, activeTab === 'clients' && styles.tabActive, pressed && { opacity: 0.7 }]}
          onPress={() => setActiveTab('clients')}
        >
          <Text
            style={[
              styles.tabText,
              activeTab === 'clients' && styles.tabTextActive,
            ]}
          >
            Clients
          </Text>
        </Pressable>
      </View>

      {/* Controls */}
      <View style={styles.controlsRow}>
        {activeTab === 'revenue' ? (
          <View style={styles.pickerRow}>
            <Text style={styles.controlLabel}>Group:</Text>
            {GROUP_BY_OPTIONS.map((opt) => (
              <Pressable
                key={opt}
                style={({ pressed }) => [
                  styles.optionChip,
                  groupBy === opt && styles.optionChipActive,
                  pressed && { opacity: 0.7 },
                ]}
                onPress={() => setGroupBy(opt)}
              >
                <Text
                  style={[
                    styles.optionChipText,
                    groupBy === opt && styles.optionChipTextActive,
                  ]}
                >
                  {opt}
                </Text>
              </Pressable>
            ))}
          </View>
        ) : (
          <View style={styles.pickerRow}>
            <Text style={styles.controlLabel}>Sort:</Text>
            {SORT_BY_OPTIONS.map((opt) => (
              <Pressable
                key={opt}
                style={({ pressed }) => [
                  styles.optionChip,
                  sortBy === opt && styles.optionChipActive,
                  pressed && { opacity: 0.7 },
                ]}
                onPress={() => setSortBy(opt)}
              >
                <Text
                  style={[
                    styles.optionChipText,
                    sortBy === opt && styles.optionChipTextActive,
                  ]}
                >
                  {opt}
                </Text>
              </Pressable>
            ))}
          </View>
        )}
      </View>

      {/* Content */}
      {reports.isLoading ? (
        <View style={styles.center}>
          <Text style={styles.loadingText}>Loading…</Text>
        </View>
      ) : activeTab === 'revenue' ? (
        <FlatList
          data={reports.revenueReports}
          keyExtractor={(_, i) => String(i)}
          contentContainerStyle={styles.list}
          renderItem={renderRevenueItem}
          ListEmptyComponent={
            <View style={styles.center}>
              <Text style={styles.emptyText}>No data</Text>
            </View>
          }
        />
      ) : (
        <FlatList
          data={reports.clientReports}
          keyExtractor={(item) => String(item.client.id)}
          contentContainerStyle={styles.list}
          renderItem={renderClientReportItem}
          ListEmptyComponent={
            <View style={styles.center}>
              <Text style={styles.emptyText}>No data</Text>
            </View>
          }
        />
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  tabRow: {
    flexDirection: 'row',
    backgroundColor: colors.surface,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.border,
  },
  tab: {
    flex: 1,
    alignItems: 'center',
    paddingVertical: spacing.md,
    borderBottomWidth: 2,
    borderBottomColor: 'transparent',
  },
  tabActive: {
    borderBottomColor: colors.primary,
  },
  tabText: {
    ...typography.body,
    color: colors.textSecondary,
    fontWeight: '600',
  },
  tabTextActive: {
    color: colors.primary,
  },
  controlsRow: {
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.sm,
    backgroundColor: colors.surface,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.separator,
  },
  pickerRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    alignItems: 'center',
    gap: spacing.xs,
  },
  controlLabel: {
    ...typography.caption,
    color: colors.textSecondary,
    fontWeight: '600',
    marginRight: spacing.xs,
  },
  optionChip: {
    borderRadius: borderRadius.xl,
    borderWidth: 1,
    borderColor: colors.border,
    paddingHorizontal: spacing.md,
    paddingVertical: 4,
  },
  optionChipActive: {
    backgroundColor: colors.primary,
    borderColor: colors.primary,
  },
  optionChipText: {
    fontSize: 12,
    color: colors.textSecondary,
  },
  optionChipTextActive: {
    color: colors.textInverse,
    fontWeight: '600',
  },
  list: {
    paddingBottom: spacing.xxl,
  },
  reportRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    backgroundColor: colors.surface,
    paddingHorizontal: spacing.lg,
    paddingVertical: 14,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.separator,
  },
  reportLeft: {
    flex: 1,
  },
  reportKey: {
    ...typography.body,
    color: colors.textPrimary,
    fontWeight: '600',
  },
  reportCount: {
    ...typography.caption,
    color: colors.textTertiary,
    marginTop: 2,
  },
  reportRight: {
    alignItems: 'flex-end',
  },
  reportRevenue: {
    ...typography.bodySmall,
    fontWeight: '700',
    color: colors.success,
  },
  reportExpenses: {
    ...typography.caption,
    color: colors.warning,
    marginTop: 2,
  },
  reportNet: {
    ...typography.caption,
    fontWeight: '700',
    marginTop: 2,
  },
  positive: {
    color: colors.success,
  },
  negative: {
    color: colors.error,
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
  emptyText: {
    ...typography.body,
    color: colors.textTertiary,
  },
});

export default ReportsScreen;
