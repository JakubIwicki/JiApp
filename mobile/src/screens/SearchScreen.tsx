import React, { useCallback, useEffect, useRef, useState } from 'react';
import {
  FlatList,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import type { StackNavigationProp } from '@react-navigation/stack';
import type { MainStackParamList } from '../navigation/types';
import type { SearchHistoryItem, VideoItem } from '../types/api';
import { getSearchHistory } from '../services/searchService';
import SearchBar from '../components/SearchBar';
import VideoCard from '../components/VideoCard';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';
import FloatingParticles from '../components/FloatingParticles';
import Logo from '../components/Logo';
import useSearch from '../hooks/useSearch';
import useScreenTitle from '../hooks/useScreenTitle';
import { RECENT_SEARCHES_LIMIT } from '../constants/app';
import { colors, commonStyles, spacing, borderRadius, typography } from '../styles/theme';

type SearchNavigationProp = StackNavigationProp<MainStackParamList, 'Search'>;

const SearchScreen: React.FC = () => {
  const { t } = useTranslation();
  const navigation = useNavigation<SearchNavigationProp>();
  const { results, isLoading, error, search, clearResults } = useSearch();
  const lastQueryRef = useRef<string>('');

  useScreenTitle('search.title');

  const [recentSearches, setRecentSearches] = useState<SearchHistoryItem[]>([]);
  const [historyLoading, setHistoryLoading] = useState(true);
  const [searchBarText, setSearchBarText] = useState('');

  useEffect(() => {
    const loadHistory = async () => {
      try {
        const items = await getSearchHistory(RECENT_SEARCHES_LIMIT);
        setRecentSearches(items);
      } catch {
        // Silently fail — history is a nice-to-have
        setRecentSearches([]);
      } finally {
        setHistoryLoading(false);
      }
    };
    loadHistory();
  }, []);

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

  const renderContent = () => {
    if (isLoading) {
      return <LoadingSpinner />;
    }

    if (error) {
      return (
        <ErrorMessage
          message={t('search.error') + ': ' + error}
          onRetry={handleRetry}
        />
      );
    }

    if (lastQueryRef.current !== '' && results.length === 0) {
      return (
        <View style={styles.emptyState}>
          <Text style={commonStyles.emptyText}>{t('search.noResults')}</Text>
        </View>
      );
    }

    if (results.length > 0) {
      return (
        <View style={styles.resultsContainer}>
          <Text
            style={styles.backButton}
            onPress={handleBackToRecent}
          >
            {'← ' + t('search.backToRecent')}
          </Text>
          <FlatList
            data={results}
            keyExtractor={(item) => item.videoId}
            renderItem={({ item }) => (
              <VideoCard video={item} onPress={handleVideoPress} />
            )}
            contentContainerStyle={styles.listContent}
            keyboardShouldPersistTaps="handled"
          />
        </View>
      );
    }

    // Initial state — show recent searches
    if (!historyLoading && recentSearches.length > 0) {
      return (
        <View style={styles.recentContainer}>
          <Text style={styles.recentTitle}>
            {t('search.recentSearches')}
          </Text>
          {recentSearches.map((item) => (
            <Text
              key={item.id}
              style={styles.recentItem}
              onPress={() => {
                setSearchBarText(item.searchText);
                handleSearch(item.searchText);
              }}
            >
              {item.searchText}
            </Text>
          ))}
        </View>
      );
    }

    // Wabi-sabi empty state — no recent searches, no query
    return (
      <View style={styles.initialEmptyContainer}>
        <FloatingParticles count={6} />
        <Text style={styles.emptyEmoji}>🎵</Text>
        <Text style={styles.emptyDescription}>{t('search.empty')}</Text>
      </View>
    );
  };

  const showLogo = !isLoading && !error && results.length === 0 && lastQueryRef.current === '';

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
      <View style={styles.content}>{renderContent()}</View>
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
