import React, { useCallback, useRef, useState } from 'react';
import { FlatList, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import type { MainStackParamList } from '../navigation/types';
import type { SearchHistoryItem, VideoItem } from '../types/api';
import { getSearchHistory } from '../services/searchService';
import SearchBar from '../components/SearchBar';
import VideoCard from '../components/VideoCard';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';
import FloatingParticles from '../components/FloatingParticles';
import Logo from '../components/Logo';
import useKeepAwake from '../hooks/useKeepAwake';
import useSearch from '../hooks/useSearch';
import useScreenTitle from '../hooks/useScreenTitle';
import { RECENT_SEARCHES_LIMIT } from '../constants/app';
import {
  colors,
  commonStyles,
  spacing,
  borderRadius,
  typography,
} from '../styles/theme';

type SearchNavigationProp = NativeStackNavigationProp<
  MainStackParamList,
  'Search'
>;

const SearchResultsView: React.FC<{
  results: VideoItem[];
  renderVideoItem: ({ item }: { item: VideoItem }) => React.ReactElement;
  keyExtractor: (item: VideoItem) => string;
  onBack: () => void;
  backLabel: string;
}> = ({ results, renderVideoItem, keyExtractor, onBack, backLabel }) => (
  <View style={styles.resultsContainer}>
    <Text style={styles.backButton} onPress={onBack}>
      {'← ' + backLabel}
    </Text>
    <FlatList
      data={results}
      keyExtractor={keyExtractor}
      renderItem={renderVideoItem}
      contentContainerStyle={styles.listContent}
      keyboardShouldPersistTaps="handled"
    />
  </View>
);

const SearchRecentView: React.FC<{
  recentSearches: SearchHistoryItem[];
  onItemPress: (text: string) => void;
  onSearch: (text: string) => void;
  title: string;
}> = ({ recentSearches, onItemPress, title }) => (
  <View style={styles.recentContainer}>
    <Text style={styles.recentTitle}>{title}</Text>
    {recentSearches.map(item => (
      <Text
        key={item.id}
        style={styles.recentItem}
        onPress={() => onItemPress(item.searchText)}
      >
        {item.searchText}
      </Text>
    ))}
  </View>
);

const SearchInitialEmptyView: React.FC<{ emptyText: string }> = ({
  emptyText,
}) => (
  <View style={styles.initialEmptyContainer}>
    <FloatingParticles count={6} />
    <Text style={styles.emptyEmoji}>🎵</Text>
    <Text style={styles.emptyDescription}>{emptyText}</Text>
  </View>
);

const SearchScreen: React.FC = () => {
  const { t } = useTranslation();
  const navigation = useNavigation<SearchNavigationProp>();
  const { results, isLoading, error, search, clearResults } = useSearch();
  const lastQueryRef = useRef<string>('');

  useScreenTitle('search.title');

  // Keep screen awake while this screen is visible
  useKeepAwake(true);

  const [historyState, setHistoryState] = useState<{
    items: SearchHistoryItem[];
    loaded: boolean;
  }>(() => {
    getSearchHistory(RECENT_SEARCHES_LIMIT)
      .then(items => setHistoryState({ items, loaded: true }))
      .catch(() => setHistoryState({ items: [], loaded: true }));
    return { items: [], loaded: false };
  });
  const recentSearches = historyState.items;
  const historyLoading = !historyState.loaded;
  const [searchBarText, setSearchBarText] = useState('');

  const handleSearch = useCallback(
    (query: string) => {
      if (query.trim().length === 0) {
        clearResults();
        setSearchBarText('');
        lastQueryRef.current = '';
        return;
      }
      lastQueryRef.current = query.trim();
      search(query.trim());
    },
    [search, clearResults],
  );

  const handleVideoPress = useCallback(
    (video: VideoItem) => {
      navigation.navigate('Download', video);
    },
    [navigation],
  );

  const handleRetry = useCallback(() => {
    if (lastQueryRef.current) {
      search(lastQueryRef.current);
    }
  }, [search]);

  const handleBackToRecent = useCallback(() => {
    clearResults();
    setSearchBarText('');
    lastQueryRef.current = '';
  }, [clearResults]);

  const renderVideoItem = useCallback(
    ({ item }: { item: VideoItem }) => (
      <VideoCard video={item} onPress={handleVideoPress} />
    ),
    [handleVideoPress],
  );

  const keyExtractor = useCallback((item: VideoItem) => item.videoId, []);

  const showLogo =
    !isLoading && !error && results.length === 0 && lastQueryRef.current === '';

  const hasQuery = lastQueryRef.current !== '';

  return (
    <View style={commonStyles.screenContainer}>
      {showLogo && (
        <View style={styles.logoContainer}>
          <Logo />
        </View>
      )}
      <SearchBar
        onSearch={handleSearch}
        value={searchBarText}
        onChangeText={setSearchBarText}
      />
      <View style={styles.content}>
        {isLoading ? (
          <LoadingSpinner />
        ) : error ? (
          <ErrorMessage
            message={t('search.error') + ': ' + error}
            onRetry={handleRetry}
          />
        ) : hasQuery && results.length === 0 ? (
          <View style={styles.emptyState}>
            <Text style={commonStyles.emptyText}>{t('search.noResults')}</Text>
          </View>
        ) : results.length > 0 ? (
          <SearchResultsView
            results={results}
            renderVideoItem={renderVideoItem}
            keyExtractor={keyExtractor}
            onBack={handleBackToRecent}
            backLabel={t('search.backToRecent')}
          />
        ) : !historyLoading && recentSearches.length > 0 ? (
          <SearchRecentView
            recentSearches={recentSearches}
            onItemPress={text => {
              setSearchBarText(text);
              handleSearch(text);
            }}
            onSearch={text => {
              handleSearch(text);
            }}
            title={t('search.recentSearches')}
          />
        ) : (
          <SearchInitialEmptyView emptyText={t('search.empty')} />
        )}
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  content: {
    flex: 1,
  },
  resultsContainer: {
    flex: 1,
  },
  backButton: {
    ...typography.caption,
    color: colors.primary,
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.md,
    paddingBottom: spacing.sm,
  },
  logoContainer: {
    alignItems: 'center',
    paddingTop: spacing.lg,
    paddingBottom: spacing.md,
  },
  listContent: {
    paddingVertical: spacing.sm,
  },
  emptyState: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: spacing.xxl,
  },
  initialEmptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: spacing.xxl,
  },
  emptyEmoji: {
    fontSize: 64,
    opacity: 0.25,
    marginBottom: spacing.lg,
  },
  emptyDescription: {
    fontSize: 15,
    color: colors.textSecondary,
    textAlign: 'center',
    lineHeight: 22,
  },
  recentContainer: {
    padding: spacing.lg,
  },
  recentTitle: {
    fontSize: 14,
    fontWeight: '600',
    color: colors.textTertiary,
    marginBottom: spacing.md,
  },
  recentItem: {
    fontSize: 14,
    color: colors.primary,
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
    backgroundColor: colors.surface,
    borderRadius: borderRadius.md,
    marginBottom: 6,
    overflow: 'hidden',
  },
});

export default SearchScreen;
