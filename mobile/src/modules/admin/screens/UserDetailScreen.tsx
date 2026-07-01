import React, { useCallback, useState } from 'react';
import {
  View,
  Text,
  Pressable,
  ScrollView,
  Alert,
  TextInput,
  StyleSheet,
} from 'react-native';
import { useNavigation, useRoute, RouteProp } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import useUserDetail from '../hooks/useUserDetail';
import useRoles from '../hooks/useRoles';
import useToast from '../../../hooks/useToast';
import { useTheme, useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';
import type { AdminStackParamList } from '../../../navigation/types';

type DetailRoute = RouteProp<AdminStackParamList, 'UserDetail'>;

const MIN_TOUCH = 44;

const RESERVED_NAMES = ['Admin', 'User', 'Guest'];

const UserDetailScreen: React.FC = () => {
  const navigation = useNavigation();
  const route = useRoute<DetailRoute>();
  const { userId } = route.params;
  const { t } = useTranslation();
  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);
  const { showSuccess, showError } = useToast();

  const {
    user,
    isLoading,
    error,
    assignRole,
    removeRole,
    resetPassword,
    disableUser,
    enableUser,
    deleteUser,
  } = useUserDetail(userId);

  const { roles: allRoles } = useRoles();

  const [showAssignRole, setShowAssignRole] = useState(false);
  const [showResetPassword, setShowResetPassword] = useState(false);
  const [newPassword, setNewPassword] = useState('');

  const handleRemoveRole = useCallback(
    (roleName: string) => {
      Alert.alert(
        t('admin.userDetail.title'),
        t('admin.userDetail.removeRoleConfirm', { role: roleName }),
        [
          { text: t('common.cancel'), style: 'cancel' },
          {
            text: t('admin.userDetail.removeRole'),
            style: 'destructive',
            onPress: async () => {
              try {
                await removeRole(roleName);
              } catch {
                showError('admin.userDetail.removeRoleError');
              }
            },
          },
        ],
      );
    },
    [t, removeRole, showError],
  );

  const handleAssignRole = useCallback(
    async (roleName: string) => {
      try {
        await assignRole(roleName);
        setShowAssignRole(false);
        showSuccess('admin.userDetail.roleAssigned');
      } catch {
        showError('admin.userDetail.assignRoleError');
      }
    },
    [assignRole, showSuccess, showError],
  );

  const handleToggleLock = useCallback(() => {
    if (!user) return;
    const action = user.isLockedOut ? enableUser : disableUser;
    const label = user.isLockedOut
      ? t('admin.userDetail.enable')
      : t('admin.userDetail.disable');
    const confirmMsg = user.isLockedOut
      ? t('admin.userDetail.enableConfirm', { username: user.username })
      : t('admin.userDetail.disableConfirm', { username: user.username });

    Alert.alert(t('admin.userDetail.title'), confirmMsg, [
      { text: t('common.cancel'), style: 'cancel' },
      {
        text: label,
        onPress: async () => {
          try {
            await action();
          } catch {
            showError('admin.userDetail.toggleLockError');
          }
        },
      },
    ]);
  }, [user, enableUser, disableUser, t, showError]);

  const handleDelete = useCallback(() => {
    if (!user) return;
    Alert.alert(
      t('admin.userDetail.title'),
      t('admin.userDetail.deleteConfirm', { username: user.username }),
      [
        { text: t('common.cancel'), style: 'cancel' },
        {
          text: t('admin.userDetail.deleteUser'),
          style: 'destructive',
          onPress: async () => {
            try {
              await deleteUser();
              navigation.goBack();
            } catch {
              showError('admin.userDetail.deleteError');
            }
          },
        },
      ],
    );
  }, [user, deleteUser, navigation, t, showError]);

  const handleResetPassword = useCallback(async () => {
    if (!newPassword.trim()) return;
    try {
      await resetPassword(newPassword.trim());
      setShowResetPassword(false);
      setNewPassword('');
      showSuccess('admin.userDetail.passwordReset');
    } catch {
      showError('admin.userDetail.passwordResetError');
    }
  }, [newPassword, resetPassword, showSuccess, showError]);

  if (isLoading) {
    return (
      <View style={styles.center}>
        <Text style={styles.loadingText}>{t('common.loading')}</Text>
      </View>
    );
  }

  if (error || !user) {
    return (
      <View style={styles.center}>
        <Text style={styles.errorText}>
          {error ?? t('admin.userDetail.notFound')}
        </Text>
      </View>
    );
  }

  const assignedRoleSet = new Set(user.roles);
  const availableRoles = allRoles.filter(r => !assignedRoleSet.has(r.name));

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.content}
      testID="user-detail-screen"
    >
      {/* User info */}
      <View style={styles.section}>
        <Text style={styles.label}>{t('admin.userDetail.username')}</Text>
        <Text style={styles.value}>{user.username}</Text>
      </View>

      <View style={styles.section}>
        <Text style={styles.label}>{t('admin.userDetail.email')}</Text>
        <Text style={styles.value}>{user.email}</Text>
      </View>

      {user.displayName ? (
        <View style={styles.section}>
          <Text style={styles.label}>{t('admin.userDetail.displayName')}</Text>
          <Text style={styles.value}>{user.displayName}</Text>
        </View>
      ) : null}

      {/* Roles */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>{t('admin.userDetail.roles')}</Text>
        <View style={styles.chipRow}>
          {user.roles.map(role => (
            <View key={role} style={styles.chip}>
              <Text style={styles.chipText}>{role}</Text>
              <Pressable
                style={({ pressed }) => [
                  styles.chipRemove,
                  pressed && { opacity: 0.7 },
                ]}
                onPress={() => handleRemoveRole(role)}
                accessibilityRole="button"
                accessibilityLabel={`${t(
                  'admin.userDetail.removeRole',
                )} ${role}`}
                testID={`remove-role-${role}`}
              >
                <Text style={styles.chipRemoveText}>✕</Text>
              </Pressable>
            </View>
          ))}
        </View>

        {!showAssignRole ? (
          <Pressable
            style={({ pressed }) => [
              styles.actionBtn,
              pressed && { opacity: 0.7 },
            ]}
            onPress={() => setShowAssignRole(true)}
            accessibilityRole="button"
            accessibilityLabel={t('admin.userDetail.assignRole')}
            testID="show-assign-role"
          >
            <Text style={styles.actionBtnText}>
              {t('admin.userDetail.assignRole')}
            </Text>
          </Pressable>
        ) : (
          <View style={styles.assignContainer}>
            {availableRoles.length === 0 ? (
              <Text style={styles.noRolesText}>
                {t('admin.userDetail.noAvailableRoles')}
              </Text>
            ) : (
              availableRoles.map(role => (
                <Pressable
                  key={role.name}
                  style={({ pressed }) => [
                    styles.assignRoleItem,
                    pressed && { opacity: 0.7 },
                  ]}
                  onPress={() => handleAssignRole(role.name)}
                  accessibilityRole="button"
                  accessibilityLabel={`${t('admin.userDetail.assignRole')} ${
                    role.name
                  }`}
                  testID={`assign-role-${role.name}`}
                >
                  <Text style={styles.assignRoleText}>{role.name}</Text>
                  <Text style={styles.assignRolePerms}>
                    {role.permissions.length}{' '}
                    {t('admin.userDetail.permissions')}
                  </Text>
                </Pressable>
              ))
            )}
            <Pressable
              style={({ pressed }) => [
                styles.cancelBtn,
                pressed && { opacity: 0.7 },
              ]}
              onPress={() => setShowAssignRole(false)}
              accessibilityRole="button"
              testID="cancel-assign-role"
            >
              <Text style={styles.cancelBtnText}>{t('common.cancel')}</Text>
            </Pressable>
          </View>
        )}
      </View>

      {/* Lock status */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>
          {t('admin.userDetail.accountStatus')}
        </Text>
        <Text style={styles.value}>
          {user.isLockedOut
            ? t('admin.userDetail.lockedOut')
            : t('admin.userDetail.active')}
        </Text>
        {user.lockoutEnd ? (
          <Text style={styles.caption}>
            {t('admin.userDetail.lockoutEnd')}: {user.lockoutEnd}
          </Text>
        ) : null}
      </View>

      {/* Actions */}
      <View style={styles.actions}>
        <Pressable
          style={({ pressed }) => [
            styles.actionBtn,
            pressed && { opacity: 0.7 },
          ]}
          onPress={handleToggleLock}
          accessibilityRole="button"
          accessibilityLabel={
            user.isLockedOut
              ? t('admin.userDetail.enable')
              : t('admin.userDetail.disable')
          }
          testID="toggle-lock-button"
        >
          <Text style={styles.actionBtnText}>
            {user.isLockedOut
              ? t('admin.userDetail.enable')
              : t('admin.userDetail.disable')}
          </Text>
        </Pressable>

        {!showResetPassword ? (
          <Pressable
            style={({ pressed }) => [
              styles.actionBtn,
              pressed && { opacity: 0.7 },
            ]}
            onPress={() => setShowResetPassword(true)}
            accessibilityRole="button"
            accessibilityLabel={t('admin.userDetail.resetPassword')}
            testID="show-reset-password"
          >
            <Text style={styles.actionBtnText}>
              {t('admin.userDetail.resetPassword')}
            </Text>
          </Pressable>
        ) : (
          <View style={styles.resetPwContainer}>
            <TextInput
              style={styles.resetPwInput}
              placeholder={t('admin.userDetail.newPassword')}
              placeholderTextColor={colors.textTertiary}
              value={newPassword}
              onChangeText={setNewPassword}
              secureTextEntry
              testID="new-password-input"
            />
            <View style={styles.resetPwActions}>
              <Pressable
                style={({ pressed }) => [
                  styles.submitBtn,
                  pressed && { opacity: 0.7 },
                ]}
                onPress={handleResetPassword}
                disabled={!newPassword.trim()}
                accessibilityRole="button"
                accessibilityLabel={t('admin.userDetail.resetPassword')}
                testID="confirm-reset-password"
              >
                <Text style={styles.submitBtnText}>
                  {t('admin.userDetail.resetPassword')}
                </Text>
              </Pressable>
              <Pressable
                style={({ pressed }) => [
                  styles.cancelBtn,
                  pressed && { opacity: 0.7 },
                ]}
                onPress={() => {
                  setShowResetPassword(false);
                  setNewPassword('');
                }}
                accessibilityRole="button"
                testID="cancel-reset-password"
              >
                <Text style={styles.cancelBtnText}>{t('common.cancel')}</Text>
              </Pressable>
            </View>
          </View>
        )}

        <Pressable
          style={({ pressed }) => [
            styles.deleteBtn,
            pressed && { opacity: 0.7 },
          ]}
          onPress={handleDelete}
          accessibilityRole="button"
          accessibilityLabel={t('admin.userDetail.deleteUser')}
          testID="delete-user-button"
        >
          <Text style={styles.deleteBtnText}>
            {t('admin.userDetail.deleteUser')}
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
    section: {
      marginBottom: spacing.lg,
    },
    sectionTitle: {
      ...t.typography.heading,
      marginBottom: spacing.sm,
    },
    label: {
      ...t.typography.label,
      color: t.colors.textSecondary,
      marginBottom: spacing.xs,
    },
    value: {
      ...t.typography.body,
      color: t.colors.textPrimary,
    },
    caption: {
      ...t.typography.caption,
      color: t.colors.textTertiary,
      marginTop: spacing.xs,
    },
    chipRow: {
      flexDirection: 'row',
      flexWrap: 'wrap',
      gap: spacing.xs,
      marginBottom: spacing.sm,
    },
    chip: {
      flexDirection: 'row',
      alignItems: 'center',
      backgroundColor: t.colors.primaryLight,
      borderRadius: borderRadius.xl,
      paddingHorizontal: spacing.md,
      paddingVertical: spacing.xs,
    },
    chipText: {
      ...t.typography.caption,
      color: t.colors.primary,
      fontWeight: '600',
    },
    chipRemove: {
      marginLeft: spacing.xs,
      width: 20,
      height: 20,
      alignItems: 'center',
      justifyContent: 'center',
    },
    chipRemoveText: {
      fontSize: 10,
      color: t.colors.primaryDark,
      fontWeight: '700',
    },
    actions: {
      gap: spacing.md,
      marginTop: spacing.lg,
    },
    actionBtn: {
      backgroundColor: t.colors.surface,
      borderRadius: borderRadius.md,
      borderWidth: 1.5,
      borderColor: t.colors.primary,
      paddingVertical: 12,
      alignItems: 'center',
      minHeight: MIN_TOUCH,
      justifyContent: 'center',
    },
    actionBtnText: {
      ...t.typography.body,
      color: t.colors.primary,
      fontWeight: '600',
    },
    deleteBtn: {
      backgroundColor: t.colors.errorLight,
      borderRadius: borderRadius.md,
      borderWidth: 1.5,
      borderColor: t.colors.error,
      paddingVertical: 12,
      alignItems: 'center',
      minHeight: MIN_TOUCH,
      justifyContent: 'center',
    },
    deleteBtnText: {
      ...t.typography.body,
      color: t.colors.error,
      fontWeight: '600',
    },
    assignContainer: {
      gap: spacing.xs,
    },
    assignRoleItem: {
      backgroundColor: t.colors.surface,
      borderRadius: borderRadius.md,
      borderWidth: 1,
      borderColor: t.colors.border,
      paddingHorizontal: spacing.md,
      paddingVertical: spacing.sm,
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      minHeight: MIN_TOUCH,
    },
    assignRoleText: {
      ...t.typography.body,
      color: t.colors.textPrimary,
    },
    assignRolePerms: {
      ...t.typography.caption,
      color: t.colors.textTertiary,
    },
    noRolesText: {
      ...t.typography.caption,
      color: t.colors.textTertiary,
    },
    cancelBtn: {
      paddingVertical: 10,
      alignItems: 'center',
      minHeight: MIN_TOUCH,
      justifyContent: 'center',
    },
    cancelBtnText: {
      ...t.typography.caption,
      color: t.colors.textSecondary,
    },
    resetPwContainer: {
      gap: spacing.sm,
    },
    resetPwInput: {
      backgroundColor: t.colors.surface,
      borderRadius: borderRadius.md,
      borderWidth: 1,
      borderColor: t.colors.border,
      paddingHorizontal: spacing.md,
      paddingVertical: 12,
      ...t.typography.body,
      color: t.colors.textPrimary,
    },
    resetPwActions: {
      flexDirection: 'row',
      gap: spacing.md,
      alignItems: 'center',
    },
    submitBtn: {
      backgroundColor: t.colors.primary,
      borderRadius: borderRadius.md,
      paddingHorizontal: spacing.xl,
      paddingVertical: 10,
      minHeight: MIN_TOUCH,
      alignItems: 'center',
      justifyContent: 'center',
    },
    submitBtnText: {
      ...t.typography.bodySmall,
      color: t.colors.textInverse,
      fontWeight: '600',
    },
  });

export default UserDetailScreen;
