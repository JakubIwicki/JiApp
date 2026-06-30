import React, { useCallback, useState } from 'react';
import {
  ActivityIndicator,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import useBoards from '../hooks/useBoards';
import { useTheme, useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';
import EmptyState from '../components/EmptyState';
import type { LovingBoardsStackParamList } from '../../../navigation/types';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

type NavigationProp = NativeStackNavigationProp<
  LovingBoardsStackParamList,
  'BoardList'
>;

const MIN_TOUCH = 44;

const BoardListScreen: React.FC = () => {
  const { t } = useTranslation();
  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);
  const navigation = useNavigation<NavigationProp>();
  const { boards, isLoading, error, refetch, createBoard } = useBoards();

  const [showCreate, setShowCreate] = useState(false);
  const [newName, setNewName] = useState('');
  const [creating, setCreating] = useState(false);

  const handleBoardPress = useCallback(
    (boardId: number) => {
      navigation.navigate('BoardDetail', { boardId });
    },
    [navigation],
  );

  const handleCreate = useCallback(async () => {
    const name = newName.trim();
    if (!name) return;
    setCreating(true);
    try {
      const id = await createBoard(name);
      setNewName('');
      setShowCreate(false);
      if (id !== undefined) {
        navigation.navigate('BoardDetail', { boardId: id });
      }
    } catch {
      // error handled by hook
    } finally {
      setCreating(false);
    }
  }, [newName, createBoard, navigation]);

  if (isLoading && boards.length === 0) {
    return (
      <View style={styles.center}>
        <ActivityIndicator
          size="large"
          color={colors.primary}
          testID="board-list-loading"
        />
      </View>
    );
  }

  if (error && boards.length === 0) {
    return (
      <View style={styles.center}>
        <Text style={styles.errorText}>
          {error ? t(error) : t('lovingBoards.boardList.loadError')}
        </Text>
        <Pressable
          style={({ pressed }) => [styles.retryBtn, pressed && styles.pressed]}
          onPress={refetch}
          accessibilityRole="button"
          accessibilityLabel={t('common.retry')}
          testID="board-list-retry"
        >
          <Text style={styles.retryText}>{t('common.retry')}</Text>
        </Pressable>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <ScrollView
        style={styles.scroll}
        contentContainerStyle={styles.scrollContent}
        testID="board-list-screen"
      >
        {boards.length === 0 ? (
          <EmptyState
            emoji="💝"
            title={t('lovingBoards.boardList.empty')}
            subtitle={t('lovingBoards.boardList.emptyHint')}
            actionLabel={t('lovingBoards.boardList.newBoard')}
            onAction={() => setShowCreate(true)}
            testID="board-list-empty"
          />
        ) : (
          boards.map(board => (
            <Pressable
              key={board.id}
              style={({ pressed }) => [styles.card, pressed && styles.pressed]}
              onPress={() => handleBoardPress(board.id)}
              accessibilityRole="button"
              accessibilityLabel={board.name}
              testID={`board-card-${board.id}`}
            >
              <View style={styles.cardBody}>
                <Text style={styles.boardName} numberOfLines={1}>
                  {board.name}
                </Text>
                <Text style={styles.boardMeta}>
                  {t('lovingBoards.boardList.memberCount', {
                    count: board.memberUserIds.length,
                  })}
                </Text>
              </View>
            </Pressable>
          ))
        )}
      </ScrollView>

      {showCreate && (
        <View style={styles.createRow}>
          <TextInput
            style={styles.createInput}
            placeholder={t('lovingBoards.boardList.createPrompt')}
            placeholderTextColor={colors.textTertiary}
            value={newName}
            onChangeText={setNewName}
            maxLength={200}
            autoFocus
            testID="board-create-input"
          />
          <Pressable
            style={({ pressed }) => [
              styles.createConfirmBtn,
              pressed && styles.pressed,
              creating && styles.disabled,
            ]}
            onPress={handleCreate}
            disabled={creating}
            accessibilityRole="button"
            accessibilityLabel={t('lovingBoards.boardList.create')}
            testID="board-create-confirm"
          >
            {creating ? (
              <ActivityIndicator color={colors.textInverse} size="small" />
            ) : (
              <Text style={styles.createConfirmText}>
                {t('lovingBoards.boardList.create')}
              </Text>
            )}
          </Pressable>
          <Pressable
            style={({ pressed }) => [
              styles.cancelBtn,
              pressed && styles.pressed,
            ]}
            onPress={() => {
              setShowCreate(false);
              setNewName('');
            }}
            accessibilityRole="button"
            accessibilityLabel={t('common.cancel')}
            testID="board-create-cancel"
          >
            <Text style={styles.cancelText}>{t('common.cancel')}</Text>
          </Pressable>
        </View>
      )}

      <Pressable
        style={({ pressed }) => [styles.fab, pressed && styles.fabPressed]}
        onPress={() => setShowCreate(true)}
        accessibilityRole="button"
        accessibilityLabel={t('lovingBoards.boardList.newBoard')}
        testID="board-list-fab"
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
    scroll: {
      flex: 1,
    },
    scrollContent: {
      padding: spacing.lg,
      gap: spacing.md,
    },
    card: {
      backgroundColor: t.colors.surface,
      borderRadius: borderRadius.lg,
      paddingVertical: spacing.lg,
      paddingHorizontal: spacing.lg,
      minHeight: 64,
      boxShadow: '0 2px 8px rgba(43,33,24,0.08)',
    },
    cardBody: {
      gap: spacing.xs,
    },
    boardName: {
      ...t.typography.body,
      fontWeight: '600',
      color: t.colors.textPrimary,
    },
    boardMeta: {
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
    createRow: {
      flexDirection: 'row',
      alignItems: 'center',
      paddingHorizontal: spacing.lg,
      paddingVertical: spacing.md,
      borderTopWidth: StyleSheet.hairlineWidth,
      borderTopColor: t.colors.separator,
      backgroundColor: t.colors.surface,
      gap: spacing.sm,
    },
    createInput: {
      flex: 1,
      minHeight: MIN_TOUCH,
      backgroundColor: t.colors.background,
      borderWidth: 1,
      borderColor: t.colors.border,
      borderRadius: borderRadius.md,
      paddingHorizontal: spacing.md,
      ...t.typography.body,
    },
    createConfirmBtn: {
      minHeight: MIN_TOUCH,
      minWidth: 80,
      paddingHorizontal: spacing.lg,
      borderRadius: borderRadius.md,
      backgroundColor: t.colors.primary,
      alignItems: 'center',
      justifyContent: 'center',
    },
    createConfirmText: {
      color: t.colors.textInverse,
      fontWeight: '600',
      fontSize: 15,
    },
    cancelBtn: {
      minHeight: MIN_TOUCH,
      minWidth: MIN_TOUCH,
      paddingHorizontal: spacing.sm,
      alignItems: 'center',
      justifyContent: 'center',
    },
    cancelText: {
      ...t.typography.link,
      color: t.colors.textSecondary,
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
    disabled: {
      opacity: 0.5,
    },
  });

export default BoardListScreen;
