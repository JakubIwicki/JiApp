import React, { useCallback, useState } from 'react';
import {
  RefreshControl,
  ScrollView,
} from 'react-native';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import type { StackNavigationProp } from '@react-navigation/stack';
import type { MainStackParamList } from '../navigation/types';
import type { DownloadHistoryItem } from '../types/api';
import { getDownloadHistory, archiveDownload } from '../services/downloadService';
import SearchBar from '../components/SearchBar';
import HistoryItem from '../components/HistoryItem';
import HistorySection from '../components/HistorySection';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';
import useScreenTitle from '../hooks/useScreenTitle';
import useToast from '../hooks/useToast';
import { colors, commonStyles } from '../styles/theme';

type DownloadsNavigationProp = StackNavigationProp<MainStackParamList, 'Download'>;

const DownloadsScreen: React.FC = () => {
  const { t } = useTranslation();
  const navigation = useNavigation<DownloadsNavigationProp>();
  useScreenTitle('nav.downloads');
  const { showSuccess, showError } = useToast();

  const [downloads, setDownloads] = useState<DownloadHistoryItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [filterQuery, setFilterQuery] = useState('');

  const loadDownloads = useCallback(async (pull: boolean) => {
    try {
      if (pull) {
        setIsRefreshing(true);
      } else {
        setIsLoading(true);
      }
      setError(null);

      const items = await getDownloadHistory();
      setDownloads(items);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : String(err);
      setError(message);
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      loadDownloads(false);
    }, [loadDownloads]),
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
    loadDownloads(false);
  }, [loadDownloads]);

  const handleRefresh = useCallback(() => {
    loadDownloads(true);
  }, [loadDownloads]);

  const handleArchive = useCallback(
    (item: DownloadHistoryItem) => {
      setDownloads((prev) => prev.filter((d) => d.id !== item.id));
      archiveDownload(item.id)
        .then(() => showSuccess('toast.downloadArchived'))
        .catch(() => {
          showError('toast.archiveFailed');
          loadDownloads(false);
        });
    },
    [loadDownloads],
  );

  const filteredDownloads = filterQuery
    ? downloads.filter((d) =>
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
    <ScrollView
      style={commonStyles.screenContainer}
      contentContainerStyle={commonStyles.scrollContent}
      refreshControl={
        <RefreshControl
          refreshing={isRefreshing}
          onRefresh={handleRefresh}
          testID="downloads-refresh-control"
          tintColor={colors.primary}
        />
      }
    >
      <SearchBar
        onSearch={setFilterQuery}
        placeholder={t('history.filterDownloads')}
      />
      <HistorySection
        title={t('history.downloads')}
        items={filteredDownloads}
        emptyText={t('history.noDownloads')}
        renderItem={(item: DownloadHistoryItem) => (
          <HistoryItem
            type="download"
            item={item}
            onPress={handleDownloadPress}
            onArchive={() => handleArchive(item)}
          />
        )}
        keyExtractor={(item: DownloadHistoryItem) =>
          String(item.id)
        }
      />
    </ScrollView>
  );
};

export default DownloadsScreen;
