import React, { useCallback, useMemo, useState } from 'react';
import { View, Text, Pressable, ScrollView, StyleSheet } from 'react-native';
import { useNavigation, useRoute, RouteProp } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import useRoles from '../hooks/useRoles';
import useToast from '../../../hooks/useToast';
import { useTheme, useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';
import { ALL_PERMISSIONS } from '../types/api';
import type { AdminStackParamList } from '../../../navigation/types';

type EditRoute = RouteProp<AdminStackParamList, 'RoleEdit'>;

const MIN_TOUCH = 44;

const RoleEditScreen: React.FC = () => {
  const navigation = useNavigation();
  const route = useRoute<EditRoute>();
  const { roleName } = route.params;
  const { t } = useTranslation();
  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);
  const { roles, updatePermissions } = useRoles();
  const { showSuccess, showError } = useToast();

  const [selectedPerms, setSelectedPerms] = useState<string[] | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  const role = useMemo(
    () => roles.find(r => r.name === roleName),
    [roles, roleName],
  );

  const isAdmin = roleName === 'Admin';
  const isReadOnly = isAdmin;

  const currentPerms = useMemo(() => {
    if (selectedPerms !== null) return selectedPerms;
    return role?.permissions ?? [];
  }, [role, selectedPerms]);

  const handleTogglePermission = useCallback(
    (perm: string) => {
      if (isReadOnly) return;
      setSelectedPerms(prev => {
        const base = prev ?? role?.permissions ?? [];
        const has = base.includes(perm);
        return has ? base.filter(p => p !== perm) : [...base, perm];
      });
    },
    [isReadOnly, role],
  );

  const handleSave = useCallback(async () => {
    setIsSaving(true);
    try {
      await updatePermissions(roleName, currentPerms);
      setSelectedPerms(null);
      showSuccess('admin.roleEdit.saved');
      navigation.goBack();
    } catch {
      showError('admin.roleEdit.saveError');
    } finally {
      setIsSaving(false);
    }
  }, [
    roleName,
    currentPerms,
    updatePermissions,
    showSuccess,
    showError,
    navigation,
  ]);

  if (!role) {
    return (
      <View style={styles.center}>
        <Text style={styles.errorText}>{t('admin.roleEdit.notFound')}</Text>
      </View>
    );
  }

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.content}
      testID="role-edit-screen"
    >
      <Text style={styles.title}>{role.name}</Text>

      {isAdmin ? (
        <Text style={styles.adminNote}>
          {t('admin.roleEdit.adminReadonly')}
        </Text>
      ) : null}

      <Text style={styles.sectionTitle}>{t('admin.roleEdit.permissions')}</Text>

      <View style={styles.permList}>
        {ALL_PERMISSIONS.map(perm => {
          const isChecked = currentPerms.includes(perm);
          return (
            <Pressable
              key={perm}
              style={({ pressed }) => [
                styles.permRow,
                pressed && !isReadOnly && { opacity: 0.7 },
                isReadOnly && styles.permRowDisabled,
              ]}
              onPress={() => handleTogglePermission(perm)}
              disabled={isReadOnly}
              accessibilityRole="checkbox"
              accessibilityState={{ checked: isChecked, disabled: isReadOnly }}
              accessibilityLabel={perm}
              testID={`perm-checkbox-${perm}`}
            >
              <View
                style={[
                  styles.checkbox,
                  isChecked && styles.checkboxChecked,
                  isReadOnly && styles.checkboxDisabled,
                ]}
              >
                {isChecked ? <Text style={styles.checkmark}>✓</Text> : null}
              </View>
              <Text
                style={[
                  styles.permLabel,
                  isReadOnly && styles.permLabelDisabled,
                ]}
              >
                {perm}
              </Text>
            </Pressable>
          );
        })}
      </View>

      {!isReadOnly ? (
        <Pressable
          style={({ pressed }) => [
            styles.saveBtn,
            isSaving && styles.saveBtnDisabled,
            pressed && { opacity: 0.7 },
          ]}
          onPress={handleSave}
          disabled={isSaving}
          accessibilityRole="button"
          accessibilityLabel={t('admin.roleEdit.save')}
          testID="save-permissions-button"
        >
          <Text style={styles.saveBtnText}>
            {isSaving ? t('common.loading') : t('admin.roleEdit.save')}
          </Text>
        </Pressable>
      ) : null}
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
    errorText: {
      ...t.typography.body,
      color: t.colors.error,
    },
    title: {
      ...t.typography.title,
      marginBottom: spacing.sm,
    },
    adminNote: {
      ...t.typography.bodySmall,
      color: t.colors.info,
      marginBottom: spacing.lg,
      padding: spacing.md,
      backgroundColor: t.colors.primaryLight,
      borderRadius: borderRadius.md,
      overflow: 'hidden',
    },
    sectionTitle: {
      ...t.typography.heading,
      marginBottom: spacing.md,
      marginTop: spacing.lg,
    },
    permList: {
      gap: spacing.xs,
    },
    permRow: {
      flexDirection: 'row',
      alignItems: 'center',
      paddingVertical: spacing.sm,
      minHeight: MIN_TOUCH,
    },
    permRowDisabled: {
      opacity: 0.6,
    },
    checkbox: {
      width: 24,
      height: 24,
      borderRadius: borderRadius.sm,
      borderWidth: 2,
      borderColor: t.colors.border,
      alignItems: 'center',
      justifyContent: 'center',
      marginRight: spacing.md,
      backgroundColor: t.colors.surface,
    },
    checkboxChecked: {
      backgroundColor: t.colors.primary,
      borderColor: t.colors.primary,
    },
    checkboxDisabled: {
      backgroundColor: t.colors.placeholder,
      borderColor: t.colors.border,
    },
    checkmark: {
      color: t.colors.textInverse,
      fontSize: 14,
      fontWeight: '700',
    },
    permLabel: {
      ...t.typography.body,
      color: t.colors.textPrimary,
    },
    permLabelDisabled: {
      color: t.colors.textTertiary,
    },
    saveBtn: {
      backgroundColor: t.colors.primary,
      borderRadius: borderRadius.lg,
      paddingVertical: 14,
      alignItems: 'center',
      marginTop: spacing.xl,
      minHeight: MIN_TOUCH,
      justifyContent: 'center',
    },
    saveBtnDisabled: {
      opacity: 0.6,
    },
    saveBtnText: {
      ...t.typography.body,
      color: t.colors.textInverse,
      fontWeight: '700',
    },
  });

export default RoleEditScreen;
