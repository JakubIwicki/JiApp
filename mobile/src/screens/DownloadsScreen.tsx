import React, { useCallback, useState } from 'react';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Routes } from '../navigation/routes';
import type { MainStackParamList } from '../navigation/types';
import type { DownloadHistoryItem } from '../types/api';
import RefreshableScrollView from '../components/RefreshableScrollView';
import SearchBar from '../components/SearchBar';
import HistoryItem from '../components/HistoryItem';
import HistorySection from '../components/HistorySection';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';
import useDownloads from '../hooks/useDownloads';
import useKeepAwake from '../hooks/useKeepAwake';
import useScreenTitle from '../hooks/useScreenTitle';
import { useTheme } from '../context/ThemeContext';

type DownloadsNavigationProp = NativeStackNavigationProp<
  MainStackParamList,
  'Download'
>;

const DownloadsScreen: React.FC = () => {
  const { t } = useTranslation();
  const navigation = useNavigation<DownloadsNavigationProp>();
  useScreenTitle('nav.downloads');
  const { commonStyles } = useTheme();

  // Keep screen awake while this screen is visible
  useKeepAwake(true);

  const {
    downloads,
    isLoading,
    isRefreshing,
    error,
    loadDownloads,
    archiveDownload,
  } = useDownloads();
  const [filterQuery, setFilterQuery] = useState('');

  useFocusEffect(
    useCallback(() => {
      loadDownloads(false);
    }, [loadDownloads]),
  );

  const handleDownloadPress = useCallback(
    (item: DownloadHistoryItem) => {
      navigation.navigate(Routes.downloads.download, {
        videoId: item.videoId,
        title: item.videoTitle,
        description: item.videoDescription,
        imageUrl: item.imageUrl,
        videoUrl: item.videoUrl,
        channelTitle: '',
      });
    },
    [navigation],
  );

  const handleRetry = useCallback(() => {
    loadDownloads(false);
  }, [loadDownloads]);

  const handleRefresh = useCallback(() => {
    loadDownloads(true);
  }, [loadDownloads]);

  const handleArchive = useCallback(
    (item: DownloadHistoryItem) => {
      archiveDownload(item.id);
    },
    [archiveDownload],
  );

  const renderDownloadItem = useCallback(
    (item: DownloadHistoryItem) => (
      <HistoryItem
        type="download"
        item={item}
        onPress={handleDownloadPress}
        onArchive={() => handleArchive(item)}
      />
    ),
    [handleDownloadPress, handleArchive],
  );

  const downloadKeyExtractor = useCallback(
    (item: DownloadHistoryItem) => String(item.id),
    [],
  );

  const filteredDownloads = filterQuery
    ? downloads.filter(d =>
        d.videoTitle.toLowerCase().includes(filterQuery.toLowerCase()),
      )
    : downloads;

  if (isLoading) {
    return <LoadingSpinner />;
  }

  if (error && downloads.length === 0) {
    return (
      <ErrorMessage
        message={t('history.loadError') + ': ' + error}
        onRetry={handleRetry}
      />
    );
  }

  return (
    <RefreshableScrollView
      style={commonStyles.screenContainer}
      contentContainerStyle={commonStyles.scrollContent}
      refreshing={isRefreshing}
      onRefresh={handleRefresh}
      refreshTestID="downloads-refresh-control"
    >
      <SearchBar
        onSearch={setFilterQuery}
        placeholder={t('history.filterDownloads')}
      />
      <HistorySection
        title={t('history.downloads')}
        items={filteredDownloads}
        emptyText={t('history.noDownloads')}
        renderItem={renderDownloadItem}
        keyExtractor={downloadKeyExtractor}
      />
    </RefreshableScrollView>
  );
};

export default DownloadsScreen;
