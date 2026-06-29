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
import { useTranslation } from 'react-i18next';
import useBoard from '../hooks/useBoard';
import { useTheme, useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';
import type { LovingBoardsStackParamList } from '../../../navigation/types';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

type Props = NativeStackScreenProps<LovingBoardsStackParamList, 'BoardMembers'>;

const MIN_TOUCH = 44;

const BoardMembersScreen: React.FC<Props> = ({ route }) => {
  const { boardId } = route.params;
  const { t } = useTranslation();
  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);
  const { board, isLoading, addMember, removeMember } = useBoard(boardId);
  const [userIdInput, setUserIdInput] = useState('');

  const handleAddMember = useCallback(async () => {
    const parsed = Number(userIdInput.trim());
    if (!userIdInput.trim() || Number.isNaN(parsed)) return;
    try {
      await addMember(parsed);
      setUserIdInput('');
    } catch {
      // error handled by hook
    }
  }, [userIdInput, addMember]);

  const handleRemoveMember = useCallback(
    async (userId: number) => {
      if (board && userId === board.ownerUserId) return;
      try {
        await removeMember(userId);
      } catch {
        // error handled by hook
      }
    },
    [board, removeMember],
  );

  if (isLoading && !board) {
    return (
      <View style={styles.center}>
        <ActivityIndicator
          size="large"
          color={colors.primary}
          testID="board-members-loading"
        />
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
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.content}
      testID="board-members-screen"
    >
      <View style={styles.membersSection}>
        {board.memberUserIds.map(userId => {
          const isOwner = userId === board.ownerUserId;
          return (
            <View key={userId} style={styles.memberRow}>
              <View style={styles.memberInfo}>
                <Text style={styles.memberId}>#{userId}</Text>
                {isOwner && (
                  <Text style={styles.ownerLabel}>
                    {t('lovingBoards.boardMembers.owner')}
                  </Text>
                )}
              </View>
              {!isOwner && (
                <Pressable
                  style={({ pressed }) => [
                    styles.removeBtn,
                    pressed && styles.pressed,
                  ]}
                  onPress={() => handleRemoveMember(userId)}
                  accessibilityRole="button"
                  accessibilityLabel={`${t(
                    'lovingBoards.boardMembers.remove',
                  )} #${userId}`}
                  testID={`member-remove-${userId}`}
                >
                  <Text style={styles.removeText}>
                    {t('lovingBoards.boardMembers.remove')}
                  </Text>
                </Pressable>
              )}
            </View>
          );
        })}
      </View>

      <View style={styles.addSection}>
        <TextInput
          style={styles.input}
          placeholder={t('lovingBoards.boardMembers.userIdPlaceholder')}
          placeholderTextColor={colors.textTertiary}
          value={userIdInput}
          onChangeText={setUserIdInput}
          keyboardType="number-pad"
          testID="board-members-userid-input"
        />
        <Pressable
          style={({ pressed }) => [styles.addBtn, pressed && styles.pressed]}
          onPress={handleAddMember}
          accessibilityRole="button"
          accessibilityLabel={t('lovingBoards.boardMembers.addMember')}
          testID="board-members-add"
        >
          <Text style={styles.addBtnText}>
            {t('lovingBoards.boardMembers.addMember')}
          </Text>
        </Pressable>
      </View>
    </ScrollView>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      flex: 1,
      backgroundColor: t.colors.background,
    },
    content: {
      padding: spacing.lg,
      gap: spacing.lg,
    },
    membersSection: {
      gap: spacing.sm,
    },
    memberRow: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      backgroundColor: t.colors.surface,
      borderRadius: borderRadius.md,
      paddingVertical: spacing.md,
      paddingHorizontal: spacing.lg,
      minHeight: MIN_TOUCH,
    },
    memberInfo: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: spacing.sm,
    },
    memberId: {
      ...t.typography.body,
      color: t.colors.textPrimary,
    },
    ownerLabel: {
      ...t.typography.caption,
      color: t.colors.primary,
    },
    removeBtn: {
      minHeight: MIN_TOUCH,
      minWidth: MIN_TOUCH,
      paddingHorizontal: spacing.sm,
      alignItems: 'center',
      justifyContent: 'center',
    },
    removeText: {
      ...t.typography.caption,
      color: t.colors.error,
    },
    addSection: {
      flexDirection: 'row',
      gap: spacing.sm,
      alignItems: 'center',
    },
    input: {
      flex: 1,
      minHeight: MIN_TOUCH,
      backgroundColor: t.colors.surface,
      borderWidth: 1,
      borderColor: t.colors.border,
      borderRadius: borderRadius.md,
      paddingHorizontal: spacing.md,
      ...t.typography.body,
    },
    addBtn: {
      minHeight: MIN_TOUCH,
      paddingHorizontal: spacing.lg,
      borderRadius: borderRadius.md,
      borderWidth: 1.5,
      borderColor: t.colors.primary,
      alignItems: 'center',
      justifyContent: 'center',
    },
    addBtnText: {
      ...t.typography.caption,
      color: t.colors.primary,
      fontWeight: '600',
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
    },
    pressed: {
      opacity: 0.7,
    },
  });

export default BoardMembersScreen;
