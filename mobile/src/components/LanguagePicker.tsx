import React, { useCallback } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import * as storageService from '../services/storageService';
import { useThemedStyles } from '../context/ThemeContext';
import type { Theme } from '../styles/theme';

const LanguagePicker: React.FC = () => {
  const { i18n, t } = useTranslation();
  const styles = useThemedStyles(makeStyles);

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
          <Pressable
            onPress={currentLanguage !== 'pl' ? toggleLanguage : undefined}
            accessibilityRole="button"
            accessibilityLabel={t('settings.languagePolish')}
            style={({ pressed }) => [
              styles.pillOption,
              currentLanguage === 'pl' && styles.pillActive,
              pressed && currentLanguage !== 'pl' && { opacity: 0.7 },
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
          </Pressable>
          <Pressable
            onPress={currentLanguage !== 'en' ? toggleLanguage : undefined}
            accessibilityRole="button"
            accessibilityLabel={t('settings.languageEnglish')}
            style={({ pressed }) => [
              styles.pillOption,
              currentLanguage === 'en' && styles.pillActive,
              pressed && currentLanguage !== 'en' && { opacity: 0.7 },
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
          </Pressable>
        </View>
      </View>
    </View>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      backgroundColor: t.colors.surface,
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
      color: t.colors.textPrimary,
    },
    togglePill: {
      flexDirection: 'row',
      backgroundColor: t.colors.primaryLight,
      borderRadius: 10,
      padding: 3,
    },
    pillOption: {
      paddingHorizontal: 14,
      paddingVertical: 6,
      borderRadius: 8,
    },
    pillActive: {
      backgroundColor: t.colors.primary,
    },
    pillText: {
      fontSize: 14,
      fontWeight: '600',
    },
    pillTextActive: {
      color: t.colors.textInverse,
    },
    pillTextInactive: {
      color: t.colors.textTertiary,
    },
  });

export default LanguagePicker;
