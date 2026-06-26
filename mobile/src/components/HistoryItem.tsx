import React from 'react';
import { Image, Pressable, StyleSheet, Text, View } from 'react-native';
import { Swipeable } from 'react-native-gesture-handler';
import { useTranslation } from 'react-i18next';
import type { SearchHistoryItem, DownloadHistoryItem } from '../types/api';
import { formatDate } from '../utils/dateUtils';
import { useThemedStyles } from '../context/ThemeContext';
import type { Theme } from '../styles/theme';
import { spacing, borderRadius } from '../styles/theme';

type AnimatedInterpolation<T extends string | number> =
  import('react-native').Animated.AnimatedInterpolation<T>;

interface BaseProps {
  /** Discriminator to determine which data type to render. */
  type: 'search' | 'download';
  /** The history item data — either a search or download record. */
  item: SearchHistoryItem | DownloadHistoryItem;
  /** Callback fired on press for download type items. */
  onPress?: (item: DownloadHistoryItem) => void;
  /** Callback fired when the item is archived via swipe action. */
  onArchive?: () => void;
}

const HistoryItem: React.FC<BaseProps> = ({
  type,
  item,
  onPress,
  onArchive,
}) => {
  const { t } = useTranslation();
  const styles = useThemedStyles(makeStyles);

  const renderRightActions = (
    _progress: AnimatedInterpolation<number>,
    _dragX: AnimatedInterpolation<number>,
  ) => {
    if (!onArchive) {
      return null;
    }
    return (
      <View style={styles.archiveContainer}>
        <Pressable
          style={({ pressed }) => [
            styles.archiveButton,
            pressed && { opacity: 0.7 },
          ]}
          onPress={onArchive}
          accessibilityRole="button"
          accessibilityLabel={t('history.archiveAction')}
        >
          <Text style={styles.archiveButtonText}>
            {t('history.archiveAction')}
          </Text>
        </Pressable>
      </View>
    );
  };

  const content =
    type === 'search' ? (
      <View style={styles.container} testID="history-item-search">
        <View style={styles.iconContainer}>
          <Text style={styles.searchIcon}>🔍</Text>
        </View>
        <View style={styles.content}>
          <Text style={styles.title} numberOfLines={1}>
            {(item as SearchHistoryItem).searchText}
          </Text>
          <Text style={styles.date}>
            {formatDate((item as SearchHistoryItem).searchedAt)}
          </Text>
        </View>
      </View>
    ) : (
      (() => {
        const downloadItem = item as DownloadHistoryItem;
        return (
          <Pressable
            style={({ pressed }) => [
              styles.container,
              pressed && { opacity: 0.7 },
            ]}
            onPress={() => onPress?.(downloadItem)}
            testID="history-item-download"
            accessibilityRole="button"
          >
            {downloadItem.imageUrl ? (
              <Image
                source={{ uri: downloadItem.imageUrl }}
                style={styles.thumbnail}
                testID="history-thumbnail"
                resizeMode="cover"
              />
            ) : (
              <View
                style={[styles.thumbnail, styles.placeholder]}
                testID="history-thumbnail-placeholder"
              />
            )}
            <View style={styles.content}>
              <Text style={styles.title} numberOfLines={1}>
                {downloadItem.videoTitle}
              </Text>
              <Text style={styles.date}>
                {formatDate(downloadItem.downloadedAt)}
              </Text>
            </View>
          </Pressable>
        );
      })()
    );

  if (onArchive) {
    return (
      <Swipeable renderRightActions={renderRightActions}>{content}</Swipeable>
    );
  }

  return content;
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      flexDirection: 'row',
      alignItems: 'center',
      backgroundColor: t.colors.surface,
      borderRadius: borderRadius.lg,
      marginHorizontal: spacing.lg,
      marginVertical: spacing.xs,
      padding: 10,
      boxShadow: '0 1px 2px rgba(43,33,24,0.08)',
    },
    iconContainer: {
      width: 40,
      height: 40,
      borderRadius: spacing.sm,
      backgroundColor: t.colors.primaryLight,
      justifyContent: 'center',
      alignItems: 'center',
      marginRight: 10,
    },
    searchIcon: {
      fontSize: 18,
    },
    thumbnail: {
      width: 40,
      height: 40,
      borderRadius: spacing.sm,
      backgroundColor: t.colors.primaryLight,
      marginRight: 10,
    },
    placeholder: {
      justifyContent: 'center',
      alignItems: 'center',
      backgroundColor: t.colors.primaryLight,
    },
    content: {
      flex: 1,
      justifyContent: 'center',
    },
    title: {
      fontSize: 14,
      fontWeight: '600',
      color: t.colors.textPrimary,
      marginBottom: 2,
    },
    date: {
      fontSize: 12,
      color: t.colors.textTertiary,
    },
    archiveContainer: {
      justifyContent: 'center',
      marginLeft: spacing.xs,
    },
    archiveButton: {
      backgroundColor: t.colors.primary,
      justifyContent: 'center',
      alignItems: 'center',
      width: 64,
      height: '100%',
      borderTopRightRadius: borderRadius.lg,
      borderBottomRightRadius: borderRadius.lg,
    },
    archiveButtonText: {
      color: t.colors.textInverse,
      fontSize: 12,
      fontWeight: '600',
    },
  });

export default HistoryItem;
