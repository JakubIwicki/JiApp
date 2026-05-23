import React from 'react';
import { Image, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
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
}

const HistoryItem: React.FC<BaseProps> = ({ type, item, onPress }) => {
  if (type === 'search') {
    const searchItem = item as SearchHistoryItem;
    return (
      <View style={styles.container} testID="history-item-search">
        <View style={styles.iconContainer}>
          <Text style={styles.searchIcon}>🔍</Text>
        </View>
        <View style={styles.content}>
          <Text style={styles.title} numberOfLines={1}>
            {searchItem.searchText}
          </Text>
          <Text style={styles.date}>{formatDate(searchItem.searchedAt)}</Text>
        </View>
      </View>
    );
  }

  const downloadItem = item as DownloadHistoryItem;

  const handlePress = () => {
    onPress?.(downloadItem);
  };

  return (
    <TouchableOpacity
      style={styles.container}
      onPress={handlePress}
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
});

export default HistoryItem;
