import React, { useCallback } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useNavigation } from '@react-navigation/native';
import useAuth from '../hooks/useAuth';
import useScreenTitle from '../hooks/useScreenTitle';
import useToast from '../hooks/useToast';
import LanguagePicker from '../components/LanguagePicker';
import PalettePicker from '../components/PalettePicker';
import ThemeModePicker from '../components/ThemeModePicker';
import { APP_VERSION } from '../constants/app';
import { useTheme, useThemedStyles } from '../context/ThemeContext';
import type { Theme } from '../styles/theme';
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
  const { commonStyles } = useTheme();
  const styles = useThemedStyles(makeStyles);

  useScreenTitle('settings.title');

  const handleSwitchModule = useCallback(() => {
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
        <Text style={commonStyles.sectionHeader}>
          {t('settings.themeMode')}
        </Text>
        <View style={styles.pickerWrapper}>
          <ThemeModePicker />
        </View>
      </View>

      <View style={commonStyles.sectionContainer}>
        <Text style={commonStyles.sectionHeader}>{t('settings.theme')}</Text>
        <View style={styles.pickerWrapper}>
          <PalettePicker />
        </View>
      </View>

      <View style={commonStyles.sectionContainer}>
        <Text style={commonStyles.sectionHeader}>{t('settings.language')}</Text>
        <View style={styles.pickerWrapper}>
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

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    pickerWrapper: {
      marginHorizontal: t.spacing.lg,
    },
    infoLabel: {
      ...t.typography.body,
      color: t.colors.textPrimary,
    },
    infoValue: {
      ...t.typography.body,
      color: t.colors.textSecondary,
    },
    switchSection: {
      marginTop: t.spacing.lg,
      paddingHorizontal: t.spacing.lg,
    },
    switchButton: {
      backgroundColor: t.colors.surface,
      borderRadius: t.borderRadius.lg,
      borderWidth: 1.5,
      borderColor: t.colors.primary,
      paddingVertical: 14,
      minHeight: 48,
      alignItems: 'center',
      justifyContent: 'center',
    },
    switchText: {
      fontSize: 16,
      color: t.colors.primary,
      fontWeight: '600',
    },
    editProfileButton: {
      backgroundColor: t.colors.surface,
      borderRadius: t.borderRadius.lg,
      borderWidth: 1.5,
      borderColor: t.colors.primary,
      paddingVertical: 14,
      minHeight: 48,
      alignItems: 'center',
      justifyContent: 'center',
      marginTop: t.spacing.md,
    },
    editProfileText: {
      fontSize: 16,
      color: t.colors.primary,
      fontWeight: '600',
    },
    logoutSection: {
      marginVertical: t.spacing.lg,
      paddingHorizontal: t.spacing.lg,
    },
    logoutButton: {
      backgroundColor: t.colors.surface,
      borderRadius: 10,
      borderWidth: 1.5,
      borderColor: t.colors.error,
      paddingVertical: 14,
      alignItems: 'center',
    },
    logoutText: {
      fontSize: 16,
      color: t.colors.error,
      fontWeight: '600',
    },
    versionText: {
      fontSize: 12,
      color: t.colors.textTertiary,
      textAlign: 'center',
      marginBottom: t.spacing.xl,
    },
  });

export default SettingsScreen;
