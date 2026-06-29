import React, { useCallback, useMemo, useState } from 'react';
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
import { useTheme, useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';
import type { LovingBoardsStackParamList } from '../../../navigation/types';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { Item, BoardItemStatus } from '../types/api';

type Props = NativeStackScreenProps<LovingBoardsStackParamList, 'BoardDetail'>;
type NavigationProp = Props['navigation'];

const MIN_TOUCH = 44;

interface RemovedItemState {
  itemId: number;
  previousStatus: BoardItemStatus;
}

function parseDueDate(expiryDate: string | null): Date | null {
  if (!expiryDate) return null;
  const d = new Date(expiryDate);
  return Number.isNaN(d.getTime()) ? null : d;
}

function getDueUrgency(expiryDate: string | null): 'none' | 'soon' | 'overdue' {
  const due = parseDueDate(expiryDate);
  if (!due) return 'none';
  const now = new Date();
  now.setHours(0, 0, 0, 0);
  due.setHours(0, 0, 0, 0);
  if (due < now) return 'overdue';
  const twoDays = new Date(now);
  twoDays.setDate(twoDays.getDate() + 2);
  if (due <= twoDays) return 'soon';
  return 'none';
}

function fmtDueDate(expiryDate: string | null): string {
  const due = parseDueDate(expiryDate);
  if (!due) return '';
  const y = due.getFullYear();
  const m = String(due.getMonth() + 1).padStart(2, '0');
  const d = String(due.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

interface ItemRowProps {
  readonly item: Item;
  readonly onToggle: (item: Item) => void;
  readonly onEdit: (item: Item) => void;
  readonly onRemove: (item: Item) => void;
  readonly colors: ReturnType<typeof useTheme>['colors'];
}

const ItemRow: React.FC<ItemRowProps> = ({
  item,
  onToggle,
  onEdit,
  onRemove,
  colors,
}) => {
  const { t } = useTranslation();
  const styles = useThemedStyles(makeItemRowStyles);
  const isCompleted = item.status === 'Completed';
  const urgency = getDueUrgency(item.expiryDate);

  const dueColor =
    urgency === 'overdue'
      ? colors.error
      : urgency === 'soon'
      ? colors.warning
      : colors.textTertiary;

  const handleRemove = useCallback(() => {
    onRemove(item);
  }, [item, onRemove]);

  return (
    <Pressable
      style={({ pressed }) => [
        styles.row,
        isCompleted && styles.rowCompleted,
        pressed && styles.pressed,
      ]}
      onPress={() => onEdit(item)}
      accessibilityRole="button"
      accessibilityLabel={item.title}
      testID={`item-row-${item.id}`}
    >
      {/* Checkbox */}
      <Pressable
        style={styles.checkHitArea}
        onPress={() => onToggle(item)}
        accessibilityRole="checkbox"
        accessibilityState={{ checked: isCompleted }}
        accessibilityLabel={item.title}
        testID={`item-check-${item.id}`}
      >
        <View
          style={[
            styles.checkbox,
            isCompleted && {
              backgroundColor: colors.success,
              borderColor: colors.success,
            },
          ]}
        >
          {isCompleted && <Text style={styles.checkmark}>✓</Text>}
        </View>
      </Pressable>

      {/* Content */}
      <View style={styles.itemBody}>
        <View style={styles.itemTitleRow}>
          <Text
            style={[styles.itemTitle, isCompleted && styles.itemTitleDone]}
            numberOfLines={2}
          >
            {item.title}
          </Text>
          {item.isRecurring && (
            <Text
              style={styles.recurringBadge}
              accessibilityLabel={t('lovingBoards.boardDetail.recurring')}
            >
              🔁
            </Text>
          )}
        </View>

        <View style={styles.chips}>
          {item.quantity && (
            <View style={styles.chip}>
              <Text style={styles.chipText}>
                {t('lovingBoards.boardDetail.qty', { qty: item.quantity })}
              </Text>
            </View>
          )}
          {item.expiryDate && (
            <View style={[styles.chip, { borderColor: dueColor }]}>
              <Text style={[styles.chipText, { color: dueColor }]}>
                {fmtDueDate(item.expiryDate)}
              </Text>
            </View>
          )}
          {item.assigneeUserId !== null && (
            <View style={styles.chip}>
              <Text style={styles.chipText}>
                {t('lovingBoards.boardDetail.assignedTo', {
                  id: item.assigneeUserId,
                })}
              </Text>
            </View>
          )}
        </View>

        <Text style={styles.mutedCaption}>
          {item.completedByUserId
            ? t('lovingBoards.boardDetail.boughtBy', {
                id: item.completedByUserId,
              })
            : t('lovingBoards.boardDetail.addedBy', { id: item.addedByUserId })}
        </Text>
      </View>

      {/* Remove affordance */}
      <Pressable
        style={styles.removeHitArea}
        onPress={handleRemove}
        accessibilityRole="button"
        accessibilityLabel={`Remove ${item.title}`}
        testID={`item-remove-${item.id}`}
      >
        <Text style={styles.removeIcon}>✕</Text>
      </Pressable>
    </Pressable>
  );
};

const makeItemRowStyles = (t: Theme) =>
  StyleSheet.create({
    row: {
      flexDirection: 'row',
      alignItems: 'flex-start',
      backgroundColor: t.colors.surface,
      borderRadius: borderRadius.md,
      paddingVertical: spacing.md,
      paddingLeft: spacing.sm,
      paddingRight: spacing.xs,
      minHeight: 64,
      boxShadow: '0 1px 3px rgba(43,33,24,0.05)',
    },
    rowCompleted: {
      opacity: 0.55,
    },
    checkHitArea: {
      minWidth: MIN_TOUCH,
      minHeight: MIN_TOUCH,
      alignItems: 'center',
      justifyContent: 'flex-start',
      paddingTop: 2,
    },
    checkbox: {
      width: 22,
      height: 22,
      borderRadius: 4,
      borderWidth: 2,
      borderColor: t.colors.border,
      alignItems: 'center',
      justifyContent: 'center',
    },
    checkmark: {
      color: t.colors.textInverse,
      fontSize: 14,
      fontWeight: '700',
      lineHeight: 16,
    },
    itemBody: {
      flex: 1,
      gap: spacing.xs,
    },
    itemTitleRow: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: spacing.xs,
    },
    itemTitle: {
      ...t.typography.body,
      color: t.colors.textPrimary,
      flexShrink: 1,
    },
    itemTitleDone: {
      textDecorationLine: 'line-through',
      color: t.colors.textTertiary,
    },
    recurringBadge: {
      fontSize: 14,
    },
    chips: {
      flexDirection: 'row',
      flexWrap: 'wrap',
      gap: spacing.xs,
    },
    chip: {
      borderWidth: 1,
      borderColor: t.colors.border,
      borderRadius: borderRadius.sm,
      paddingHorizontal: spacing.sm,
      paddingVertical: 2,
    },
    chipText: {
      ...t.typography.label,
      color: t.colors.textSecondary,
    },
    mutedCaption: {
      ...t.typography.label,
      color: t.colors.textTertiary,
    },
    removeHitArea: {
      minWidth: MIN_TOUCH,
      minHeight: MIN_TOUCH,
      alignItems: 'center',
      justifyContent: 'flex-start',
      paddingTop: 2,
    },
    removeIcon: {
      fontSize: 14,
      color: t.colors.textTertiary,
    },
    pressed: {
      opacity: 0.7,
    },
  });

const BoardDetailScreen: React.FC<Props> = ({ route }) => {
  const { boardId } = route.params;
  const { t } = useTranslation();
  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);
  const navigation = useNavigation<NavigationProp>();
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
  const [removedItem, setRemovedItem] = useState<RemovedItemState | null>(null);
  const [clearing, setClearing] = useState(false);

  // Undo timer ref — clear undo after a few seconds
  const undoTimeoutRef = React.useRef<ReturnType<typeof setTimeout> | null>(
    null,
  );

  React.useEffect(() => {
    const timer = undoTimeoutRef.current;
    return () => {
      if (timer) clearTimeout(timer);
    };
  }, [undoTimeoutRef]);

  // Group items by category
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
        // Capture first, then set status
        setRemovedItem({ itemId: item.id, previousStatus });
        await setItemStatus(item.id, 'Removed');

        // Auto-dismiss undo after 5 seconds
        if (undoTimeoutRef.current) clearTimeout(undoTimeoutRef.current);
        undoTimeoutRef.current = setTimeout(() => {
          setRemovedItem(null);
        }, 5000);
      } catch {
        setRemovedItem(null);
      }
    },
    [setItemStatus],
  );

  const handleUndo = useCallback(async () => {
    if (!removedItem) return;
    try {
      await setItemStatus(removedItem.itemId, removedItem.previousStatus);
    } catch {
      // error handled by hook
    } finally {
      setRemovedItem(null);
      if (undoTimeoutRef.current) {
        clearTimeout(undoTimeoutRef.current);
        undoTimeoutRef.current = null;
      }
    }
  }, [removedItem, setItemStatus]);

  const handleClearCompleted = useCallback(async () => {
    setClearing(true);
    try {
      await clearCompleted();
      setDoneExpanded(false);
    } catch {
      // error handled by hook
    } finally {
      setClearing(false);
    }
  }, [clearCompleted]);

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
        <Text style={styles.errorText}>{error}</Text>
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

  const allNeeded = items.filter(i => i.status === 'Needed');
  const hasCompleted = completedItems.length > 0;

  return (
    <View style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <View style={styles.headerLeft}>
          <Text style={styles.boardName}>{board.name}</Text>
          <Text style={styles.headerMeta}>
            {t('lovingBoards.boardList.memberCount', {
              count: board.memberUserIds.length,
            })}
            {' · '}
            {t('lovingBoards.boardList.itemCount', { count: allNeeded.length })}
          </Text>
        </View>
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

      {/* Undo banner */}
      {removedItem && (
        <View style={styles.undoBanner}>
          <Text style={styles.undoText}>
            {t('lovingBoards.boardDetail.removed')}
          </Text>
          <Pressable
            style={({ pressed }) => [styles.undoBtn, pressed && styles.pressed]}
            onPress={handleUndo}
            accessibilityRole="button"
            accessibilityLabel={t('lovingBoards.boardDetail.undo')}
            testID="board-detail-undo"
          >
            <Text style={styles.undoBtnText}>
              {t('lovingBoards.boardDetail.undo')}
            </Text>
          </Pressable>
        </View>
      )}

      <ScrollView
        style={styles.scroll}
        contentContainerStyle={styles.scrollContent}
        testID="board-detail-screen"
      >
        {allNeeded.length === 0 && completedItems.length === 0 ? (
          <View style={styles.emptyState}>
            <Text style={styles.emptyText}>
              {t('lovingBoards.boardDetail.empty')}
            </Text>
          </View>
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
                <View key={cat} style={styles.category}>
                  <Pressable
                    style={styles.categoryHeader}
                    onPress={() => toggleCategory(cat)}
                    accessibilityRole="button"
                    accessibilityLabel={`${catLabel} (${catItems.length})`}
                    testID={`category-header-${cat}`}
                  >
                    <Text style={styles.categoryTitle}>
                      {catLabel}{' '}
                      <Text style={styles.categoryCount}>
                        ({catItems.length})
                      </Text>
                    </Text>
                    <Text style={styles.chevron}>
                      {isCollapsed ? '▸' : '▾'}
                    </Text>
                  </Pressable>
                  {!isCollapsed &&
                    catItems.map(item => (
                      <ItemRow
                        key={item.id}
                        item={item}
                        onToggle={handleToggleItem}
                        onEdit={handleEditItem}
                        onRemove={handleRemoveItem}
                        colors={colors}
                      />
                    ))}
                </View>
              );
            })}

            {/* Completed (Done) section */}
            {hasCompleted && (
              <View style={styles.category}>
                <Pressable
                  style={styles.categoryHeader}
                  onPress={() => setDoneExpanded(prev => !prev)}
                  accessibilityRole="button"
                  accessibilityLabel={`${t('lovingBoards.boardDetail.done')} (${
                    completedItems.length
                  })`}
                  testID="done-section-header"
                >
                  <Text style={styles.categoryTitle}>
                    {t('lovingBoards.boardDetail.done')}{' '}
                    <Text style={styles.categoryCount}>
                      ({completedItems.length})
                    </Text>
                  </Text>
                  <View style={styles.doneActions}>
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
                    <Text style={styles.chevron}>
                      {doneExpanded ? '▾' : '▸'}
                    </Text>
                  </View>
                </Pressable>
                {doneExpanded &&
                  completedItems.map(item => (
                    <ItemRow
                      key={item.id}
                      item={item}
                      onToggle={handleToggleItem}
                      onEdit={handleEditItem}
                      onRemove={handleRemoveItem}
                      colors={colors}
                    />
                  ))}
              </View>
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
    headerLeft: {
      gap: spacing.xs,
      marginBottom: spacing.sm,
    },
    boardName: {
      ...t.typography.heading,
    },
    headerMeta: {
      ...t.typography.caption,
      color: t.colors.textTertiary,
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
    undoBanner: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'center',
      paddingVertical: spacing.sm,
      paddingHorizontal: spacing.lg,
      backgroundColor: t.colors.warning,
      gap: spacing.md,
    },
    undoText: {
      ...t.typography.caption,
      color: t.colors.textInverse,
    },
    undoBtn: {
      minHeight: MIN_TOUCH,
      minWidth: MIN_TOUCH,
      paddingHorizontal: spacing.md,
      alignItems: 'center',
      justifyContent: 'center',
    },
    undoBtnText: {
      ...t.typography.link,
      color: t.colors.textInverse,
      fontWeight: '700',
      textDecorationLine: 'underline',
    },
    scroll: {
      flex: 1,
    },
    scrollContent: {
      padding: spacing.lg,
      gap: spacing.md,
      paddingBottom: 80,
    },
    category: {
      gap: spacing.xs,
    },
    categoryHeader: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      paddingVertical: spacing.sm,
      minHeight: MIN_TOUCH,
    },
    categoryTitle: {
      ...t.typography.body,
      fontWeight: '600',
      color: t.colors.textPrimary,
    },
    categoryCount: {
      ...t.typography.caption,
      color: t.colors.textTertiary,
      fontWeight: '400',
    },
    chevron: {
      fontSize: 14,
      color: t.colors.textTertiary,
    },
    doneActions: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: spacing.sm,
    },
    clearBtn: {
      minHeight: MIN_TOUCH,
      minWidth: MIN_TOUCH,
      paddingHorizontal: spacing.sm,
      alignItems: 'center',
      justifyContent: 'center',
    },
    clearBtnText: {
      ...t.typography.caption,
      color: t.colors.textTertiary,
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
    emptyState: {
      paddingVertical: spacing.xxl,
      alignItems: 'center',
    },
    emptyText: {
      ...t.typography.body,
      color: t.colors.textSecondary,
    },
    pressed: {
      opacity: 0.7,
    },
  });

export default BoardDetailScreen;
