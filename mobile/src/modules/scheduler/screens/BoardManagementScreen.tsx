import React, { useCallback, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';
import { useTranslation } from 'react-i18next';
import { useBoard } from '../hooks/useBoard';
import useToast from '../../../hooks/useToast';
import {
  borderRadius,
  colors,
  commonStyles,
  spacing,
  typography,
} from '../../../styles/theme';
import type { Board } from '../types/api';

const MIN_TOUCH = 44;

interface BoardRowProps {
  readonly board: Board;
  readonly expanded: boolean;
  readonly onToggle: (id: number) => void;
  readonly onDelete: (board: Board) => void;
  readonly onAddMember: (boardId: number, userId: number) => void;
  readonly onRemoveMember: (boardId: number, userId: number) => void;
}

const BoardRow: React.FC<BoardRowProps> = ({
  board,
  expanded,
  onToggle,
  onDelete,
  onAddMember,
  onRemoveMember,
}) => {
  const { t } = useTranslation();
  const [memberId, setMemberId] = useState('');

  const handleAddMember = useCallback(() => {
    const parsed = Number(memberId.trim());
    if (!memberId.trim() || Number.isNaN(parsed)) return;
    onAddMember(board.id, parsed);
    setMemberId('');
  }, [memberId, onAddMember, board.id]);

  const memberCountLabel = t('boardManagement.memberCount', {
    count: board.memberUserIds.length,
  });

  return (
    <View style={styles.boardCard}>
      <Pressable
        style={({ pressed }) => [styles.boardHeader, pressed && styles.pressed]}
        onPress={() => onToggle(board.id)}
        accessibilityRole="button"
        accessibilityLabel={board.name}
        testID={`board-expand-${board.id}`}
      >
        <View style={styles.boardInfo}>
          <Text style={styles.boardName}>{board.name}</Text>
          <Text style={styles.memberCount}>{memberCountLabel}</Text>
        </View>
        <Pressable
          style={({ pressed }) => [styles.deleteBtn, pressed && styles.pressed]}
          onPress={() => onDelete(board)}
          accessibilityRole="button"
          accessibilityLabel={`${t('boardManagement.delete')} ${board.name}`}
          testID={`board-delete-${board.id}`}
        >
          <Text style={styles.deleteBtnText}>
            {t('boardManagement.delete')}
          </Text>
        </Pressable>
      </Pressable>

      {expanded && (
        <View style={styles.membersSection}>
          <Text style={styles.membersTitle}>
            {t('boardManagement.members')}
          </Text>
          {board.memberUserIds.map(userId => (
            <View key={userId} style={styles.memberRow}>
              <Text style={styles.memberId}>#{userId}</Text>
              <Pressable
                style={({ pressed }) => [
                  styles.removeBtn,
                  pressed && styles.pressed,
                ]}
                onPress={() => onRemoveMember(board.id, userId)}
                accessibilityRole="button"
                accessibilityLabel={`${t('boardManagement.remove')} #${userId}`}
                testID={`member-remove-${board.id}-${userId}`}
              >
                <Text style={styles.removeBtnText}>
                  {t('boardManagement.remove')}
                </Text>
              </Pressable>
            </View>
          ))}

          <View style={styles.addMemberRow}>
            <TextInput
              style={styles.memberInput}
              placeholder={t('boardManagement.memberIdPlaceholder')}
              placeholderTextColor={colors.textTertiary}
              value={memberId}
              onChangeText={setMemberId}
              keyboardType="number-pad"
              testID={`member-id-input-${board.id}`}
            />
            <Pressable
              style={({ pressed }) => [
                styles.addMemberBtn,
                pressed && styles.pressed,
              ]}
              onPress={handleAddMember}
              accessibilityRole="button"
              accessibilityLabel={t('boardManagement.addMember')}
              testID={`member-add-button-${board.id}`}
            >
              <Text style={styles.addMemberBtnText}>
                {t('boardManagement.addMember')}
              </Text>
            </Pressable>
          </View>
        </View>
      )}
    </View>
  );
};

const BoardManagementScreen: React.FC = () => {
  const { t } = useTranslation();
  const {
    boards,
    isLoading,
    createBoard,
    deleteBoard,
    addMember,
    removeMember,
  } = useBoard();
  const { showError, showSuccess } = useToast();

  const [newName, setNewName] = useState('');
  const [creating, setCreating] = useState(false);
  const [expandedId, setExpandedId] = useState<number | null>(null);

  const handleToggle = useCallback((id: number) => {
    setExpandedId(current => (current === id ? null : id));
  }, []);

  const handleCreate = useCallback(async () => {
    const name = newName.trim();
    if (!name) return;
    setCreating(true);
    try {
      await createBoard(name);
      setNewName('');
      showSuccess('boardManagement.create');
    } catch {
      showError('boardManagement.createError');
    } finally {
      setCreating(false);
    }
  }, [newName, createBoard, showSuccess, showError]);

  const handleDelete = useCallback(
    (board: Board) => {
      Alert.alert(
        t('boardManagement.title'),
        t('boardManagement.deleteConfirm', { name: board.name }),
        [
          { text: t('common.cancel'), style: 'cancel' },
          {
            text: t('boardManagement.delete'),
            style: 'destructive',
            onPress: async () => {
              try {
                await deleteBoard(board.id);
              } catch {
                showError('boardManagement.deleteError');
              }
            },
          },
        ],
      );
    },
    [t, deleteBoard, showError],
  );

  const handleAddMember = useCallback(
    async (boardId: number, userId: number) => {
      try {
        await addMember(boardId, userId);
      } catch {
        showError('boardManagement.memberError');
      }
    },
    [addMember, showError],
  );

  const handleRemoveMember = useCallback(
    async (boardId: number, userId: number) => {
      try {
        await removeMember(boardId, userId);
      } catch {
        showError('boardManagement.memberError');
      }
    },
    [removeMember, showError],
  );

  if (isLoading && boards.length === 0) {
    return (
      <View style={[commonStyles.screenContainer, styles.center]}>
        <ActivityIndicator
          size="large"
          color={colors.primary}
          testID="board-management-loading"
        />
      </View>
    );
  }

  return (
    <ScrollView
      style={commonStyles.screenContainer}
      contentContainerStyle={styles.content}
      testID="board-management-screen"
    >
      <View style={styles.createCard}>
        <TextInput
          style={styles.createInput}
          placeholder={t('boardManagement.createPlaceholder')}
          placeholderTextColor={colors.textTertiary}
          value={newName}
          onChangeText={setNewName}
          maxLength={200}
          testID="board-name-input"
        />
        <Pressable
          style={({ pressed }) => [
            styles.createButton,
            pressed && styles.pressed,
            creating && styles.disabled,
          ]}
          onPress={handleCreate}
          disabled={creating}
          accessibilityRole="button"
          accessibilityLabel={t('boardManagement.create')}
          testID="board-create-button"
        >
          {creating ? (
            <ActivityIndicator color={colors.textInverse} size="small" />
          ) : (
            <Text style={styles.createButtonText}>
              {t('boardManagement.create')}
            </Text>
          )}
        </Pressable>
      </View>

      {boards.length === 0 ? (
        <View style={commonStyles.emptyState}>
          <Text style={commonStyles.emptyText}>
            {t('boardManagement.empty')}
          </Text>
        </View>
      ) : (
        boards.map(board => (
          <BoardRow
            key={board.id}
            board={board}
            expanded={expandedId === board.id}
            onToggle={handleToggle}
            onDelete={handleDelete}
            onAddMember={handleAddMember}
            onRemoveMember={handleRemoveMember}
          />
        ))
      )}
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  center: {
    alignItems: 'center',
    justifyContent: 'center',
  },
  content: {
    padding: spacing.lg,
    gap: spacing.md,
  },
  createCard: {
    flexDirection: 'row',
    gap: spacing.sm,
    alignItems: 'center',
  },
  createInput: {
    flex: 1,
    minHeight: MIN_TOUCH,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: borderRadius.md,
    paddingHorizontal: spacing.md,
    ...typography.body,
  },
  createButton: {
    minHeight: MIN_TOUCH,
    minWidth: 88,
    paddingHorizontal: spacing.lg,
    borderRadius: borderRadius.md,
    backgroundColor: colors.primary,
    alignItems: 'center',
    justifyContent: 'center',
  },
  createButtonText: {
    color: colors.textInverse,
    fontWeight: '600',
    fontSize: 15,
  },
  boardCard: {
    backgroundColor: colors.surface,
    borderRadius: borderRadius.lg,
    overflow: 'hidden',
    boxShadow: '0 2px 8px rgba(43,33,24,0.08)',
  },
  boardHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.lg,
    minHeight: 56,
  },
  boardInfo: {
    flex: 1,
  },
  boardName: {
    ...typography.body,
    fontWeight: '600',
    color: colors.textPrimary,
  },
  memberCount: {
    ...typography.caption,
    color: colors.textTertiary,
  },
  deleteBtn: {
    minHeight: MIN_TOUCH,
    minWidth: MIN_TOUCH,
    paddingHorizontal: spacing.md,
    alignItems: 'center',
    justifyContent: 'center',
  },
  deleteBtnText: {
    ...typography.caption,
    color: colors.error,
    fontWeight: '600',
  },
  membersSection: {
    paddingHorizontal: spacing.lg,
    paddingBottom: spacing.md,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.separator,
    paddingTop: spacing.md,
  },
  membersTitle: {
    ...commonStyles.sectionHeader,
    marginHorizontal: 0,
  },
  memberRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingVertical: spacing.xs,
    minHeight: MIN_TOUCH,
  },
  memberId: {
    ...typography.body,
    color: colors.textPrimary,
  },
  removeBtn: {
    minHeight: MIN_TOUCH,
    minWidth: MIN_TOUCH,
    paddingHorizontal: spacing.sm,
    alignItems: 'center',
    justifyContent: 'center',
  },
  removeBtnText: {
    ...typography.caption,
    color: colors.error,
  },
  addMemberRow: {
    flexDirection: 'row',
    gap: spacing.sm,
    alignItems: 'center',
    marginTop: spacing.sm,
  },
  memberInput: {
    flex: 1,
    minHeight: MIN_TOUCH,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: borderRadius.md,
    paddingHorizontal: spacing.md,
    ...typography.body,
  },
  addMemberBtn: {
    minHeight: MIN_TOUCH,
    paddingHorizontal: spacing.md,
    borderRadius: borderRadius.md,
    borderWidth: 1.5,
    borderColor: colors.primary,
    alignItems: 'center',
    justifyContent: 'center',
  },
  addMemberBtnText: {
    ...typography.caption,
    color: colors.primary,
    fontWeight: '600',
  },
  pressed: {
    opacity: 0.7,
  },
  disabled: {
    opacity: 0.5,
  },
});

export default BoardManagementScreen;
