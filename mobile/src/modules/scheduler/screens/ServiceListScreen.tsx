import React, { useEffect, useState, useMemo, useCallback } from 'react';
import {
  View,
  Text,
  Pressable,
  SectionList,
  Alert,
  StyleSheet,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import * as serviceCatalogService from '../services/serviceCatalogService';
import { useBoard } from '../hooks/useBoard';
import { useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';
import type { ServiceItem } from '../types/api';
import type { SchedulerStackParamList } from '../types/navigation';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

type NavigationProp = NativeStackNavigationProp<SchedulerStackParamList>;

const CATEGORY_ORDER: Record<string, number> = {
  MensHaircut: 0,
  WomensHaircut: 1,
  WomensStyling: 2,
  Coloring: 3,
  Treatment: 4,
  Other: 5,
};

interface Section {
  title: string;
  data: ServiceItem[];
}

const ServiceRow: React.FC<{
  item: ServiceItem;
  onNavigate: (id: number, boardId: number) => void;
  onDelete: (id: number, name: string) => void;
}> = ({ item, onNavigate, onDelete }) => {
  const styles = useThemedStyles(makeStyles);
  return (
    <Pressable
      style={({ pressed }) => [styles.serviceItem, pressed && { opacity: 0.7 }]}
      onPress={() => onNavigate(item.id, item.boardId)}
      onLongPress={() => onDelete(item.id, item.name)}
    >
      <View style={styles.serviceInfo}>
        <Text style={styles.serviceName}>{item.name}</Text>
        <Text style={styles.serviceDetail}>
          {item.baseDuration} min | {item.basePrice.amount}{' '}
          {item.basePrice.currency}
        </Text>
      </View>
      <Text style={styles.chevron}>{'>'}</Text>
    </Pressable>
  );
};

const ServiceListScreen: React.FC = () => {
  const navigation = useNavigation<NavigationProp>();
  const { selectedBoardId } = useBoard();
  const styles = useThemedStyles(makeStyles);
  const [services, setServices] = useState<ServiceItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    serviceCatalogService
      .listServices(selectedBoardId ?? undefined)
      .then(setServices)
      .finally(() => setIsLoading(false));
  }, [selectedBoardId]);

  const sections = useMemo(() => {
    const map = new Map<string, ServiceItem[]>();
    services.forEach(svc => {
      const cat = svc.category;
      if (!map.has(cat)) map.set(cat, []);
      map.get(cat)!.push(svc);
    });
    return Array.from(map.entries())
      .sort(([a], [b]) => (CATEGORY_ORDER[a] ?? 99) - (CATEGORY_ORDER[b] ?? 99))
      .map(([title, data]) => ({ title, data }));
  }, [services]);

  const handleDelete = useCallback((id: number, name: string) => {
    Alert.alert('Delete Service', `Delete "${name}"?`, [
      { text: 'Cancel', style: 'cancel' },
      {
        text: 'Delete',
        style: 'destructive',
        onPress: async () => {
          try {
            await serviceCatalogService.deleteService(id);
            setServices(prev => prev.filter(s => s.id !== id));
          } catch {
            Alert.alert('Error', 'Failed to delete service');
          }
        },
      },
    ]);
  }, []);

  const handleServiceNavigate = useCallback(
    (id: number, boardId: number) =>
      navigation.navigate('ServiceEdit', { serviceId: id, boardId }),
    [navigation],
  );

  const renderServiceItem = useCallback(
    ({ item }: { item: ServiceItem }) => (
      <ServiceRow
        item={item}
        onNavigate={handleServiceNavigate}
        onDelete={handleDelete}
      />
    ),
    [handleServiceNavigate, handleDelete],
  );

  return (
    <View style={styles.container}>
      {isLoading ? (
        <View style={styles.center}>
          <Text style={styles.loadingText}>Loading…</Text>
        </View>
      ) : sections.length > 0 ? (
        <SectionList
          sections={sections}
          keyExtractor={item => String(item.id)}
          contentContainerStyle={styles.listContent}
          renderItem={renderServiceItem}
          renderSectionHeader={({ section: { title } }) => (
            <Text style={styles.sectionHeader}>{title}</Text>
          )}
        />
      ) : (
        <View style={styles.center}>
          <Text style={styles.emptyText}>No services yet</Text>
        </View>
      )}

      <Pressable
        style={({ pressed }) => [styles.fab, pressed && { opacity: 0.7 }]}
        onPress={() =>
          navigation.navigate('ServiceEdit', {
            serviceId: undefined,
            boardId: selectedBoardId ?? 0,
          })
        }
        accessibilityLabel="Create service"
        accessibilityRole="button"
      >
        <Text style={styles.fabText}>+</Text>
      </Pressable>
    </View>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      flex: 1,
      backgroundColor: t.colors.background,
    },
    listContent: {
      paddingBottom: 80,
    },
    serviceItem: {
      flexDirection: 'row',
      alignItems: 'center',
      backgroundColor: t.colors.surface,
      paddingHorizontal: spacing.lg,
      paddingVertical: 14,
      borderBottomWidth: StyleSheet.hairlineWidth,
      borderBottomColor: t.colors.separator,
    },
    serviceInfo: {
      flex: 1,
    },
    serviceName: {
      ...t.typography.body,
      color: t.colors.textPrimary,
      fontWeight: '600',
    },
    serviceDetail: {
      ...t.typography.caption,
      color: t.colors.textTertiary,
      marginTop: 2,
    },
    chevron: {
      fontSize: 18,
      color: t.colors.textTertiary,
    },
    sectionHeader: {
      ...t.typography.label,
      color: t.colors.textSecondary,
      backgroundColor: t.colors.background,
      paddingHorizontal: spacing.lg,
      paddingVertical: spacing.xs,
      fontWeight: '700',
      textTransform: 'uppercase',
    },
    center: {
      flex: 1,
      alignItems: 'center',
      justifyContent: 'center',
    },
    loadingText: {
      ...t.typography.body,
      color: t.colors.textSecondary,
    },
    emptyText: {
      ...t.typography.body,
      color: t.colors.textTertiary,
    },
    fab: {
      position: 'absolute',
      right: spacing.xl,
      bottom: spacing.xl,
      width: 56,
      height: 56,
      borderRadius: 28,
      backgroundColor: t.colors.primary,
      alignItems: 'center',
      justifyContent: 'center',
      boxShadow: '0 2px 4px rgba(43,33,24,0.25)',
    },
    fabText: {
      fontSize: 28,
      color: t.colors.textInverse,
      lineHeight: 30,
    },
  });

export default ServiceListScreen;
