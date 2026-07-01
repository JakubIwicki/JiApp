import React, { useCallback, useState } from 'react';
import {
  View,
  Text,
  TextInput,
  Pressable,
  FlatList,
  StyleSheet,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import useUsers from '../hooks/useUsers';
import useRoles from '../hooks/useRoles';
import { useTheme, useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';
import type { AdminStackParamList } from '../../../navigation/types';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import type { UserSummary } from '../types/api';

type NavigationProp = NativeStackNavigationProp<AdminStackParamList>;

const UserRow: React.FC<{
  item: UserSummary;
  onPress: () => void;
}> = ({ item, onPress }) => {
  const styles = useThemedStyles(makeStyles);
  const { t } = useTranslation();
  const roleList = item.roles.join(', ');
  return (
    <Pressable
      style={({ pressed }) => [styles.row, pressed && { opacity: 0.7 }]}
      onPress={onPress}
      accessibilityRole="button"
      accessibilityLabel={`${item.username}, ${roleList}`}
      testID={`user-row-${item.id}`}
    >
      <View style={styles.rowInfo}>
        <Text style={styles.username}>{item.username}</Text>
        <Text style={styles.email}>{item.email}</Text>
        <Text style={styles.roles} numberOfLines={1}>
          {roleList || t('admin.userList.noRoles')}
        </Text>
      </View>
      {item.isLockedOut && (
        <View style={styles.lockedBadge}>
          <Text style={styles.lockedText}>{t('admin.userList.locked')}</Text>
        </View>
      )}
    </Pressable>
  );
};

const UserListScreen: React.FC = () => {
  const navigation = useNavigation<NavigationProp>();
  const { t } = useTranslation();
  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);
  const { users, isLoading, error, search } = useUsers();
  const { roles } = useRoles();
  const [query, setQuery] = useState('');

  const handleSearch = useCallback(
    (text: string) => {
      setQuery(text);
      search(text);
    },
    [search],
  );

  const handleUserPress = useCallback(
    (userId: number) => {
      navigation.navigate('UserDetail', { userId });
    },
    [navigation],
  );

  const handleCreateUser = useCallback(() => {
    navigation.navigate('CreateUser');
  }, [navigation]);

  const handleGoToRoles = useCallback(() => {
    navigation.navigate('RoleList');
  }, [navigation]);

  const renderItem = useCallback(
    ({ item }: { item: UserSummary }) => (
      <UserRow item={item} onPress={() => handleUserPress(item.id)} />
    ),
    [handleUserPress],
  );

  const keyExtractor = useCallback((item: UserSummary) => String(item.id), []);

  return (
    <View style={styles.container}>
      <View style={styles.searchRow}>
        <View style={styles.searchContainer}>
          <TextInput
            style={styles.searchInput}
            placeholder={t('admin.userList.searchPlaceholder')}
            placeholderTextColor={colors.textTertiary}
            value={query}
            onChangeText={handleSearch}
          />
        </View>
        <Pressable
          style={({ pressed }) => [
            styles.toggleBtn,
            pressed && { opacity: 0.7 },
          ]}
          onPress={handleGoToRoles}
          accessibilityRole="button"
          accessibilityLabel={t('admin.userList.goToRoles')}
          testID="goto-roles-button"
        >
          <Text style={styles.toggleBtnText}>
            {t('admin.userList.goToRoles')}
          </Text>
        </Pressable>
      </View>

      {error ? (
        <View style={styles.center}>
          <Text style={styles.errorText}>{error}</Text>
        </View>
      ) : isLoading && users.length === 0 ? (
        <View style={styles.center}>
          <Text style={styles.loadingText}>{t('common.loading')}</Text>
        </View>
      ) : (
        <FlatList
          data={users}
          renderItem={renderItem}
          keyExtractor={keyExtractor}
          contentContainerStyle={styles.listContent}
          ListEmptyComponent={
            <View style={styles.center}>
              <Text style={styles.emptyText}>{t('admin.userList.empty')}</Text>
            </View>
          }
        />
      )}

      <Pressable
        style={({ pressed }) => [styles.fab, pressed && { opacity: 0.7 }]}
        onPress={handleCreateUser}
        accessibilityRole="button"
        accessibilityLabel={t('admin.userList.createUser')}
        testID="create-user-fab"
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
    searchRow: {
      flexDirection: 'row',
      alignItems: 'center',
      paddingHorizontal: spacing.lg,
      paddingVertical: spacing.sm,
      gap: spacing.sm,
    },
    searchContainer: {
      flex: 1,
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
    toggleBtn: {
      paddingHorizontal: spacing.md,
      paddingVertical: 10,
      borderRadius: borderRadius.md,
      borderWidth: 1.5,
      borderColor: t.colors.primary,
      minHeight: 44,
      alignItems: 'center',
      justifyContent: 'center',
    },
    toggleBtnText: {
      ...t.typography.caption,
      color: t.colors.primary,
      fontWeight: '600',
    },
    listContent: {
      paddingBottom: 80,
    },
    row: {
      flexDirection: 'row',
      alignItems: 'center',
      backgroundColor: t.colors.surface,
      paddingHorizontal: spacing.lg,
      paddingVertical: 14,
      borderBottomWidth: StyleSheet.hairlineWidth,
      borderBottomColor: t.colors.separator,
      minHeight: 44,
    },
    rowInfo: {
      flex: 1,
    },
    username: {
      ...t.typography.body,
      color: t.colors.textPrimary,
      fontWeight: '600',
    },
    email: {
      ...t.typography.caption,
      color: t.colors.textTertiary,
      marginTop: 2,
    },
    roles: {
      ...t.typography.caption,
      color: t.colors.textSecondary,
      marginTop: 2,
    },
    lockedBadge: {
      backgroundColor: t.colors.errorLight,
      borderRadius: borderRadius.sm,
      paddingHorizontal: spacing.sm,
      paddingVertical: 2,
    },
    lockedText: {
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

export default UserListScreen;
