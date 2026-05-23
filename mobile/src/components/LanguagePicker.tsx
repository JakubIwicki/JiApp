import React, { useCallback } from 'react';
import { StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import * as storageService from '../services/storageService';
import { colors } from '../styles/theme';

const LanguagePicker: React.FC = () => {
  const { i18n, t } = useTranslation();

  const currentLanguage = i18n.language?.startsWith('pl') ? 'pl' : 'en';

  const toggleLanguage = useCallback(async () => {
    const newLanguage = currentLanguage === 'pl' ? 'en' : 'pl';
    await i18n.changeLanguage(newLanguage);
    await storageService.saveLanguage(newLanguage);
  }, [currentLanguage, i18n]);

  return (
    <View style={styles.container}>
      <View style={styles.row}>
        <Text style={styles.labelText}>{t('settings.language')}</Text>
        <View style={styles.togglePill} testID="language-picker-button">
          <TouchableOpacity
            onPress={currentLanguage !== 'pl' ? toggleLanguage : undefined}
            activeOpacity={currentLanguage === 'pl' ? 1 : 0.6}
            accessibilityRole="button"
            accessibilityLabel={t('settings.languagePolish')}
            style={[
              styles.pillOption,
              currentLanguage === 'pl' && styles.pillActive,
            ]}
          >
            <Text
              style={[
                styles.pillText,
                currentLanguage === 'pl'
                  ? styles.pillTextActive
                  : styles.pillTextInactive,
              ]}
            >
              PL
            </Text>
          </TouchableOpacity>
          <TouchableOpacity
            onPress={currentLanguage !== 'en' ? toggleLanguage : undefined}
            activeOpacity={currentLanguage === 'en' ? 1 : 0.6}
            accessibilityRole="button"
            accessibilityLabel={t('settings.languageEnglish')}
            style={[
              styles.pillOption,
              currentLanguage === 'en' && styles.pillActive,
            ]}
          >
            <Text
              style={[
                styles.pillText,
                currentLanguage === 'en'
                  ? styles.pillTextActive
                  : styles.pillTextInactive,
              ]}
            >
              EN
            </Text>
          </TouchableOpacity>
        </View>
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    backgroundColor: colors.surface,
    borderRadius: 10,
    overflow: 'hidden',
  },
  row: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 14,
  },
  labelText: {
    fontSize: 16,
    color: colors.textPrimary,
  },
  togglePill: {
    flexDirection: 'row',
    backgroundColor: colors.primaryLight,
    borderRadius: 10,
    padding: 3,
  },
  pillOption: {
    paddingHorizontal: 14,
    paddingVertical: 6,
    borderRadius: 8,
  },
  pillActive: {
    backgroundColor: colors.primary,
  },
  pillText: {
    fontSize: 14,
    fontWeight: '600',
  },
  pillTextActive: {
    color: colors.textInverse,
  },
  pillTextInactive: {
    color: colors.textTertiary,
  },
});

export default LanguagePicker;
