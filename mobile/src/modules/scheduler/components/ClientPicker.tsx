import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  TextInput,
  Pressable,
  FlatList,
  StyleSheet,
} from 'react-native';
import { colors, typography, spacing, borderRadius } from '../../../styles/theme';
import type { Client } from '../types/api';

const ClientPickerRow: React.FC<{
  item: Client;
  selectedClientId?: number;
  onSelect: (client: Client) => void;
  onClose: () => void;
  onClearQuery: () => void;
}> = ({ item, selectedClientId, onSelect, onClose, onClearQuery }) => (
  <Pressable
    style={({ pressed }) => [
      styles.clientItem,
      item.id === selectedClientId && styles.clientItemSelected,
      pressed && { opacity: 0.7 },
    ]}
    onPress={() => {
      onSelect(item);
      onClearQuery();
      onClose();
    }}
  >
    <Text style={styles.clientItemText}>{item.name}</Text>
    {item.phone ? (
      <Text style={styles.clientItemPhone}>{item.phone}</Text>
    ) : null}
  </Pressable>
);

interface ClientPickerProps {
  clients: Client[];
  selectedClientId?: number;
  onSelect: (client: Client) => void;
  onCreateNew: (name: string) => Promise<number | undefined>;
  isLoading?: boolean;
}

const ClientPicker: React.FC<ClientPickerProps> = ({
  clients,
  selectedClientId,
  onSelect,
  onCreateNew,
  isLoading,
}) => {
  const [query, setQuery] = useState('');
  const [isOpen, setIsOpen] = useState(false);
  const [isCreating, setIsCreating] = useState(false);

  const selectedClient = clients.find((c) => c.id === selectedClientId);

  const filteredClients = query.trim()
    ? clients.filter((c) =>
        c.name.toLowerCase().includes(query.toLowerCase()),
      )
    : clients;

  const handleCreateNew = useCallback(async () => {
    const name = query.trim();
    if (!name) return;

    setIsCreating(true);
    try {
      const newId = await onCreateNew(name);
      if (newId) {
        onSelect({ id: newId, boardId: 1, name, phone: undefined, notes: undefined });
        setQuery('');
        setIsOpen(false);
      }
    } finally {
      setIsCreating(false);
    }
  }, [query, onCreateNew, onSelect]);

  const clearQuery = useCallback(() => setQuery(''), []);
  const closeDropdown = useCallback(() => setIsOpen(false), []);

  const renderClientItem = useCallback(
    ({ item }: { item: Client }) => (
      <ClientPickerRow
        item={item}
        selectedClientId={selectedClientId}
        onSelect={onSelect}
        onClearQuery={clearQuery}
        onClose={closeDropdown}
      />
    ),
    [selectedClientId, onSelect, clearQuery, closeDropdown],
  );

  return (
    <View style={styles.container}>
      <Text style={styles.label}>Client</Text>
      <Pressable
        style={({ pressed }) => [styles.selector, pressed && { opacity: 0.7 }]}
        onPress={() => setIsOpen(!isOpen)}
        accessibilityRole="button"
        accessibilityLabel="Select client"
      >
        <Text style={[styles.selectorText, !selectedClient && styles.placeholder]}>
          {selectedClient ? selectedClient.name : 'Select a client…'}
        </Text>
        <Text style={styles.chevron}>{isOpen ? '▲' : '▼'}</Text>
      </Pressable>

      {isOpen && (
        <View style={styles.dropdown}>
          <TextInput
            style={styles.searchInput}
            placeholder="Search clients…"
            placeholderTextColor={colors.textTertiary}
            value={query}
            onChangeText={setQuery}
            autoFocus
          />

          {isLoading ? (
            <View style={styles.centerState}>
              <Text style={styles.stateText}>Loading…</Text>
            </View>
          ) : filteredClients.length > 0 ? (
            <FlatList
              data={filteredClients}
              keyExtractor={(item) => String(item.id)}
              style={styles.list}
              renderItem={renderClientItem}
            />
          ) : (
            <View style={styles.centerState}>
              <Text style={styles.stateText}>No clients found</Text>
              <Pressable
                style={({ pressed }) => [
                  styles.createButton,
                  isCreating && styles.createButtonDisabled,
                  pressed && { opacity: 0.7 },
                ]}
                onPress={handleCreateNew}
                disabled={isCreating || !query.trim()}
              >
                <Text style={styles.createButtonText}>
                  {isCreating ? 'Creating…' : `Create "${query.trim()}"`}
                </Text>
              </Pressable>
            </View>
          )}
        </View>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    marginBottom: spacing.md,
    zIndex: 10,
  },
  label: {
    ...typography.label,
    color: colors.textSecondary,
    marginBottom: spacing.xs,
  },
  selector: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    backgroundColor: colors.background,
    borderRadius: borderRadius.md,
    borderWidth: 1,
    borderColor: colors.border,
    paddingHorizontal: spacing.md,
    paddingVertical: 12,
  },
  selectorText: {
    ...typography.body,
    color: colors.textPrimary,
    flex: 1,
  },
  placeholder: {
    color: colors.textTertiary,
  },
  chevron: {
    fontSize: 10,
    color: colors.textSecondary,
    marginLeft: spacing.sm,
  },
  dropdown: {
    backgroundColor: colors.surface,
    borderRadius: borderRadius.md,
    borderWidth: 1,
    borderColor: colors.border,
    marginTop: spacing.xs,
    maxHeight: 280,
    overflow: 'hidden',
  },
  searchInput: {
    ...typography.body,
    color: colors.textPrimary,
    paddingHorizontal: spacing.md,
    paddingVertical: 10,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.separator,
  },
  list: {
    maxHeight: 200,
  },
  clientItem: {
    paddingHorizontal: spacing.md,
    paddingVertical: 12,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.separator,
  },
  clientItemSelected: {
    backgroundColor: colors.primaryLight,
  },
  clientItemText: {
    ...typography.body,
    color: colors.textPrimary,
  },
  clientItemPhone: {
    ...typography.caption,
    color: colors.textTertiary,
    marginTop: 2,
  },
  centerState: {
    paddingVertical: spacing.lg,
    alignItems: 'center',
  },
  stateText: {
    ...typography.caption,
    color: colors.textTertiary,
    marginBottom: spacing.sm,
  },
  createButton: {
    backgroundColor: colors.primary,
    borderRadius: borderRadius.md,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.sm,
    marginTop: spacing.sm,
  },
  createButtonDisabled: {
    opacity: 0.5,
  },
  createButtonText: {
    ...typography.bodySmall,
    color: colors.textInverse,
    fontWeight: '600',
  },
});

export default ClientPicker;
