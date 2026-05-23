import React, { useCallback } from 'react';
import {
  Image,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { useNavigation, useRoute } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import type { StackNavigationProp } from '@react-navigation/stack';
import type { RouteProp } from '@react-navigation/native';
import type { MainStackParamList, MainTabParamList } from '../navigation/types';
import Button from '../components/Button';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';
import SuccessCheckmark from '../components/SuccessCheckmark';
import FloatingParticles from '../components/FloatingParticles';
import useDownload from '../hooks/useDownload';
import useScreenTitle from '../hooks/useScreenTitle';
import { colors, commonStyles, spacing, typography, borderRadius } from '../styles/theme';

type DownloadNavigationProp = StackNavigationProp<MainStackParamList & MainTabParamList, 'Download'>;
type DownloadRouteProp = RouteProp<MainStackParamList, 'Download'>;

const DownloadScreen: React.FC = () => {
  const { t } = useTranslation();
  const navigation = useNavigation<DownloadNavigationProp>();
  const route = useRoute<DownloadRouteProp>();
  const { videoId, title, description, imageUrl, videoUrl } = route.params;

  useScreenTitle('download.title');

  const { isDownloading, error, localFilePath, download } = useDownload();

  const handleDownload = useCallback(() => {
    download({
      videoId,
      title,
      description,
      imageUrl,
      videoUrl,
    });
  }, [videoId, title, description, imageUrl, videoUrl, download]);

  const handleGoBack = useCallback(() => {
    navigation.navigate('Search');
  }, [navigation]);

  const handleViewHistory = useCallback(() => {
    navigation.navigate('DownloadsTab');
  }, [navigation]);

  const renderContent = () => {
    if (isDownloading) {
      return (
        <View style={commonStyles.centerContent}>
          <LoadingSpinner text={t('download.downloading')} />
          <Text style={styles.downloadingTitle} numberOfLines={1}>
            {title}
          </Text>
          <Text style={commonStyles.statusText}>
            {t('download.pleaseWait')}
          </Text>
        </View>
      );
    }

    if (localFilePath) {
      return (
        <View style={commonStyles.centerContent}>
          <SuccessCheckmark visible={true} size={64} />
          <Text style={styles.successTitle}>{t('download.success')}</Text>
          <Text style={styles.fileSavedLabel}>{t('download.fileSaved')}</Text>
          <Text style={styles.filePath} testID="file-path">
            {localFilePath}
          </Text>
          <View style={styles.successButtons}>
            <View style={styles.buttonWrapper}>
              <Button
                title={t('download.goBack')}
                onPress={handleGoBack}
                variant="outline"
              />
            </View>
            <View style={styles.buttonWrapper}>
              <Button
                title={t('download.viewHistory')}
                onPress={handleViewHistory}
              />
            </View>
          </View>
        </View>
      );
    }

    if (error) {
      return (
        <View style={commonStyles.centerContent}>
          <ErrorMessage
            message={t('download.failed') + ': ' + error}
            onRetry={handleDownload}
          />
        </View>
      );
    }

    // Initial state — show download button
    return (
      <View style={styles.buttonWrapper}>
        <Button
          title={t('download.downloadMp3')}
          onPress={handleDownload}
        />
      </View>
    );
  };

  return (
    <ScrollView
      style={commonStyles.screenContainer}
      contentContainerStyle={styles.scrollContent}
      testID="download-screen"
    >
      {localFilePath && <FloatingParticles count={8} />}

      {imageUrl ? (
        <Image
          source={{ uri: imageUrl }}
          style={styles.thumbnail}
          testID="download-thumbnail"
          resizeMode="cover"
        />
      ) : (
        <View
          style={[styles.thumbnail, styles.placeholder]}
          testID="download-thumbnail-placeholder"
        />
      )}

      <View style={styles.infoContainer}>
        <Text style={styles.title}>{title}</Text>
        {description ? (
          <Text style={styles.description}>{description}</Text>
        ) : null}
      </View>

      {renderContent()}
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  scrollContent: {
    padding: spacing.lg,
  },
  thumbnail: {
    width: 120,
    height: 80,
    borderRadius: borderRadius.md,
    backgroundColor: colors.placeholder,
    alignSelf: 'center',
  },
  placeholder: {
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: colors.placeholderDark,
  },
  infoContainer: {
    marginTop: spacing.lg,
    marginBottom: spacing.xl,
    alignItems: 'center',
  },
  title: {
    ...typography.heading,
    textAlign: 'center',
    marginBottom: spacing.sm,
  },
  description: {
    ...typography.bodySmall,
    color: colors.textSecondary,
    textAlign: 'center',
    lineHeight: 20,
    paddingHorizontal: spacing.lg,
  },
  buttonWrapper: {
    marginTop: spacing.sm,
  },
  downloadingTitle: {
    ...typography.body,
    fontWeight: '600',
    color: colors.textPrimary,
    textAlign: 'center',
    maxWidth: '80%',
    marginTop: spacing.md,
  },
  successTitle: {
    ...typography.heading,
    color: colors.success,
    marginTop: spacing.lg,
    marginBottom: spacing.xs,
  },
  fileSavedLabel: {
    fontSize: 13,
    color: colors.textSecondary,
    marginBottom: spacing.xs,
  },
  filePath: {
    ...typography.monospace,
    color: colors.textTertiary,
    marginBottom: spacing.xl,
    paddingHorizontal: spacing.lg,
  },
  successButtons: {
    flexDirection: 'row',
    gap: spacing.md,
    paddingHorizontal: spacing.lg,
  },
  buttonWrapper: {
    flex: 1,
  },
});

export default DownloadScreen;
