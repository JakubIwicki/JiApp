import React, { useCallback, useReducer, useState } from 'react';
import {
  ScrollView,
} from 'react-native';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import type { MainStackParamList } from '../navigation/types';
import type { DownloadHistoryItem } from '../types/api';
import { getDownloadHistory, archiveDownload } from '../services/downloadService';
import RefreshableScrollView from '../components/RefreshableScrollView';
import SearchBar from '../components/SearchBar';
import HistoryItem from '../components/HistoryItem';
import HistorySection from '../components/HistorySection';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';
import useScreenTitle from '../hooks/useScreenTitle';
import useToast from '../hooks/useToast';
import { colors, commonStyles } from '../styles/theme';

type DownloadsNavigationProp = NativeStackNavigationProp<MainStackParamList, 'Download'>;

interface DownloadsState {
  downloads: DownloadHistoryItem[];
  isLoading: boolean;
  isRefreshing: boolean;
  error: string | null;
}

type DownloadsAction =
  | { type: 'FETCH_START'; pull: boolean }
  | { type: 'FETCH_SUCCESS'; downloads: DownloadHistoryItem[] }
  | { type: 'FETCH_ERROR'; error: string }
  | { type: 'REMOVE_DOWNLOAD'; id: number };

function downloadsReducer(state: DownloadsState, action: DownloadsAction): DownloadsState {
  switch (action.type) {
    case 'FETCH_START':
      return {
        ...state,
        error: null,
        isLoading: action.pull ? state.isLoading : true,
        isRefreshing: action.pull ? true : false,
      };
    case 'FETCH_SUCCESS':
      return { ...state, downloads: action.downloads, isLoading: false, isRefreshing: false, error: null };
    case 'FETCH_ERROR':
      return { ...state, error: action.error, isLoading: false, isRefreshing: false };
    case 'REMOVE_DOWNLOAD':
      return { ...state, downloads: state.downloads.filter((d) => d.id !== action.id) };
    default:
      return state;
  }
}

const initialDownloadsState: DownloadsState = {
  downloads: [],
  isLoading: true,
  isRefreshing: false,
  error: null,
};

const DownloadsScreen: React.FC = () => {
  const { t } = useTranslation();
  const navigation = useNavigation<DownloadsNavigationProp>();
  useScreenTitle('nav.downloads');
  const { showSuccess, showError } = useToast();

  const [state, dispatch] = useReducer(downloadsReducer, initialDownloadsState);
  const [filterQuery, setFilterQuery] = useState('');

  const loadDownloads = useCallback(async (pull: boolean) => {
    dispatch({ type: 'FETCH_START', pull });
    try {
      const items = await getDownloadHistory();
      dispatch({ type: 'FETCH_SUCCESS', downloads: items });
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : String(err);
      dispatch({ type: 'FETCH_ERROR', error: message });
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
      dispatch({ type: 'REMOVE_DOWNLOAD', id: item.id });
      archiveDownload(item.id)
        .then(() => showSuccess('toast.downloadArchived'))
        .catch(() => {
          showError('toast.archiveFailed');
          loadDownloads(false);
        });
    },
    [loadDownloads, showSuccess, showError],
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
    ? state.downloads.filter((d) =>
        d.videoTitle.toLowerCase().includes(filterQuery.toLowerCase()),
      )
    : state.downloads;

  if (state.isLoading) {
    return <LoadingSpinner />;
  }

  if (state.error && state.downloads.length === 0) {
    return (
      <ErrorMessage
        message={t('history.loadError') + ': ' + state.error}
        onRetry={handleRetry}
      />
    );
  }

  return (
    <RefreshableScrollView
      style={commonStyles.screenContainer}
      contentContainerStyle={commonStyles.scrollContent}
      refreshing={state.isRefreshing}
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
