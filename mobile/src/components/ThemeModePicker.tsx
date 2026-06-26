import React, { useCallback } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useTheme, useThemedStyles } from '../context/ThemeContext';
import type { Theme, ThemeMode } from '../styles/theme';

const MODES: { mode: ThemeMode; labelKey: string }[] = [
  { mode: 'system', labelKey: 'settings.themeModeSystem' },
  { mode: 'light', labelKey: 'settings.themeModeLight' },
  { mode: 'dark', labelKey: 'settings.themeModeDark' },
];

const ThemeModePicker: React.FC = () => {
  const { t } = useTranslation();
  const { themeMode, setThemeMode } = useTheme();
  const styles = useThemedStyles(makeStyles);

  const selectMode = useCallback(
    async (mode: ThemeMode) => {
      await setThemeMode(mode);
    },
    [setThemeMode],
  );

  return (
    <View style={styles.container}>
      <View style={styles.row}>
        <Text style={styles.labelText}>{t('settings.themeMode')}</Text>
        <View style={styles.togglePill} testID="theme-mode-picker-button">
          {MODES.map(({ mode, labelKey }) => {
            const isActive = themeMode === mode;
            return (
              <Pressable
                key={mode}
                onPress={!isActive ? () => selectMode(mode) : undefined}
                accessibilityRole="button"
                accessibilityLabel={t(labelKey)}
                style={({ pressed }) => [
                  styles.pillOption,
                  isActive && styles.pillActive,
                  pressed && !isActive && { opacity: 0.7 },
                ]}
              >
                <Text
                  style={[
                    styles.pillText,
                    isActive ? styles.pillTextActive : styles.pillTextInactive,
                  ]}
                >
                  {t(labelKey)}
                </Text>
              </Pressable>
            );
          })}
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
      paddingHorizontal: 12,
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

export default ThemeModePicker;
