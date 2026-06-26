import React, { useEffect } from 'react';
import {
  ActivityIndicator,
  Pressable,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { useTranslation } from 'react-i18next';
import usePreview from '../hooks/usePreview';
import useKeepAwake from '../hooks/useKeepAwake';
import { useTheme, useThemedStyles } from '../context/ThemeContext';
import type { Theme } from '../styles/theme';
import { spacing, borderRadius } from '../styles/theme';

interface AudioPreviewPlayerProps {
  videoId: string;
}

const AudioPreviewPlayer = ({ videoId }: AudioPreviewPlayerProps) => {
  const { t } = useTranslation();
  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);
  const { isPlaying, isLoading, progress, elapsed, error, play, stop } =
    usePreview();

  // Distinguish buffering (playback confirmed but no audio yet) from actual playing
  const isBuffering = isPlaying && elapsed === 0;

  // Keep screen awake during preview playback
  useKeepAwake(isPlaying || isLoading);

  // Clean up when videoId changes or component unmounts
  useEffect(() => {
    return () => {
      stop();
    };
  }, [stop, videoId]);

  const handlePress = () => {
    if (isLoading) return;
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
        <Pressable
          style={({ pressed }) => [
            styles.playButton,
            isLoading && { opacity: 0.5 },
            pressed && !isLoading && { opacity: 0.7 },
          ]}
          onPress={handlePress}
          testID="preview-play-button"
          accessibilityRole="button"
          accessibilityLabel={
            isPlaying ? t('common.stop') : t('preview.tapToListen')
          }
        >
          {isLoading ? (
            <ActivityIndicator size="small" color={colors.textInverse} />
          ) : isPlaying ? (
            <Text style={styles.playIcon}>&#9208;</Text>
          ) : (
            <Text style={styles.playIcon}>&#9654;</Text>
          )}
        </Pressable>

        <View style={styles.labelContainer}>
          {isLoading ? (
            <Text style={styles.label}>{t('preview.loading')}</Text>
          ) : isBuffering ? (
            <>
              <Text style={styles.label}>{t('preview.buffering')}</Text>
              <Text style={styles.duration}>{t('preview.duration')}</Text>
            </>
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

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      backgroundColor: t.colors.surface,
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
      backgroundColor: t.colors.primary,
      justifyContent: 'center',
      alignItems: 'center',
    },
    playIcon: {
      fontSize: 16,
      color: t.colors.textInverse,
    },
    labelContainer: {
      flex: 1,
      justifyContent: 'center',
    },
    label: {
      fontSize: 14,
      fontWeight: '600',
      color: t.colors.textPrimary,
    },
    duration: {
      fontSize: 12,
      color: t.colors.textSecondary,
      marginTop: 2,
    },
    counter: {
      fontSize: 12,
      color: t.colors.textSecondary,
      marginTop: 2,
      fontVariant: ['tabular-nums'],
    },
    progressTrack: {
      height: 3,
      backgroundColor: t.colors.primaryLight,
      borderRadius: 2,
      marginTop: spacing.md,
      overflow: 'hidden',
    },
    progressFill: {
      height: '100%',
      backgroundColor: t.colors.primary,
      borderRadius: 2,
    },
    errorText: {
      color: t.colors.error,
      fontSize: 13,
      marginTop: spacing.sm,
      textAlign: 'center',
    },
  });

export default AudioPreviewPlayer;
