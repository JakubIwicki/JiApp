import { useCallback, useMemo, useState } from 'react';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { useTranslation } from 'react-i18next';
import useAuth from '../../../hooks/useAuth';
import useBoard from './useBoard';
import useUndoSnackbar, { type UndoState } from './useUndoSnackbar';
import type { Board, BoardItemStatus, Item } from '../types/api';
import type { LovingBoardsStackParamList } from '../../../navigation/types';

type NavigationProp = NativeStackNavigationProp<
  LovingBoardsStackParamList,
  'BoardDetail'
>;

export interface UseBoardDetailResult {
  board: Board | null;
  isLoading: boolean;
  error: string | null;
  refetch: () => Promise<void>;
  userId: number | null;
  collapsedCategories: Set<string>;
  doneExpanded: boolean;
  clearing: boolean;
  undoState: UndoState | null;
  clearedMessage: string | null;
  activeByCategory: Map<string, Item[]>;
  completedItems: Item[];
  uncategorizedActive: Item[];
  categoryNames: string[];
  allNeeded: Item[];
  completedCount: number;
  progressDone: number;
  progressTotal: number;
  hasItems: boolean;
  hasCompleted: boolean;
  toggleCategory: (category: string) => void;
  toggleDoneExpanded: () => void;
  handleToggleItem: (item: Item) => Promise<void>;
  handleEditItem: (item: Item) => void;
  handleRemoveItem: (item: Item) => Promise<void>;
  handleUndo: () => Promise<void>;
  handleDismissSnackbar: () => void;
  dismissCleared: () => void;
  handleClearCompleted: () => Promise<void>;
  handleResetWeekly: () => Promise<void>;
  handleAddItem: () => void;
  handleMembersPress: () => void;
}

const useBoardDetail = (boardId: number): UseBoardDetailResult => {
  const { t } = useTranslation();
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
  const {
    undoState,
    clearedMessage,
    showUndo,
    armUndoTimeout,
    clearUndo,
    showCleared,
    dismissCleared,
  } = useUndoSnackbar();

  const [collapsedCategories, setCollapsedCategories] = useState<Set<string>>(
    new Set(),
  );
  const [doneExpanded, setDoneExpanded] = useState(false);
  const [clearing, setClearing] = useState(false);

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
  const hasItems = allNeeded.length > 0 || completedCount > 0;
  const hasCompleted = completedCount > 0;

  const toggleCategory = useCallback((cat: string) => {
    setCollapsedCategories(prev => {
      const next = new Set(prev);
      if (next.has(cat)) next.delete(cat);
      else next.add(cat);
      return next;
    });
  }, []);

  const toggleDoneExpanded = useCallback(() => {
    setDoneExpanded(prev => !prev);
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
        showUndo(item.id, previousStatus);
        await setItemStatus(item.id, 'Removed');
        armUndoTimeout();
      } catch {
        clearUndo();
      }
    },
    [setItemStatus, showUndo, armUndoTimeout, clearUndo],
  );

  const handleUndo = useCallback(async () => {
    if (!undoState) return;
    try {
      await setItemStatus(undoState.itemId, undoState.previousStatus);
    } catch {
      // error handled by hook
    } finally {
      clearUndo();
    }
  }, [undoState, setItemStatus, clearUndo]);

  const handleDismissSnackbar = useCallback(() => {
    clearUndo();
  }, [clearUndo]);

  const handleClearCompleted = useCallback(async () => {
    const count = completedCount;
    setClearing(true);
    try {
      await clearCompleted();
      setDoneExpanded(false);
      showCleared(t('lovingBoards.boardDetail.clearedWithCount', { count }));
    } catch {
      // error handled by hook
    } finally {
      setClearing(false);
    }
  }, [clearCompleted, completedCount, t, showCleared]);

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

  return {
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
  };
};

export default useBoardDetail;
