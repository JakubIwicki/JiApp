import React, {
  useCallback,
  useMemo,
  useState,
  useRef,
  useEffect,
} from 'react';
import {
  ActivityIndicator,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import useBoard from '../hooks/useBoard';
import useAuth from '../../../hooks/useAuth';
import { useTheme, useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius, zIndexScale } from '../../../styles/theme';
import type { LovingBoardsStackParamList } from '../../../navigation/types';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { Item, BoardItemStatus } from '../types/api';
import type { CategoryTint } from '../components/CategoryCard';
import ProgressStrip from '../components/ProgressStrip';
import CategoryCard from '../components/CategoryCard';
import ItemRow from '../components/ItemRow';
import Snackbar from '../components/Snackbar';
import EmptyState from '../components/EmptyState';

type Props = NativeStackScreenProps<LovingBoardsStackParamList, 'BoardDetail'>;
type NavigationProp = Props['navigation'];

const MIN_TOUCH = 44;

// ── Category metadata ──────────────────────────────────────────────────────────

const CATEGORY_EMOJI: Record<string, string> = {
  dairy: '🥛',
  vegetables: '🥬',
  bakery: '🍞',
  meat: '🥩',
  fruits: '🍎',
  frozen: '❄️',
  beverages: '🥤',
  household: '🏠',
  cleaning: '🧹',
  personal: '🧴',
  snacks: '🍿',
  canned: '🥫',
  spices: '🌿',
  grains: '🌾',
};

const CATEGORY_TINTS: Record<string, CategoryTint> = {
  dairy: 'info',
  vegetables: 'success',
  bakery: 'warning',
};

function getCategoryEmoji(category: string): string {
  const key = category.toLowerCase().trim();
  return CATEGORY_EMOJI[key] ?? '📦';
}

function getCategoryTint(category: string): CategoryTint {
  const key = category.toLowerCase().trim();
  return CATEGORY_TINTS[key] ?? 'primary';
}

// ── Screen ─────────────────────────────────────────────────────────────────────

const BoardDetailScreen: React.FC<Props> = ({ route }) => {
  const { boardId } = route.params;
  const { t } = useTranslation();
  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);
  const navigation = useNavigation<NavigationProp>();
  const { userId } = useAuth();
  const {
    board,
    items,
    isLoading,
    error,
    refetch,
    setItemStatus,
    clearCompleted,
    resetWeekly,
  } = useBoard(boardId);

  const [collapsedCategories, setCollapsedCategories] = useState<Set<string>>(
    new Set(),
  );
  const [doneExpanded, setDoneExpanded] = useState(false);
  const [clearing, setClearing] = useState(false);

  // ── Undo snackbar state ────────────────────────────────────────────────────
  interface UndoState {
    itemId: number;
    previousStatus: BoardItemStatus;
  }
  const [undoState, setUndoState] = useState<UndoState | null>(null);
  const undoTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // ── Cleared snackbar ───────────────────────────────────────────────────────
  const [clearedMessage, setClearedMessage] = useState<string | null>(null);
  const clearTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    const timer = undoTimerRef.current;
    const clearTimer = clearTimerRef.current;
    return () => {
      if (timer) clearTimeout(timer);
      if (clearTimer) clearTimeout(clearTimer);
    };
  }, [undoTimerRef, clearTimerRef]);

  // ── Group items by category ────────────────────────────────────────────────
  const { activeByCategory, completedItems, uncategorizedActive } =
    useMemo(() => {
      const active: Item[] = [];
      const completed: Item[] = [];
      for (const item of items) {
        if (item.status === 'Completed') completed.push(item);
        else if (item.status === 'Needed') active.push(item);
      }

      const grouped = new Map<string, Item[]>();
      const uncategorized: Item[] = [];

      for (const item of active) {
        const cat = item.category?.trim() || null;
        if (cat) {
          const list = grouped.get(cat) ?? [];
          list.push(item);
          grouped.set(cat, list);
        } else {
          uncategorized.push(item);
        }
      }

      return {
        activeByCategory: grouped,
        completedItems: completed,
        uncategorizedActive: uncategorized,
      };
    }, [items]);

  const categoryNames = useMemo(() => {
    const names = Array.from(activeByCategory.keys()).sort();
    if (uncategorizedActive.length > 0) names.push('__uncategorized__');
    return names;
  }, [activeByCategory, uncategorizedActive]);

  const allNeeded = useMemo(
    () => items.filter(i => i.status === 'Needed'),
    [items],
  );
  const completedCount = completedItems.length;
  const progressDone = completedCount;
  const progressTotal = allNeeded.length + completedCount;

  // ── Callbacks ──────────────────────────────────────────────────────────────

  const toggleCategory = useCallback((cat: string) => {
    setCollapsedCategories(prev => {
      const next = new Set(prev);
      if (next.has(cat)) next.delete(cat);
      else next.add(cat);
      return next;
    });
  }, []);

  const handleToggleItem = useCallback(
    async (item: Item) => {
      const newStatus: BoardItemStatus =
        item.status === 'Completed' ? 'Needed' : 'Completed';
      try {
        await setItemStatus(item.id, newStatus);
      } catch {
        // error handled by hook
      }
    },
    [setItemStatus],
  );

  const handleEditItem = useCallback(
    (item: Item) => {
      navigation.navigate('ItemSheet', { boardId, itemId: item.id });
    },
    [navigation, boardId],
  );

  const handleRemoveItem = useCallback(
    async (item: Item) => {
      const previousStatus = item.status;
      try {
        setUndoState({ itemId: item.id, previousStatus });
        await setItemStatus(item.id, 'Removed');

        if (undoTimerRef.current) clearTimeout(undoTimerRef.current);
        undoTimerRef.current = setTimeout(() => {
          setUndoState(null);
        }, 5000);
      } catch {
        setUndoState(null);
      }
    },
    [setItemStatus],
  );

  const handleUndo = useCallback(async () => {
    if (!undoState) return;
    try {
      await setItemStatus(undoState.itemId, undoState.previousStatus);
    } catch {
      // error handled by hook
    } finally {
      setUndoState(null);
      if (undoTimerRef.current) {
        clearTimeout(undoTimerRef.current);
        undoTimerRef.current = null;
      }
    }
  }, [undoState, setItemStatus]);

  const handleDismissSnackbar = useCallback(() => {
    setUndoState(null);
    if (undoTimerRef.current) {
      clearTimeout(undoTimerRef.current);
      undoTimerRef.current = null;
    }
  }, []);

  const handleClearCompleted = useCallback(async () => {
    const count = completedCount;
    setClearing(true);
    try {
      await clearCompleted();
      setDoneExpanded(false);
      setClearedMessage(
        t('lovingBoards.boardDetail.clearedWithCount', { count }),
      );
      if (clearTimerRef.current) clearTimeout(clearTimerRef.current);
      clearTimerRef.current = setTimeout(() => {
        setClearedMessage(null);
      }, 4000);
    } catch {
      // error handled by hook
    } finally {
      setClearing(false);
    }
  }, [clearCompleted, completedCount, t]);

  const handleResetWeekly = useCallback(async () => {
    try {
      await resetWeekly();
    } catch {
      // error handled by hook
    }
  }, [resetWeekly]);

  const handleAddItem = useCallback(() => {
    navigation.navigate('ItemSheet', { boardId });
  }, [navigation, boardId]);

  const handleMembersPress = useCallback(() => {
    navigation.navigate('BoardMembers', { boardId });
  }, [navigation, boardId]);

  // ── Render guards ─────────────────────────────────────────────────────────

  if (isLoading && !board) {
    return (
      <View style={styles.center}>
        <ActivityIndicator
          size="large"
          color={colors.primary}
          testID="board-detail-loading"
        />
      </View>
    );
  }

  if (error && !board) {
    return (
      <View style={styles.center}>
        <Text style={styles.errorText}>
          {error ? t(error) : t('common.error')}
        </Text>
        <Pressable
          style={({ pressed }) => [styles.retryBtn, pressed && styles.pressed]}
          onPress={refetch}
          accessibilityRole="button"
          accessibilityLabel={t('common.retry')}
          testID="board-detail-retry"
        >
          <Text style={styles.retryText}>{t('common.retry')}</Text>
        </Pressable>
      </View>
    );
  }

  if (!board) {
    return (
      <View style={styles.center}>
        <Text style={styles.errorText}>{t('common.error')}</Text>
      </View>
    );
  }

  const hasItems = allNeeded.length > 0 || completedCount > 0;
  const hasCompleted = completedCount > 0;

  return (
    <View style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <Text style={styles.boardName}>{board.name}</Text>
        <Text style={styles.headerMeta}>
          {t('lovingBoards.boardList.memberCount', {
            count: board.memberUserIds.length,
          })}
          {' · '}
          {t('lovingBoards.boardList.itemCount', { count: allNeeded.length })}
        </Text>
        <View style={styles.headerActions}>
          <Pressable
            style={({ pressed }) => [
              styles.headerBtn,
              pressed && styles.pressed,
            ]}
            onPress={handleMembersPress}
            accessibilityRole="button"
            accessibilityLabel={t('lovingBoards.boardDetail.members')}
            testID="board-detail-members"
          >
            <Text style={styles.headerBtnText}>
              {t('lovingBoards.boardDetail.members')}
            </Text>
          </Pressable>
          <Pressable
            style={({ pressed }) => [
              styles.headerBtn,
              pressed && styles.pressed,
            ]}
            onPress={handleResetWeekly}
            accessibilityRole="button"
            accessibilityLabel={t('lovingBoards.boardDetail.resetWeekly')}
            testID="board-detail-reset-weekly"
          >
            <Text style={styles.headerBtnText}>
              {t('lovingBoards.boardDetail.resetWeekly')}
            </Text>
          </Pressable>
        </View>
      </View>

      {/* Progress strip */}
      <ProgressStrip done={progressDone} total={progressTotal} />

      {/* Undo snackbar */}
      {undoState && (
        <View style={styles.snackbarContainer}>
          <Snackbar
            message={t('lovingBoards.boardDetail.removed')}
            actionLabel={t('lovingBoards.boardDetail.undo')}
            onAction={handleUndo}
            onDismiss={handleDismissSnackbar}
            durationMs={5000}
          />
        </View>
      )}

      {/* Cleared snackbar */}
      {clearedMessage && !undoState && (
        <View style={styles.snackbarContainer}>
          <Snackbar
            message={clearedMessage}
            onDismiss={() => {
              setClearedMessage(null);
              if (clearTimerRef.current) {
                clearTimeout(clearTimerRef.current);
                clearTimerRef.current = null;
              }
            }}
            durationMs={4000}
          />
        </View>
      )}

      <ScrollView
        style={styles.scroll}
        contentContainerStyle={styles.scrollContent}
        testID="board-detail-screen"
      >
        {!hasItems ? (
          <EmptyState
            emoji="📝"
            title={t('lovingBoards.boardDetail.emptyDetail')}
            subtitle={t('lovingBoards.boardDetail.emptyDetailHint')}
            actionLabel={t('lovingBoards.boardDetail.addItem')}
            onAction={handleAddItem}
            testID="board-detail-empty"
          />
        ) : (
          <>
            {/* Active items by category */}
            {categoryNames.map(cat => {
              const isUncategorized = cat === '__uncategorized__';
              const catLabel = isUncategorized
                ? t('lovingBoards.boardDetail.uncategorized')
                : cat;
              const catItems = isUncategorized
                ? uncategorizedActive
                : activeByCategory.get(cat) ?? [];
              const isCollapsed = collapsedCategories.has(cat);

              if (catItems.length === 0) return null;

              return (
                <CategoryCard
                  key={cat}
                  categoryName={catLabel}
                  categoryEmoji={isUncategorized ? '📦' : getCategoryEmoji(cat)}
                  itemCount={catItems.length}
                  tint={isUncategorized ? 'primary' : getCategoryTint(cat)}
                  isCollapsed={isCollapsed}
                  onToggle={() => toggleCategory(cat)}
                  accessibilityLabel={`${catLabel} (${catItems.length})`}
                >
                  {catItems.map(item => (
                    <ItemRow
                      key={item.id}
                      item={item}
                      currentUserId={userId ?? undefined}
                      onToggle={handleToggleItem}
                      onEdit={handleEditItem}
                      onRemove={handleRemoveItem}
                    />
                  ))}
                </CategoryCard>
              );
            })}

            {/* Done section */}
            {hasCompleted && (
              <CategoryCard
                categoryName={`${t(
                  'lovingBoards.boardDetail.done',
                )} · ${completedCount}`}
                categoryEmoji="✅"
                itemCount={completedCount}
                tint="success"
                isCollapsed={!doneExpanded}
                onToggle={() => setDoneExpanded(prev => !prev)}
                accessibilityLabel={`${t(
                  'lovingBoards.boardDetail.done',
                )} (${completedCount})`}
              >
                {/* Clear button inside done section */}
                <Pressable
                  style={({ pressed }) => [
                    styles.clearBtn,
                    pressed && styles.pressed,
                  ]}
                  onPress={handleClearCompleted}
                  disabled={clearing}
                  accessibilityRole="button"
                  accessibilityLabel={t(
                    'lovingBoards.boardDetail.clearCompleted',
                  )}
                  testID="clear-completed-button"
                >
                  {clearing ? (
                    <ActivityIndicator
                      size="small"
                      color={colors.textTertiary}
                    />
                  ) : (
                    <Text style={styles.clearBtnText}>
                      {t('lovingBoards.boardDetail.clearCompleted')}
                    </Text>
                  )}
                </Pressable>
                {completedItems.map(item => (
                  <ItemRow
                    key={item.id}
                    item={item}
                    currentUserId={userId ?? undefined}
                    onToggle={handleToggleItem}
                    onEdit={handleEditItem}
                    onRemove={handleRemoveItem}
                  />
                ))}
              </CategoryCard>
            )}
          </>
        )}
      </ScrollView>

      {/* FAB */}
      <Pressable
        style={({ pressed }) => [styles.fab, pressed && styles.fabPressed]}
        onPress={handleAddItem}
        accessibilityRole="button"
        accessibilityLabel={t('lovingBoards.boardDetail.addItem')}
        accessibilityHint={t('lovingBoards.boardDetail.emptyDetailHint')}
        testID="board-detail-add-item"
      >
        <Text style={styles.fabText}>＋</Text>
      </Pressable>
    </View>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      flex: 1,
      backgroundColor: t.colors.background,
    },
    header: {
      paddingHorizontal: spacing.lg,
      paddingVertical: spacing.md,
      borderBottomWidth: StyleSheet.hairlineWidth,
      borderBottomColor: t.colors.separator,
    },
    boardName: {
      ...t.typography.heading,
      marginBottom: spacing.xs,
    },
    headerMeta: {
      ...t.typography.caption,
      color: t.colors.textTertiary,
      marginBottom: spacing.sm,
    },
    headerActions: {
      flexDirection: 'row',
      gap: spacing.sm,
    },
    headerBtn: {
      minHeight: MIN_TOUCH,
      minWidth: MIN_TOUCH,
      paddingHorizontal: spacing.md,
      paddingVertical: spacing.sm,
      borderRadius: borderRadius.md,
      borderWidth: 1,
      borderColor: t.colors.border,
      alignItems: 'center',
      justifyContent: 'center',
    },
    headerBtnText: {
      ...t.typography.link,
      color: t.colors.primary,
    },
    snackbarContainer: {
      position: 'absolute',
      bottom: 76,
      left: 0,
      right: 0,
      zIndex: zIndexScale.toast,
    },
    scroll: {
      flex: 1,
    },
    scrollContent: {
      padding: spacing.lg,
      gap: spacing.md,
      paddingBottom: 80,
    },
    clearBtn: {
      alignSelf: 'flex-end',
      minHeight: MIN_TOUCH,
      minWidth: MIN_TOUCH,
      paddingHorizontal: spacing.sm,
      paddingVertical: spacing.xs,
      alignItems: 'center',
      justifyContent: 'center',
    },
    clearBtnText: {
      ...t.typography.caption,
      color: t.colors.error,
    },
    fab: {
      position: 'absolute',
      right: spacing.xl,
      bottom: spacing.xl,
      width: 56,
      height: 56,
      borderRadius: 28,
      backgroundColor: t.colors.primary,
      alignItems: 'center',
      justifyContent: 'center',
      boxShadow: '0 2px 4px rgba(43,33,24,0.25)',
    },
    fabPressed: {
      opacity: 0.8,
    },
    fabText: {
      fontSize: 24,
      color: t.colors.textInverse,
      lineHeight: 26,
    },
    center: {
      flex: 1,
      alignItems: 'center',
      justifyContent: 'center',
      backgroundColor: t.colors.background,
      gap: spacing.md,
    },
    errorText: {
      ...t.typography.body,
      color: t.colors.error,
      textAlign: 'center',
      paddingHorizontal: spacing.xl,
    },
    retryBtn: {
      minHeight: MIN_TOUCH,
      minWidth: MIN_TOUCH,
      paddingHorizontal: spacing.lg,
      paddingVertical: spacing.sm,
    },
    retryText: {
      ...t.typography.link,
      color: t.colors.primary,
    },
    pressed: {
      opacity: 0.7,
    },
  });

export default BoardDetailScreen;
