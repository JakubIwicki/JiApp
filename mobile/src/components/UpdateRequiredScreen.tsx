import React, { useCallback, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  BackHandler,
  Linking,
  Pressable,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemedStyles } from '../context/ThemeContext';
import type { Theme } from '../styles/theme';
import { borderRadius, spacing, zIndexScale } from '../styles/theme';

interface Props {
  downloadUrl: string;
}

const UpdateRequiredScreen: React.FC<Props> = ({ downloadUrl }) => {
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();
  const styles = useThemedStyles(makeStyles);
  const [downloadFailed, setDownloadFailed] = useState(false);

  const handleDownload = useCallback(async () => {
    try {
      await Linking.openURL(downloadUrl);
    } catch {
      setDownloadFailed(true);
    }
  }, [downloadUrl]);

  const handleExit = useCallback(() => {
    BackHandler.exitApp();
  }, []);

  return (
    <View style={styles.container} testID="update-required-screen">
      <View style={styles.background} />
      <View style={[styles.content, { paddingTop: insets.top }]}>
        <Text style={styles.warningIcon} testID="warning-icon">
          {'⚠'}
        </Text>
        <Text style={styles.title}>{t('update.title')}</Text>
        <Text style={styles.message}>{t('update.message')}</Text>
        {downloadFailed && (
          <Text style={styles.errorText} testID="update-download-error">
            {t('common.error')}
          </Text>
        )}
        <View style={styles.buttonGroup}>
          <Pressable
            style={({ pressed }) => [
              styles.button,
              styles.downloadButton,
              pressed && { opacity: 0.7 },
            ]}
            onPress={handleDownload}
            accessibilityRole="button"
            accessibilityLabel={t('update.download')}
            testID="update-download-button"
          >
            <Text style={styles.downloadButtonText}>
              {t('update.download')}
            </Text>
          </Pressable>
          <Pressable
            style={({ pressed }) => [
              styles.button,
              styles.exitButton,
              pressed && { opacity: 0.7 },
            ]}
            onPress={handleExit}
            accessibilityRole="button"
            accessibilityLabel={t('update.exit')}
            testID="update-exit-button"
          >
            <Text style={styles.exitButtonText}>{t('update.exit')}</Text>
          </Pressable>
        </View>
      </View>
    </View>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      position: 'absolute',
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      zIndex: zIndexScale.overlay,
    },
    background: {
      position: 'absolute',
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      backgroundColor: t.colors.background,
    },
    content: {
      flex: 1,
      justifyContent: 'center',
      alignItems: 'center',
      paddingHorizontal: spacing.xl,
    },
    warningIcon: {
      fontSize: 48,
      textAlign: 'center',
      marginBottom: spacing.lg,
    },
    title: {
      fontSize: t.typography.heading.fontSize,
      fontWeight: t.typography.heading.fontWeight,
      color: t.colors.textPrimary,
      textAlign: 'center',
      marginBottom: spacing.sm,
      marginTop: spacing.lg,
    },
    message: {
      fontSize: t.typography.bodySmall.fontSize,
      color: t.colors.textTertiary,
      textAlign: 'center',
      marginBottom: spacing.xl,
      marginTop: spacing.sm,
    },
    errorText: {
      fontSize: t.typography.bodySmall.fontSize,
      color: t.colors.error,
      textAlign: 'center',
      marginBottom: spacing.md,
    },
    buttonGroup: {
      gap: spacing.md,
      alignItems: 'center',
    },
    button: {
      borderRadius: borderRadius.md,
      paddingHorizontal: 24,
      paddingVertical: 12,
      minWidth: 200,
      alignItems: 'center',
      minHeight: 44,
      justifyContent: 'center',
    },
    downloadButton: {
      backgroundColor: t.colors.primary,
    },
    exitButton: {
      backgroundColor: t.colors.surface,
      borderWidth: 1,
      borderColor: t.colors.border,
    },
    downloadButtonText: {
      color: t.colors.textInverse,
      fontSize: t.typography.body.fontSize,
      fontWeight: '600',
    },
    exitButtonText: {
      color: t.colors.textPrimary,
      fontSize: t.typography.body.fontSize,
      fontWeight: '600',
    },
  });

export default UpdateRequiredScreen;
