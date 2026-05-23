import React from 'react';
import { ScrollView, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import useAuth from '../hooks/useAuth';
import useScreenTitle from '../hooks/useScreenTitle';
import LanguagePicker from '../components/LanguagePicker';
import { APP_VERSION } from '../constants/app';
import { colors, commonStyles, typography, spacing } from '../styles/theme';

const SettingsScreen: React.FC = () => {
  const { t } = useTranslation();
  const { displayName, username, logout } = useAuth();

  useScreenTitle('settings.title');

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
      </View>

      <View style={styles.logoutSection}>
        <TouchableOpacity
          style={styles.logoutButton}
          onPress={logout}
          testID="logout-button"
          accessibilityRole="button"
        >
          <Text style={styles.logoutText}>{t('settings.logout')}</Text>
        </TouchableOpacity>
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
