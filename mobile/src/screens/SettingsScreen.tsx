import React, { useCallback } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useNavigation } from '@react-navigation/native';
import useAuth from '../hooks/useAuth';
import useScreenTitle from '../hooks/useScreenTitle';
import useToast from '../hooks/useToast';
import LanguagePicker from '../components/LanguagePicker';
import { APP_VERSION } from '../constants/app';
import {
  borderRadius,
  colors,
  commonStyles,
  typography,
  spacing,
} from '../styles/theme';
import type {
  RootStackParamList,
  SettingsStackParamList,
} from '../navigation/types';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

type SettingsNavigationProp = NativeStackNavigationProp<
  SettingsStackParamList,
  'Settings'
>;

const SettingsScreen: React.FC = () => {
  const { t } = useTranslation();
  const navigation = useNavigation<SettingsNavigationProp>();
  const { displayName, username, logout } = useAuth();
  const { showSuccess } = useToast();

  useScreenTitle('settings.title');

  const handleSwitchModule = useCallback(() => {
    // The module picker lives on the root stack, above the tab navigator.
    const rootNavigation =
      navigation.getParent<NativeStackNavigationProp<RootStackParamList>>();
    rootNavigation?.navigate('ModuleSelection');
  }, [navigation]);

  const handleEditProfile = useCallback(() => {
    navigation.navigate('EditProfile');
  }, [navigation]);

  return (
    <ScrollView
      style={commonStyles.screenContainer}
      contentContainerStyle={commonStyles.scrollContent}
    >
      <View style={commonStyles.sectionContainer}>
        <Text style={commonStyles.sectionHeader}>{t('settings.language')}</Text>
        <View style={styles.languagePickerWrapper}>
          <LanguagePicker />
        </View>
      </View>

      <View style={commonStyles.sectionContainer}>
        <Text style={commonStyles.sectionHeader}>{t('settings.account')}</Text>
        <View style={commonStyles.card}>
          <View style={commonStyles.cardRow}>
            <Text style={styles.infoLabel}>{t('settings.displayName')}</Text>
            <Text style={styles.infoValue}>{displayName ?? '-'}</Text>
          </View>
          <View style={commonStyles.cardSeparator} />
          <View style={commonStyles.cardRow}>
            <Text style={styles.infoLabel}>{t('settings.username')}</Text>
            <Text style={styles.infoValue}>{username ?? '-'}</Text>
          </View>
        </View>
        <Pressable
          style={({ pressed }) => [
            styles.editProfileButton,
            pressed && { opacity: 0.7 },
          ]}
          onPress={handleEditProfile}
          testID="edit-profile-button"
          accessibilityRole="button"
          accessibilityLabel={t('settings.editProfile')}
        >
          <Text style={styles.editProfileText}>
            {t('settings.editProfile')}
          </Text>
        </Pressable>
      </View>

      <View style={styles.switchSection}>
        <Pressable
          style={({ pressed }) => [
            styles.switchButton,
            pressed && { opacity: 0.7 },
          ]}
          onPress={handleSwitchModule}
          testID="switch-module-button"
          accessibilityRole="button"
          accessibilityLabel={t('modules.switch')}
        >
          <Text style={styles.switchText}>{t('modules.switch')}</Text>
        </Pressable>
      </View>

      <View style={styles.logoutSection}>
        <Pressable
          style={({ pressed }) => [
            styles.logoutButton,
            pressed && { opacity: 0.7 },
          ]}
          onPress={() => {
            showSuccess('toast.loggedOff');
            logout();
          }}
          testID="logout-button"
          accessibilityRole="button"
        >
          <Text style={styles.logoutText}>{t('settings.logout')}</Text>
        </Pressable>
      </View>

      <Text style={styles.versionText}>
        {t('settings.version')}: {APP_VERSION}
      </Text>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  languagePickerWrapper: {
    marginHorizontal: spacing.lg,
  },
  infoLabel: {
    ...typography.body,
    color: colors.textPrimary,
  },
  infoValue: {
    ...typography.body,
    color: colors.textSecondary,
  },
  switchSection: {
    marginTop: spacing.lg,
    paddingHorizontal: spacing.lg,
  },
  switchButton: {
    backgroundColor: colors.surface,
    borderRadius: borderRadius.lg,
    borderWidth: 1.5,
    borderColor: colors.primary,
    paddingVertical: 14,
    minHeight: 48,
    alignItems: 'center',
    justifyContent: 'center',
  },
  switchText: {
    fontSize: 16,
    color: colors.primary,
    fontWeight: '600',
  },
  editProfileButton: {
    backgroundColor: colors.surface,
    borderRadius: borderRadius.lg,
    borderWidth: 1.5,
    borderColor: colors.primary,
    paddingVertical: 14,
    minHeight: 48,
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: spacing.md,
    marginHorizontal: spacing.lg,
  },
  editProfileText: {
    fontSize: 16,
    color: colors.primary,
    fontWeight: '600',
  },
  logoutSection: {
    marginVertical: spacing.lg,
    paddingHorizontal: spacing.lg,
  },
  logoutButton: {
    backgroundColor: colors.surface,
    borderRadius: 10,
    borderWidth: 1.5,
    borderColor: colors.error,
    paddingVertical: 14,
    alignItems: 'center',
  },
  logoutText: {
    fontSize: 16,
    color: colors.error,
    fontWeight: '600',
  },
  versionText: {
    fontSize: 12,
    color: colors.textTertiary,
    textAlign: 'center',
    marginBottom: spacing.xl,
  },
});

export default SettingsScreen;
