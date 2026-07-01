import React, { useCallback, useState } from 'react';
import {
  View,
  Text,
  TextInput,
  Pressable,
  FlatList,
  Alert,
  StyleSheet,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import useRoles from '../hooks/useRoles';
import { useTheme, useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';
import type { AdminStackParamList } from '../../../navigation/types';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import type { RoleSummary } from '../types/api';

type NavigationProp = NativeStackNavigationProp<AdminStackParamList>;

const MIN_TOUCH = 44;
const RESERVED_NAMES = ['Admin', 'User', 'Guest'];

const RoleRow: React.FC<{
  item: RoleSummary;
  onPress: () => void;
  onDelete: () => void;
  canDelete: boolean;
}> = ({ item, onPress, onDelete, canDelete }) => {
  const styles = useThemedStyles(makeStyles);
  const { t } = useTranslation();

  return (
    <View style={styles.row}>
      <Pressable
        style={({ pressed }) => [
          styles.rowContent,
          pressed && { opacity: 0.7 },
        ]}
        onPress={onPress}
        accessibilityRole="button"
        accessibilityLabel={`${item.name}, ${item.permissions.length} ${t(
          'admin.roleList.permissions',
        )}`}
        testID={`role-row-${item.name}`}
      >
        <View style={styles.rowInfo}>
          <Text style={styles.roleName}>{item.name}</Text>
          <Text style={styles.permCount}>
            {item.permissions.length} {t('admin.roleList.permissions')}
          </Text>
        </View>
      </Pressable>
      {canDelete ? (
        <Pressable
          style={({ pressed }) => [
            styles.deleteBtn,
            pressed && { opacity: 0.7 },
          ]}
          onPress={onDelete}
          accessibilityRole="button"
          accessibilityLabel={`${t('admin.roleList.delete')} ${item.name}`}
          testID={`role-delete-${item.name}`}
        >
          <Text style={styles.deleteBtnText}>{t('admin.roleList.delete')}</Text>
        </Pressable>
      ) : null}
    </View>
  );
};

const RoleListScreen: React.FC = () => {
  const navigation = useNavigation<NavigationProp>();
  const { t } = useTranslation();
  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);
  const { roles, isLoading, error, createRole, deleteRole } = useRoles();

  const [showCreate, setShowCreate] = useState(false);
  const [newRoleName, setNewRoleName] = useState('');

  const handleRolePress = useCallback(
    (roleName: string) => {
      navigation.navigate('RoleEdit', { roleName });
    },
    [navigation],
  );

  const handleGoToUsers = useCallback(() => {
    navigation.navigate('UserList');
  }, [navigation]);

  const handleDelete = useCallback(
    (role: RoleSummary) => {
      Alert.alert(
        t('admin.roleList.title'),
        t('admin.roleList.deleteConfirm', { name: role.name }),
        [
          { text: t('common.cancel'), style: 'cancel' },
          {
            text: t('admin.roleList.delete'),
            style: 'destructive',
            onPress: async () => {
              try {
                await deleteRole(role.name);
              } catch {
                // error surfaced by hook
              }
            },
          },
        ],
      );
    },
    [t, deleteRole],
  );

  const handleCreate = useCallback(async () => {
    const name = newRoleName.trim();
    if (!name) return;
    try {
      await createRole({ name, permissions: [] });
      setNewRoleName('');
      setShowCreate(false);
    } catch {
      // error surfaced by hook
    }
  }, [newRoleName, createRole]);

  const renderItem = useCallback(
    ({ item }: { item: RoleSummary }) => (
      <RoleRow
        item={item}
        onPress={() => handleRolePress(item.name)}
        onDelete={() => handleDelete(item)}
        canDelete={!RESERVED_NAMES.includes(item.name)}
      />
    ),
    [handleRolePress, handleDelete],
  );

  const keyExtractor = useCallback((item: RoleSummary) => item.name, []);

  return (
    <View style={styles.container}>
      <View style={styles.headerRow}>
        <Pressable
          style={({ pressed }) => [
            styles.toggleBtn,
            pressed && { opacity: 0.7 },
          ]}
          onPress={handleGoToUsers}
          accessibilityRole="button"
          accessibilityLabel={t('admin.roleList.goToUsers')}
          testID="goto-users-button"
        >
          <Text style={styles.toggleBtnText}>
            {t('admin.roleList.goToUsers')}
          </Text>
        </Pressable>
      </View>

      {showCreate ? (
        <View style={styles.createCard}>
          <TextInput
            style={styles.createInput}
            placeholder={t('admin.roleList.createPlaceholder')}
            placeholderTextColor={colors.textTertiary}
            value={newRoleName}
            onChangeText={setNewRoleName}
            autoFocus
            testID="new-role-name-input"
          />
          <View style={styles.createActions}>
            <Pressable
              style={({ pressed }) => [
                styles.createSubmit,
                pressed && { opacity: 0.7 },
              ]}
              onPress={handleCreate}
              disabled={!newRoleName.trim()}
              accessibilityRole="button"
              accessibilityLabel={t('admin.roleList.create')}
              testID="create-role-submit"
            >
              <Text style={styles.createSubmitText}>
                {t('admin.roleList.create')}
              </Text>
            </Pressable>
            <Pressable
              style={({ pressed }) => pressed && { opacity: 0.7 }}
              onPress={() => {
                setShowCreate(false);
                setNewRoleName('');
              }}
              accessibilityRole="button"
              testID="cancel-create-role"
            >
              <Text style={styles.cancelText}>{t('common.cancel')}</Text>
            </Pressable>
          </View>
        </View>
      ) : null}

      {error ? (
        <View style={styles.center}>
          <Text style={styles.errorText}>{error}</Text>
        </View>
      ) : isLoading && roles.length === 0 ? (
        <View style={styles.center}>
          <Text style={styles.loadingText}>{t('common.loading')}</Text>
        </View>
      ) : (
        <FlatList
          data={roles}
          renderItem={renderItem}
          keyExtractor={keyExtractor}
          contentContainerStyle={styles.listContent}
          ListEmptyComponent={
            <View style={styles.center}>
              <Text style={styles.emptyText}>{t('admin.roleList.empty')}</Text>
            </View>
          }
        />
      )}

      {!showCreate ? (
        <Pressable
          style={({ pressed }) => [styles.fab, pressed && { opacity: 0.7 }]}
          onPress={() => setShowCreate(true)}
          accessibilityRole="button"
          accessibilityLabel={t('admin.roleList.createRole')}
          testID="create-role-fab"
        >
          <Text style={styles.fabText}>+</Text>
        </Pressable>
      ) : null}
    </View>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      flex: 1,
      backgroundColor: t.colors.background,
    },
    headerRow: {
      flexDirection: 'row',
      justifyContent: 'flex-end',
      paddingHorizontal: spacing.lg,
      paddingVertical: spacing.sm,
    },
    toggleBtn: {
      paddingHorizontal: spacing.md,
      paddingVertical: 10,
      borderRadius: borderRadius.md,
      borderWidth: 1.5,
      borderColor: t.colors.primary,
      minHeight: MIN_TOUCH,
      alignItems: 'center',
      justifyContent: 'center',
    },
    toggleBtnText: {
      ...t.typography.caption,
      color: t.colors.primary,
      fontWeight: '600',
    },
    createCard: {
      paddingHorizontal: spacing.lg,
      paddingBottom: spacing.md,
    },
    createInput: {
      backgroundColor: t.colors.surface,
      borderRadius: borderRadius.md,
      borderWidth: 1,
      borderColor: t.colors.border,
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
      minHeight: MIN_TOUCH,
      alignItems: 'center',
      justifyContent: 'center',
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
    listContent: {
      paddingBottom: 80,
    },
    row: {
      flexDirection: 'row',
      alignItems: 'center',
      backgroundColor: t.colors.surface,
      paddingRight: spacing.lg,
      borderBottomWidth: StyleSheet.hairlineWidth,
      borderBottomColor: t.colors.separator,
    },
    rowContent: {
      flex: 1,
      paddingLeft: spacing.lg,
      paddingVertical: 14,
      minHeight: MIN_TOUCH,
      justifyContent: 'center',
    },
    rowInfo: {
      flex: 1,
    },
    roleName: {
      ...t.typography.body,
      color: t.colors.textPrimary,
      fontWeight: '600',
    },
    permCount: {
      ...t.typography.caption,
      color: t.colors.textTertiary,
      marginTop: 2,
    },
    deleteBtn: {
      minHeight: MIN_TOUCH,
      minWidth: MIN_TOUCH,
      paddingHorizontal: spacing.md,
      alignItems: 'center',
      justifyContent: 'center',
    },
    deleteBtnText: {
      ...t.typography.caption,
      color: t.colors.error,
      fontWeight: '600',
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
    errorText: {
      ...t.typography.body,
      color: t.colors.error,
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

export default RoleListScreen;
