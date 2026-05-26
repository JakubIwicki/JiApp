import React, { useCallback, useState } from 'react';
import {
  RefreshControl,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import type { StackNavigationProp } from '@react-navigation/stack';
import type { MainStackParamList } from '../navigation/types';
import type { SearchHistoryItem, DownloadHistoryItem } from '../types/api';
import SearchBar from '../components/SearchBar';
import HistoryItem from '../components/HistoryItem';
import HistorySection from '../components/HistorySection';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';
import useHistory from '../hooks/useHistory';
import useScreenTitle from '../hooks/useScreenTitle';
import { HISTORY_PAGE_SIZE } from '../constants/app';
import { colors, commonStyles, spacing } from '../styles/theme';

type HistoryNavigationProp = StackNavigationProp<MainStackParamList, 'History'>;

const HistoryScreen: React.FC = () => {
  const { t } = useTranslation();
  const navigation = useNavigation<HistoryNavigationProp>();
  useScreenTitle('history.title');
  const [filterQuery, setFilterQuery] = useState('');

  const { searches, downloads, isLoading, error, loadHistory, refresh, archiveSearch, archiveDownload } =
    useHistory();

  useFocusEffect(
    useCallback(() => {
      loadHistory(HISTORY_PAGE_SIZE);
    }, [loadHistory]),
  );

  const handleDownloadPress = useCallback(
    (item: DownloadHistoryItem) => {
      navigation.navigate('Download', {
        videoId: item.videoId,
        title: item.videoTitle,
        description: item.videoDescription,
        imageUrl: item.imageUrl,
        videoUrl: item.videoUrl,
      });
    },
    [navigation],
  );

  const handleRetry = useCallback(() => {
    loadHistory(HISTORY_PAGE_SIZE);
  }, [loadHistory]);

  const filteredSearches = filterQuery
    ? searches.filter((s) =>
        s.searchText.toLowerCase().includes(filterQuery.toLowerCase()),
      )
    : searches;
  const filteredDownloads = filterQuery
    ? downloads.filter((d) =>
        d.videoTitle.toLowerCase().includes(filterQuery.toLowerCase()),
      )
    : downloads;

  if (isLoading && searches.length === 0 && downloads.length === 0) {
    return <LoadingSpinner />;
  }

  if (error && searches.length === 0 && downloads.length === 0) {
    return (
      <ErrorMessage
        message={t('history.loadError') + ': ' + error}
        onRetry={handleRetry}
      />
    );
  }

  return (
    <ScrollView
      style={commonStyles.screenContainer}
      contentContainerStyle={commonStyles.scrollContent}
      refreshControl={
        <RefreshControl
          refreshing={isLoading}
          onRefresh={refresh}
          testID="history-refresh-control"
          tintColor={colors.primary}
        />
      }
    >
      {error && (searches.length > 0 || downloads.length > 0) ? (
        <View style={styles.errorBanner}>
          <Text style={styles.errorBannerText}>
            {t('history.loadError')}: {error}
          </Text>
        </View>
      ) : null}

      <SearchBar
        onSearch={setFilterQuery}
        placeholder={t('history.filterSearches')}
      />

      <HistorySection
        title={t('history.searches')}
        items={filteredSearches}
        emptyText={t('history.noSearches')}
        renderItem={(item: SearchHistoryItem) => (
          <HistoryItem
            type="search"
            item={item}
            onArchive={() => archiveSearch(item.id)}
          />
        )}
        keyExtractor={(item: SearchHistoryItem) =>
          String(item.id)
        }
      />

      <View style={styles.separator} />

      <HistorySection
        title={t('history.downloads')}
        items={filteredDownloads}
        emptyText={t('history.noDownloads')}
        renderItem={(item: DownloadHistoryItem) => (
          <HistoryItem
            type="download"
            item={item}
            onPress={handleDownloadPress}
            onArchive={() => archiveDownload(item.id)}
          />
        )}
        keyExtractor={(item: DownloadHistoryItem) =>
          String(item.id)
        }
      />
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  separator: {
    height: 1,
    backgroundColor: colors.separator,
    marginHorizontal: spacing.lg,
    marginVertical: spacing.sm,
  },
  errorBanner: {
    marginHorizontal: spacing.lg,
    marginBottom: spacing.md,
    padding: spacing.sm,
    backgroundColor: colors.errorLight,
    borderRadius: 8,
  },
  errorBannerText: {
    fontSize: 13,
    color: colors.error,
    textAlign: 'center',
  },
});

export default HistoryScreen;
