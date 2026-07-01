import React, { useCallback, useReducer, useState } from 'react';
import {
  View,
  Text,
  TextInput,
  Pressable,
  ScrollView,
  StyleSheet,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import useUsers from '../hooks/useUsers';
import useRoles from '../hooks/useRoles';
import useToast from '../../../hooks/useToast';
import { useTheme, useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';

const MIN_TOUCH = 44;

interface CreateUserFormState {
  username: string;
  email: string;
  password: string;
  displayName: string;
  selectedRoles: string[];
}

type CreateUserFormAction =
  | {
      type: 'SET_FIELD';
      field: 'username' | 'email' | 'password' | 'displayName';
      value: string;
    }
  | { type: 'TOGGLE_ROLE'; role: string }
  | { type: 'RESET' };

const initialFormState: CreateUserFormState = {
  username: '',
  email: '',
  password: '',
  displayName: '',
  selectedRoles: [],
};

function createUserFormReducer(
  state: CreateUserFormState,
  action: CreateUserFormAction,
): CreateUserFormState {
  switch (action.type) {
    case 'SET_FIELD':
      return { ...state, [action.field]: action.value };
    case 'TOGGLE_ROLE': {
      const has = state.selectedRoles.includes(action.role);
      return {
        ...state,
        selectedRoles: has
          ? state.selectedRoles.filter(r => r !== action.role)
          : [...state.selectedRoles, action.role],
      };
    }
    case 'RESET':
      return initialFormState;
    default:
      return state;
  }
}

const CreateUserScreen: React.FC = () => {
  const navigation = useNavigation();
  const { t } = useTranslation();
  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);
  const { createUser } = useUsers();
  const { roles } = useRoles();
  const { showSuccess, showError } = useToast();

  const [form, dispatch] = useReducer(createUserFormReducer, initialFormState);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = useCallback(async () => {
    if (!form.username.trim() || !form.email.trim() || !form.password.trim()) {
      showError('admin.createUser.validationError');
      return;
    }

    setIsSubmitting(true);
    try {
      await createUser({
        username: form.username.trim(),
        email: form.email.trim(),
        password: form.password,
        displayName: form.displayName.trim(),
        roles: form.selectedRoles,
      });
      showSuccess('admin.createUser.success');
      navigation.goBack();
    } catch {
      showError('admin.createUser.createError');
    } finally {
      setIsSubmitting(false);
    }
  }, [form, createUser, navigation, showSuccess, showError]);

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.content}
      testID="create-user-screen"
    >
      <Text style={styles.title}>{t('admin.createUser.title')}</Text>

      <Text style={styles.label}>{t('admin.createUser.username')}</Text>
      <TextInput
        style={styles.input}
        value={form.username}
        onChangeText={value =>
          dispatch({ type: 'SET_FIELD', field: 'username', value })
        }
        placeholder={t('admin.createUser.usernamePlaceholder')}
        placeholderTextColor={colors.textTertiary}
        autoCapitalize="none"
        testID="create-user-username"
      />

      <Text style={styles.label}>{t('admin.createUser.email')}</Text>
      <TextInput
        style={styles.input}
        value={form.email}
        onChangeText={value =>
          dispatch({ type: 'SET_FIELD', field: 'email', value })
        }
        placeholder={t('admin.createUser.emailPlaceholder')}
        placeholderTextColor={colors.textTertiary}
        autoCapitalize="none"
        keyboardType="email-address"
        testID="create-user-email"
      />

      <Text style={styles.label}>{t('admin.createUser.password')}</Text>
      <TextInput
        style={styles.input}
        value={form.password}
        onChangeText={value =>
          dispatch({ type: 'SET_FIELD', field: 'password', value })
        }
        placeholder={t('admin.createUser.passwordPlaceholder')}
        placeholderTextColor={colors.textTertiary}
        secureTextEntry
        testID="create-user-password"
      />

      <Text style={styles.label}>{t('admin.createUser.displayName')}</Text>
      <TextInput
        style={styles.input}
        value={form.displayName}
        onChangeText={value =>
          dispatch({ type: 'SET_FIELD', field: 'displayName', value })
        }
        placeholder={t('admin.createUser.displayNamePlaceholder')}
        placeholderTextColor={colors.textTertiary}
        testID="create-user-displayname"
      />

      <Text style={styles.label}>{t('admin.createUser.roles')}</Text>
      <View style={styles.roleRow}>
        {roles.map(role => {
          const isSelected = form.selectedRoles.includes(role.name);
          return (
            <Pressable
              key={role.name}
              style={({ pressed }) => [
                styles.roleChip,
                isSelected && styles.roleChipActive,
                pressed && { opacity: 0.7 },
              ]}
              onPress={() => dispatch({ type: 'TOGGLE_ROLE', role: role.name })}
              accessibilityRole="checkbox"
              accessibilityState={{ checked: isSelected }}
              accessibilityLabel={`${role.name} (${role.permissions.length} permissions)`}
              testID={`role-checkbox-${role.name}`}
            >
              <Text
                style={[
                  styles.roleChipText,
                  isSelected && styles.roleChipTextActive,
                ]}
              >
                {role.name}
              </Text>
            </Pressable>
          );
        })}
      </View>

      <Pressable
        style={({ pressed }) => [
          styles.submitBtn,
          isSubmitting && styles.submitBtnDisabled,
          pressed && { opacity: 0.7 },
        ]}
        onPress={handleSubmit}
        disabled={isSubmitting}
        accessibilityRole="button"
        accessibilityLabel={t('admin.createUser.create')}
        testID="create-user-submit"
      >
        <Text style={styles.submitBtnText}>
          {isSubmitting ? t('common.loading') : t('admin.createUser.create')}
        </Text>
      </Pressable>
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
    title: {
      ...t.typography.heading,
      marginBottom: spacing.lg,
    },
    label: {
      ...t.typography.label,
      color: t.colors.textSecondary,
      marginBottom: spacing.xs,
      marginTop: spacing.md,
    },
    input: {
      backgroundColor: t.colors.surface,
      borderRadius: borderRadius.md,
      borderWidth: 1,
      borderColor: t.colors.border,
      paddingHorizontal: spacing.md,
      paddingVertical: 12,
      ...t.typography.body,
      color: t.colors.textPrimary,
    },
    roleRow: {
      flexDirection: 'row',
      flexWrap: 'wrap',
      gap: spacing.xs,
    },
    roleChip: {
      borderRadius: borderRadius.xl,
      borderWidth: 1,
      borderColor: t.colors.border,
      paddingHorizontal: spacing.md,
      paddingVertical: spacing.xs,
      backgroundColor: t.colors.surface,
      minHeight: MIN_TOUCH,
      alignItems: 'center',
      justifyContent: 'center',
    },
    roleChipActive: {
      backgroundColor: t.colors.primary,
      borderColor: t.colors.primary,
    },
    roleChipText: {
      ...t.typography.caption,
      color: t.colors.textSecondary,
    },
    roleChipTextActive: {
      color: t.colors.textInverse,
    },
    submitBtn: {
      backgroundColor: t.colors.primary,
      borderRadius: borderRadius.lg,
      paddingVertical: 14,
      alignItems: 'center',
      marginTop: spacing.xl,
      minHeight: MIN_TOUCH,
      justifyContent: 'center',
    },
    submitBtnDisabled: {
      opacity: 0.6,
    },
    submitBtnText: {
      ...t.typography.body,
      color: t.colors.textInverse,
      fontWeight: '700',
    },
  });

export default CreateUserScreen;
