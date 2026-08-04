import React, { useState } from 'react';
import {
  Text,
  Pressable,
  Modal,
  Alert,
  TextInput,
  StyleSheet,
  ActivityIndicator,
  View,
} from 'react-native';
import { useTranslation } from 'react-i18next';
import { useBoard } from '../hooks/useBoard';
import { useTheme, useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';
import type { Board } from '../types/api';

const BoardSelector: React.FC = () => {
  const {
    boards,
    selectedBoardId,
    isLoading,
    switchBoard,
    createBoard,
    deleteBoard,
  } = useBoard();
  const { t } = useTranslation();
  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);
  const [modalVisible, setModalVisible] = useState(false);
  const [showCreate, setShowCreate] = useState(false);
  const [newName, setNewName] = useState('');
  const [creating, setCreating] = useState(false);

  const selectedBoard = boards.find(b => b.id === selectedBoardId);

  const handleCreate = async () => {
    if (!newName.trim()) return;
    setCreating(true);
    try {
      await createBoard(newName.trim());
      setNewName('');
      setShowCreate(false);
      setModalVisible(false);
    } catch {
      Alert.alert(t('scheduler.error'), t('boardManagement.createError'));
    } finally {
      setCreating(false);
    }
  };

  const handleDelete = (board: Board) => {
    Alert.alert(
      t('scheduler.boardSelector.deleteTitle'),
      t('boardManagement.deleteConfirm', { name: board.name }),
      [
        { text: t('common.cancel'), style: 'cancel' },
        {
          text: t('boardManagement.delete'),
          style: 'destructive',
          onPress: () => deleteBoard(board.id),
        },
      ],
    );
  };

  return (
    <>
      <Pressable
        style={({ pressed }) => [styles.selector, pressed && { opacity: 0.7 }]}
        onPress={() => setModalVisible(true)}
      >
        {isLoading ? (
          <ActivityIndicator size="small" color={colors.primary} />
        ) : (
          <Text style={styles.selectorText}>
            {selectedBoard?.name ?? t('scheduler.boardSelector.noBoard')} ▾
          </Text>
        )}
      </Pressable>

      <Modal visible={modalVisible} animationType="slide" transparent>
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <Text style={styles.modalTitle}>{t('boardManagement.title')}</Text>

            {boards.length === 0 ? (
              <Text style={styles.emptyText}>{t('boardManagement.empty')}</Text>
            ) : (
              boards.map(board => (
                <View key={board.id} style={styles.boardRow}>
                  <Pressable
                    style={({ pressed }) => [
                      styles.boardInfo,
                      pressed && { opacity: 0.7 },
                    ]}
                    onPress={() => {
                      switchBoard(board.id);
                      setModalVisible(false);
                    }}
                  >
                    <Text style={styles.boardName}>
                      {board.id === selectedBoardId ? '✓ ' : ''}
                      {board.name}
                    </Text>
                    <Text style={styles.memberCount}>
                      {t('boardManagement.memberCount', {
                        count: board.memberUserIds.length,
                      })}
                    </Text>
                  </Pressable>
                  <Pressable
                    onPress={() => handleDelete(board)}
                    style={({ pressed }) => [
                      styles.deleteBtn,
                      pressed && { opacity: 0.7 },
                    ]}
                  >
                    <Text style={styles.deleteBtnText}>🗑</Text>
                  </Pressable>
                </View>
              ))
            )}

            {showCreate ? (
              <View style={styles.createRow}>
                <TextInput
                  style={styles.createInput}
                  placeholder={t('boardManagement.createPlaceholder')}
                  placeholderTextColor={colors.textTertiary}
                  value={newName}
                  onChangeText={setNewName}
                  maxLength={200}
                  autoFocus
                />
                <Pressable
                  onPress={handleCreate}
                  disabled={creating}
                  style={({ pressed }) => pressed && { opacity: 0.7 }}
                >
                  <Text style={styles.createBtnText}>
                    {creating ? '…' : t('boardManagement.create')}
                  </Text>
                </Pressable>
                <Pressable
                  onPress={() => {
                    setShowCreate(false);
                    setNewName('');
                  }}
                  style={({ pressed }) => pressed && { opacity: 0.7 }}
                >
                  <Text style={styles.cancelBtnText}>{t('common.cancel')}</Text>
                </Pressable>
              </View>
            ) : (
              <Pressable
                style={({ pressed }) => [
                  styles.createButton,
                  pressed && { opacity: 0.7 },
                ]}
                onPress={() => setShowCreate(true)}
              >
                <Text style={styles.createButtonText}>
                  {t('scheduler.boardSelector.createNew')}
                </Text>
              </Pressable>
            )}

            <Pressable
              style={({ pressed }) => [
                styles.closeButton,
                pressed && { opacity: 0.7 },
              ]}
              onPress={() => setModalVisible(false)}
            >
              <Text style={styles.closeButtonText}>
                {t('scheduler.boardSelector.close')}
              </Text>
            </Pressable>
          </View>
        </View>
      </Modal>
    </>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    selector: {
      paddingHorizontal: spacing.sm,
      paddingVertical: spacing.xs,
      alignItems: 'center',
      justifyContent: 'center',
    },
    selectorText: {
      ...t.typography.body,
      color: t.colors.primary,
      fontWeight: '600',
    },
    modalOverlay: {
      flex: 1,
      backgroundColor: 'rgba(0,0,0,0.4)',
      justifyContent: 'center',
      padding: spacing.lg,
    },
    modalContent: {
      backgroundColor: t.colors.surface,
      borderRadius: borderRadius.lg,
      padding: spacing.lg,
      maxHeight: '80%',
    },
    modalTitle: {
      ...t.typography.heading,
      marginBottom: spacing.md,
    },
    boardRow: {
      flexDirection: 'row',
      alignItems: 'center',
      paddingVertical: spacing.sm,
      borderBottomWidth: StyleSheet.hairlineWidth,
      borderBottomColor: t.colors.separator,
    },
    boardInfo: {
      flex: 1,
    },
    boardName: {
      ...t.typography.body,
      fontWeight: '600',
      color: t.colors.textPrimary,
    },
    memberCount: {
      ...t.typography.caption,
      color: t.colors.textTertiary,
    },
    deleteBtn: {
      padding: spacing.sm,
    },
    deleteBtnText: {
      fontSize: 16,
    },
    createRow: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: spacing.sm,
      marginTop: spacing.md,
    },
    createInput: {
      flex: 1,
      borderWidth: 1,
      borderColor: t.colors.border,
      borderRadius: borderRadius.sm,
      paddingHorizontal: spacing.sm,
      paddingVertical: spacing.xs,
      ...t.typography.body,
    },
    createBtnText: {
      color: t.colors.primary,
      fontWeight: '600',
    },
    cancelBtnText: {
      color: t.colors.textTertiary,
    },
    createButton: {
      marginTop: spacing.md,
      paddingVertical: spacing.sm,
      alignItems: 'center',
    },
    createButtonText: {
      ...t.typography.body,
      color: t.colors.primary,
      fontWeight: '600',
    },
    closeButton: {
      marginTop: spacing.lg,
      alignItems: 'center',
      paddingVertical: spacing.sm,
      borderTopWidth: StyleSheet.hairlineWidth,
      borderTopColor: t.colors.separator,
    },
    closeButtonText: {
      ...t.typography.body,
      color: t.colors.textSecondary,
    },
    emptyText: {
      ...t.typography.body,
      color: t.colors.textTertiary,
      textAlign: 'center',
      paddingVertical: spacing.xl,
    },
  });

export default BoardSelector;
