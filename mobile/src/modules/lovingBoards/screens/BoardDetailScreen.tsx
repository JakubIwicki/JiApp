import React from 'react';
import {
  ActivityIndicator,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { useTranslation } from 'react-i18next';
import useBoardDetail from '../hooks/useBoardDetail';
import { useTheme, useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius, zIndexScale } from '../../../styles/theme';
import type { LovingBoardsStackParamList } from '../../../navigation/types';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { CategoryTint } from '../components/CategoryCard';
import ProgressStrip from '../components/ProgressStrip';
import CategoryCard from '../components/CategoryCard';
import ItemRow from '../components/ItemRow';
import Snackbar from '../components/Snackbar';
import EmptyState from '../components/EmptyState';

type Props = NativeStackScreenProps<LovingBoardsStackParamList, 'BoardDetail'>;

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
  const {
    board,
    isLoading,
    error,
    refetch,
    userId,
    collapsedCategories,
    doneExpanded,
    clearing,
    undoState,
    clearedMessage,
    activeByCategory,
    completedItems,
    uncategorizedActive,
    categoryNames,
    allNeeded,
    completedCount,
    progressDone,
    progressTotal,
    hasItems,
    hasCompleted,
    toggleCategory,
    toggleDoneExpanded,
    handleToggleItem,
    handleEditItem,
    handleRemoveItem,
    handleUndo,
    handleDismissSnackbar,
    dismissCleared,
    handleClearCompleted,
    handleResetWeekly,
    handleAddItem,
    handleMembersPress,
  } = useBoardDetail(boardId);

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
            onDismiss={dismissCleared}
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
                : (activeByCategory.get(cat) ?? []);
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
                onToggle={toggleDoneExpanded}
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
