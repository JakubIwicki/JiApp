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
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import type { RouteProp } from '@react-navigation/native';
import type { MainStackParamList, MainTabParamList } from '../navigation/types';
import Button from '../components/Button';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';
import SuccessCheckmark from '../components/SuccessCheckmark';
import FloatingParticles from '../components/FloatingParticles';
import AudioPreviewPlayer from '../components/AudioPreviewPlayer';
import useDownload from '../hooks/useDownload';
import useScreenTitle from '../hooks/useScreenTitle';
import { colors, commonStyles, spacing, typography, borderRadius } from '../styles/theme';

type DownloadNavigationProp = NativeStackNavigationProp<MainStackParamList & MainTabParamList, 'Download'>;
type DownloadRouteProp = RouteProp<MainStackParamList, 'Download'>;

const DownloadPendingView: React.FC<{
  videoId: string;
  onDownload: () => void;
  downloadLabel: string;
}> = ({ videoId, onDownload, downloadLabel }) => (
  <View style={styles.buttonWrapper}>
    <AudioPreviewPlayer videoId={videoId} />
    <Button title={downloadLabel} onPress={onDownload} />
  </View>
);

const DownloadingView: React.FC<{
  title: string;
  downloadingText: string;
  pleaseWaitText: string;
}> = ({ title, downloadingText, pleaseWaitText }) => (
  <View style={commonStyles.centerContent}>
    <LoadingSpinner text={downloadingText} />
    <Text style={styles.downloadingTitle} numberOfLines={1}>
      {title}
    </Text>
    <Text style={commonStyles.statusText}>{pleaseWaitText}</Text>
  </View>
);

const DownloadSuccessView: React.FC<{
  localFilePath: string;
  successLabel: string;
  fileSavedLabel: string;
  goBackLabel: string;
  viewHistoryLabel: string;
  openInPlayerLabel: string;
  onGoBack: () => void;
  onViewHistory: () => void;
  onPlay: () => void;
}> = ({
  localFilePath,
  successLabel,
  fileSavedLabel,
  goBackLabel,
  viewHistoryLabel,
  openInPlayerLabel,
  onGoBack,
  onViewHistory,
  onPlay,
}) => (
  <View style={commonStyles.centerContent}>
    <SuccessCheckmark size={64} />
    <Text style={styles.successTitle}>{successLabel}</Text>
    <Text style={styles.fileSavedLabel}>{fileSavedLabel}</Text>
    <Text style={styles.filePath} testID="file-path">
      {localFilePath}
    </Text>
    <View style={styles.successButtons}>
      <View style={styles.buttonRow}>
        <View style={styles.successButtonWrapper}>
          <Button title={goBackLabel} onPress={onGoBack} variant="outline" />
        </View>
        <View style={styles.successButtonWrapper}>
          <Button title={viewHistoryLabel} onPress={onViewHistory} />
        </View>
      </View>
      <Button title={openInPlayerLabel} onPress={onPlay} />
    </View>
  </View>
);

const DownloadErrorView: React.FC<{
  error: string;
  onRetry: () => void;
  failedLabel: string;
}> = ({ error, onRetry, failedLabel }) => (
  <View style={commonStyles.centerContent}>
    <ErrorMessage
      message={failedLabel + ': ' + error}
      onRetry={onRetry}
    />
  </View>
);

const DownloadScreen: React.FC = () => {
  const { t } = useTranslation();
  const navigation = useNavigation<DownloadNavigationProp>();
  const route = useRoute<DownloadRouteProp>();
  const { videoId, title, description, imageUrl, videoUrl } = route.params;

  useScreenTitle('download.title');

  const { isDownloading, error, localFilePath, download, playInMusicPlayer } = useDownload();

  const handleDownload = useCallback(() => {
    download({
      videoId,
      title,
      description,
      imageUrl,
      videoUrl,
      channelTitle: '',
    });
  }, [videoId, title, description, imageUrl, videoUrl, download]);

  const handleGoBack = useCallback(() => {
    navigation.navigate('Search');
  }, [navigation]);

  const handleViewHistory = useCallback(() => {
    navigation.navigate('DownloadsTab');
  }, [navigation]);

  const handlePlay = useCallback(() => {
    playInMusicPlayer(t('download.openWith'));
  }, [playInMusicPlayer, t]);

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

      {isDownloading ? (
        <DownloadingView
          title={title}
          downloadingText={t('download.downloading')}
          pleaseWaitText={t('download.pleaseWait')}
        />
      ) : localFilePath ? (
        <DownloadSuccessView
          localFilePath={localFilePath}
          successLabel={t('download.success')}
          fileSavedLabel={t('download.fileSaved')}
          goBackLabel={t('download.goBack')}
          viewHistoryLabel={t('download.viewHistory')}
          openInPlayerLabel={t('download.openInPlayer')}
          onGoBack={handleGoBack}
          onViewHistory={handleViewHistory}
          onPlay={handlePlay}
        />
      ) : error ? (
        <DownloadErrorView
          error={error}
          onRetry={handleDownload}
          failedLabel={t('download.failed')}
        />
      ) : (
        <DownloadPendingView
          videoId={videoId}
          onDownload={handleDownload}
          downloadLabel={t('download.downloadMp3')}
        />
      )}
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
    paddingHorizontal: spacing.lg,
    gap: spacing.md,
  },
  buttonRow: {
    flexDirection: 'row',
    gap: spacing.md,
  },
  successButtonWrapper: {
    flex: 1,
  },
});

export default DownloadScreen;
