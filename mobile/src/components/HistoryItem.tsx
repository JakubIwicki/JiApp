import React from 'react';
import { Animated, Image, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { Swipeable } from 'react-native-gesture-handler';
import { useTranslation } from 'react-i18next';
import type { SearchHistoryItem, DownloadHistoryItem } from '../types/api';
import { formatDate } from '../utils/dateUtils';
import { colors, spacing, borderRadius } from '../styles/theme';

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

const HistoryItem: React.FC<BaseProps> = ({ type, item, onPress, onArchive }) => {
  const { t } = useTranslation();

  const renderRightActions = (
    _progress: Animated.AnimatedInterpolation<number>,
    _dragX: Animated.AnimatedInterpolation<number>,
  ) => {
    if (!onArchive) {
      return null;
    }
    return (
      <View style={styles.archiveContainer}>
        <TouchableOpacity
          style={styles.archiveButton}
          onPress={onArchive}
          activeOpacity={0.7}
          accessibilityRole="button"
          accessibilityLabel={t('history.archiveAction')}
        >
          <Text style={styles.archiveButtonText}>{t('history.archiveAction')}</Text>
        </TouchableOpacity>
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
          <Text style={styles.date}>{formatDate((item as SearchHistoryItem).searchedAt)}</Text>
        </View>
      </View>
    ) : (
      (() => {
        const downloadItem = item as DownloadHistoryItem;
        return (
          <TouchableOpacity
            style={styles.container}
            onPress={() => onPress?.(downloadItem)}
            activeOpacity={0.7}
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
              <Text style={styles.date}>{formatDate(downloadItem.downloadedAt)}</Text>
            </View>
          </TouchableOpacity>
        );
      })()
    );

  if (onArchive) {
    return <Swipeable renderRightActions={renderRightActions}>{content}</Swipeable>;
  }

  return content;
};

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: colors.surface,
    borderRadius: borderRadius.lg,
    marginHorizontal: spacing.lg,
    marginVertical: spacing.xs,
    padding: 10,
    shadowColor: colors.cardShadow,
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.08,
    shadowRadius: 2,
    elevation: 1,
  },
  iconContainer: {
    width: 40,
    height: 40,
    borderRadius: spacing.sm,
    backgroundColor: colors.primaryLight,
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
    backgroundColor: colors.primaryLight,
    marginRight: 10,
  },
  placeholder: {
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: colors.primaryLight,
  },
  content: {
    flex: 1,
    justifyContent: 'center',
  },
  title: {
    fontSize: 14,
    fontWeight: '600',
    color: colors.textPrimary,
    marginBottom: 2,
  },
  date: {
    fontSize: 12,
    color: colors.textTertiary,
  },
  archiveContainer: {
    justifyContent: 'center',
    marginLeft: spacing.xs,
  },
  archiveButton: {
    backgroundColor: colors.primary,
    justifyContent: 'center',
    alignItems: 'center',
    width: 64,
    height: '100%',
    borderTopRightRadius: borderRadius.lg,
    borderBottomRightRadius: borderRadius.lg,
  },
  archiveButtonText: {
    color: colors.textInverse,
    fontSize: 12,
    fontWeight: '600',
  },
});

export default HistoryItem;
