import React, { useCallback } from 'react';
import { Pressable, StyleSheet, Text } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import TabIcon from '../../components/TabIcon';
import { spacing } from '../../styles/theme';
import type { Theme } from '../../styles/theme';
import { useThemedStyles, useTheme } from '../../context/ThemeContext';
import type { RootStackParamList } from '../types';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import type { CompositeNavigationProp } from '@react-navigation/native';

type AnyParamList = Record<string, object | undefined>;

type HeaderNavProp = CompositeNavigationProp<
  NativeStackNavigationProp<AnyParamList>,
  NativeStackNavigationProp<RootStackParamList>
>;

export const SwitchModuleButton: React.FC = () => {
  const { t } = useTranslation();
  const navigation = useNavigation<HeaderNavProp>();
  const styles = useThemedStyles(makeStyles);

  const handlePress = useCallback(() => {
    navigation.navigate('ModuleSelection');
  }, [navigation]);

  return (
    <Pressable
      onPress={handlePress}
      style={({ pressed }) => [styles.headerBtn, pressed && styles.pressed]}
      accessibilityRole="button"
      accessibilityLabel={t('modules.switch')}
      testID="header-switch-module"
    >
      <Text style={styles.headerBtnText}>{t('modules.switch')}</Text>
    </Pressable>
  );
};

export const SettingsButton: React.FC = () => {
  const { t } = useTranslation();
  const navigation = useNavigation<HeaderNavProp>();
  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);

  const handlePress = useCallback(() => {
    navigation.navigate('Settings');
  }, [navigation]);

  return (
    <Pressable
      onPress={handlePress}
      style={({ pressed }) => [styles.headerBtn, pressed && styles.pressed]}
      accessibilityRole="button"
      accessibilityLabel={t('settings.title')}
      testID="header-settings"
    >
      <TabIcon name="settings" color={colors.primary} size={22} />
    </Pressable>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    headerBtn: {
      minHeight: 44,
      minWidth: 44,
      paddingHorizontal: spacing.sm,
      justifyContent: 'center',
      alignItems: 'center',
    },
    headerBtnText: {
      ...t.typography.link,
      color: t.colors.primary,
      fontWeight: '600',
    },
    pressed: {
      opacity: 0.6,
    },
  });
