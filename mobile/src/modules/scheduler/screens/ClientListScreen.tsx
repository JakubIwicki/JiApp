import React, { useEffect, useState, useCallback, useMemo } from 'react';
import {
  View,
  Text,
  TextInput,
  Pressable,
  SectionList,
  Alert,
  StyleSheet,
} from 'react-native';
import { useNavigation, useRoute, RouteProp } from '@react-navigation/native';
import useClients from '../hooks/useClients';
import { useTheme, useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';
import type { SchedulerStackParamList } from '../types/navigation';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import type { Client } from '../types/api';

type NavigationProp = NativeStackNavigationProp<SchedulerStackParamList>;
type ClientListRoute = RouteProp<SchedulerStackParamList, 'ClientList'>;

interface Section {
  title: string;
  data: Client[];
}

const ClientRow: React.FC<{ item: Client; onPress: () => void }> = ({
  item,
  onPress,
}) => {
  const styles = useThemedStyles(makeStyles);
  return (
    <Pressable
      style={({ pressed }) => [styles.clientItem, pressed && { opacity: 0.7 }]}
      onPress={onPress}
    >
      <Text style={styles.clientName}>{item.name}</Text>
      {item.phone ? <Text style={styles.clientPhone}>{item.phone}</Text> : null}
    </Pressable>
  );
};

const ClientListScreen: React.FC = () => {
  const navigation = useNavigation<NavigationProp>();
  const route = useRoute<ClientListRoute>();
  const { boardId } = route.params;
  const clients = useClients(boardId);
  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);
  const [query, setQuery] = useState('');
  const [showCreateInput, setShowCreateInput] = useState(false);
  const [newName, setNewName] = useState('');

  useEffect(() => {
    clients.loadAll();
  }, [clients.loadAll]);

  useEffect(() => {
    if (query.trim()) {
      clients.searchClients(query);
    } else {
      clients.loadAll();
    }
  }, [query, clients.searchClients, clients.loadAll]);

  const sections = useMemo(() => {
    const map = new Map<string, Client[]>();
    const data = query.trim() ? clients.clients : clients.clients;
    data.forEach(client => {
      const letter = client.name.charAt(0).toUpperCase();
      if (!map.has(letter)) map.set(letter, []);
      map.get(letter)!.push(client);
    });
    return Array.from(map.entries())
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([title, data]) => ({ title, data }));
  }, [clients.clients, query]);

  const handleClientNavigate = useCallback(
    (id: number) => navigation.navigate('ClientDetail', { clientId: id }),
    [navigation],
  );

  const renderClientItem = useCallback(
    ({ item }: { item: Client }) => (
      <ClientRow item={item} onPress={() => handleClientNavigate(item.id)} />
    ),
    [handleClientNavigate],
  );

  const handleCreate = useCallback(async () => {
    const name = newName.trim();
    if (!name) return;
    try {
      await clients.addClient({ name });
      setNewName('');
      setShowCreateInput(false);
    } catch (err) {
      Alert.alert(
        'Error',
        err instanceof Error ? err.message : 'Failed to create client',
      );
    }
  }, [newName, clients]);

  return (
    <View style={styles.container}>
      {/* Search bar */}
      <View style={styles.searchContainer}>
        <TextInput
          style={styles.searchInput}
          placeholder="Search clients…"
          placeholderTextColor={colors.textTertiary}
          value={query}
          onChangeText={setQuery}
        />
      </View>

      {clients.isLoading ? (
        <View style={styles.center}>
          <Text style={styles.loadingText}>Loading…</Text>
        </View>
      ) : sections.length > 0 ? (
        <SectionList
          sections={sections}
          keyExtractor={item => String(item.id)}
          contentContainerStyle={styles.listContent}
          renderItem={renderClientItem}
          renderSectionHeader={({ section: { title } }) => (
            <Text style={styles.sectionHeader}>{title}</Text>
          )}
        />
      ) : (
        <View style={styles.center}>
          <Text style={styles.emptyText}>No clients found</Text>
        </View>
      )}

      {/* Create button */}
      {showCreateInput ? (
        <View style={styles.createContainer}>
          <TextInput
            style={styles.createInput}
            placeholder="Client name"
            placeholderTextColor={colors.textTertiary}
            value={newName}
            onChangeText={setNewName}
            autoFocus
          />
          <View style={styles.createActions}>
            <Pressable
              style={({ pressed }) => [
                styles.createSubmit,
                pressed && { opacity: 0.7 },
              ]}
              onPress={handleCreate}
              disabled={!newName.trim()}
            >
              <Text style={styles.createSubmitText}>Add</Text>
            </Pressable>
            <Pressable
              style={({ pressed }) => pressed && { opacity: 0.7 }}
              onPress={() => {
                setShowCreateInput(false);
                setNewName('');
              }}
            >
              <Text style={styles.cancelText}>Cancel</Text>
            </Pressable>
          </View>
        </View>
      ) : (
        <Pressable
          style={({ pressed }) => [styles.fab, pressed && { opacity: 0.7 }]}
          onPress={() => setShowCreateInput(true)}
          accessibilityLabel="Create client"
          accessibilityRole="button"
        >
          <Text style={styles.fabText}>+</Text>
        </Pressable>
      )}
    </View>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      flex: 1,
      backgroundColor: t.colors.background,
    },
    searchContainer: {
      paddingHorizontal: spacing.lg,
      paddingVertical: spacing.sm,
    },
    searchInput: {
      backgroundColor: t.colors.surface,
      borderRadius: borderRadius.md,
      borderWidth: 1,
      borderColor: t.colors.border,
      paddingHorizontal: spacing.md,
      paddingVertical: 10,
      ...t.typography.body,
      color: t.colors.textPrimary,
    },
    listContent: {
      paddingBottom: 80,
    },
    clientItem: {
      backgroundColor: t.colors.surface,
      paddingHorizontal: spacing.lg,
      paddingVertical: 14,
      borderBottomWidth: StyleSheet.hairlineWidth,
      borderBottomColor: t.colors.separator,
    },
    clientName: {
      ...t.typography.body,
      color: t.colors.textPrimary,
      fontWeight: '600',
    },
    clientPhone: {
      ...t.typography.caption,
      color: t.colors.textTertiary,
      marginTop: 2,
    },
    sectionHeader: {
      ...t.typography.label,
      color: t.colors.textSecondary,
      backgroundColor: t.colors.background,
      paddingHorizontal: spacing.lg,
      paddingVertical: spacing.xs,
      fontWeight: '700',
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
    createContainer: {
      padding: spacing.lg,
      backgroundColor: t.colors.surface,
      borderTopWidth: StyleSheet.hairlineWidth,
      borderTopColor: t.colors.border,
    },
    createInput: {
      backgroundColor: t.colors.background,
      borderRadius: borderRadius.md,
      paddingHorizontal: spacing.md,
      paddingVertical: 10,
      ...t.typography.body,
      color: t.colors.textPrimary,
    },
    createActions: {
      flexDirection: 'row',
      alignItems: 'center',
      marginTop: spacing.sm,
      gap: spacing.md,
    },
    createSubmit: {
      backgroundColor: t.colors.primary,
      borderRadius: borderRadius.md,
      paddingHorizontal: spacing.xl,
      paddingVertical: spacing.sm,
    },
    createSubmitText: {
      ...t.typography.bodySmall,
      color: t.colors.textInverse,
      fontWeight: '600',
    },
    cancelText: {
      ...t.typography.bodySmall,
      color: t.colors.textSecondary,
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

export default ClientListScreen;
