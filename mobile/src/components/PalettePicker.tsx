import React, { useCallback } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useTheme, useThemedStyles } from '../context/ThemeContext';
import type { Theme } from '../styles/theme';

const PalettePicker: React.FC = () => {
  const { t } = useTranslation();
  const { palette, setPalette } = useTheme();
  const styles = useThemedStyles(makeStyles);

  const selectPalette = useCallback(
    async (name: 'lavender' | 'wabisabi') => {
      await setPalette(name);
    },
    [setPalette],
  );

  return (
    <View style={styles.container}>
      <View style={styles.row}>
        <Text style={styles.labelText}>{t('settings.theme')}</Text>
        <View style={styles.togglePill} testID="palette-picker-button">
          <Pressable
            onPress={
              palette !== 'lavender'
                ? () => selectPalette('lavender')
                : undefined
            }
            accessibilityRole="button"
            accessibilityLabel={t('settings.themeLavender')}
            style={({ pressed }) => [
              styles.pillOption,
              palette === 'lavender' && styles.pillActive,
              pressed && palette !== 'lavender' && { opacity: 0.7 },
            ]}
          >
            <Text
              style={[
                styles.pillText,
                palette === 'lavender'
                  ? styles.pillTextActive
                  : styles.pillTextInactive,
              ]}
            >
              {t('settings.themeLavender')}
            </Text>
          </Pressable>
          <Pressable
            onPress={
              palette !== 'wabisabi'
                ? () => selectPalette('wabisabi')
                : undefined
            }
            accessibilityRole="button"
            accessibilityLabel={t('settings.themeEarthy')}
            style={({ pressed }) => [
              styles.pillOption,
              palette === 'wabisabi' && styles.pillActive,
              pressed && palette !== 'wabisabi' && { opacity: 0.7 },
            ]}
          >
            <Text
              style={[
                styles.pillText,
                palette === 'wabisabi'
                  ? styles.pillTextActive
                  : styles.pillTextInactive,
              ]}
            >
              {t('settings.themeEarthy')}
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
      paddingHorizontal: t.spacing.lg,
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

export default PalettePicker;
