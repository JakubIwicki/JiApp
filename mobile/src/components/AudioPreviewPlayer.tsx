import React, { useEffect } from 'react';
import {
  ActivityIndicator,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from 'react-native';
import { useTranslation } from 'react-i18next';
import usePreview from '../hooks/usePreview';
import { colors, spacing, borderRadius } from '../styles/theme';

interface AudioPreviewPlayerProps {
  videoId: string;
}

const AudioPreviewPlayer = ({ videoId }: AudioPreviewPlayerProps) => {
  const { t } = useTranslation();
  const {
    isPlaying,
    isLoading,
    progress,
    elapsed,
    error,
    play,
    stop,
  } = usePreview();

  // Clean up when videoId changes or component unmounts
  useEffect(() => {
    return () => {
      stop();
    };
  }, [stop, videoId]);

  const handlePress = () => {
    if (isPlaying) {
      stop();
    } else {
      play(videoId);
    }
  };

  const remaining = Math.max(0, Math.ceil(10 - elapsed));
  const remainingFormatted = String(remaining).padStart(2, '0');

  const showProgressBar = isPlaying || progress > 0;

  return (
    <View style={styles.container}>
      <View style={styles.controls}>
        <TouchableOpacity
          style={styles.playButton}
          onPress={handlePress}
          testID="preview-play-button"
          accessibilityRole="button"
          accessibilityLabel={
            isPlaying ? t('common.stop') : t('preview.tapToListen')
          }
          activeOpacity={0.7}
        >
          {isLoading ? (
            <ActivityIndicator
              size="small"
              color={colors.textInverse}
            />
          ) : isPlaying ? (
            <Text style={styles.playIcon}>&#9208;</Text>
          ) : (
            <Text style={styles.playIcon}>&#9654;</Text>
          )}
        </TouchableOpacity>

        <View style={styles.labelContainer}>
          {isLoading ? (
            <Text style={styles.label}>{t('preview.loading')}</Text>
          ) : isPlaying ? (
            <>
              <Text style={styles.label}>{t('preview.playing')}</Text>
              <Text style={styles.counter} testID="preview-counter">
                {`0:${remainingFormatted}s / ${t('preview.duration')}`}
              </Text>
            </>
          ) : (
            <>
              <Text style={styles.label}>{t('preview.tapToListen')}</Text>
              <Text style={styles.duration}>{t('preview.duration')}</Text>
            </>
          )}
        </View>
      </View>

      {showProgressBar && (
        <View style={styles.progressTrack} testID="preview-progress-bar">
          <View
            style={[
              styles.progressFill,
              { width: `${Math.round(progress * 100)}%` },
            ]}
          />
        </View>
      )}

      {error && (
        <Text style={styles.errorText} testID="preview-error">
          {error}
        </Text>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    backgroundColor: colors.surface,
    borderRadius: borderRadius.md,
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.lg,
  },
  controls: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
  },
  playButton: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: colors.primary,
    justifyContent: 'center',
    alignItems: 'center',
  },
  playIcon: {
    fontSize: 16,
    color: colors.textInverse,
  },
  labelContainer: {
    flex: 1,
    justifyContent: 'center',
  },
  label: {
    fontSize: 14,
    fontWeight: '600',
    color: colors.textPrimary,
  },
  duration: {
    fontSize: 12,
    color: colors.textSecondary,
    marginTop: 2,
  },
  counter: {
    fontSize: 12,
    color: colors.textSecondary,
    marginTop: 2,
    fontVariant: ['tabular-nums'],
  },
  progressTrack: {
    height: 3,
    backgroundColor: colors.primaryLight,
    borderRadius: 2,
    marginTop: spacing.md,
    overflow: 'hidden',
  },
  progressFill: {
    height: '100%',
    backgroundColor: colors.primary,
    borderRadius: 2,
  },
  errorText: {
    color: colors.error,
    fontSize: 13,
    marginTop: spacing.sm,
    textAlign: 'center',
  },
});

export default AudioPreviewPlayer;
