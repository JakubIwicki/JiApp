import { useCallback, useMemo, useState } from 'react';
import { Alert } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { useTranslation } from 'react-i18next';
import useToast from '../../../hooks/useToast';
import useUserDetail from './useUserDetail';
import useRoles from './useRoles';
import type { RoleSummary, UserDetail } from '../types/api';
import type { AdminStackParamList } from '../../../navigation/types';

type NavigationProp = NativeStackNavigationProp<
  AdminStackParamList,
  'UserDetail'
>;

export interface UseUserDetailScreenResult {
  user: UserDetail | null;
  isLoading: boolean;
  error: string | null;
  showAssignRole: boolean;
  showResetPassword: boolean;
  newPassword: string;
  availableRoles: RoleSummary[];
  setShowAssignRole: (value: boolean) => void;
  setShowResetPassword: (value: boolean) => void;
  setNewPassword: (value: string) => void;
  cancelResetPassword: () => void;
  handleRemoveRole: (roleName: string) => void;
  handleAssignRole: (roleName: string) => Promise<void>;
  handleToggleLock: () => void;
  handleDelete: () => void;
  handleResetPassword: () => Promise<void>;
}

const useUserDetailScreen = (userId: number): UseUserDetailScreenResult => {
  const { t } = useTranslation();
  const navigation = useNavigation<NavigationProp>();
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

  const availableRoles = useMemo(() => {
    if (!user) return [];
    const assignedRoleSet = new Set(user.roles);
    return allRoles.filter(role => !assignedRoleSet.has(role.name));
  }, [user, allRoles]);

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

  const cancelResetPassword = useCallback(() => {
    setShowResetPassword(false);
    setNewPassword('');
  }, []);

  return {
    user,
    isLoading,
    error,
    showAssignRole,
    showResetPassword,
    newPassword,
    availableRoles,
    setShowAssignRole,
    setShowResetPassword,
    setNewPassword,
    cancelResetPassword,
    handleRemoveRole,
    handleAssignRole,
    handleToggleLock,
    handleDelete,
    handleResetPassword,
  };
};

export default useUserDetailScreen;
