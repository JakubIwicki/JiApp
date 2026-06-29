import React, { useCallback, useReducer, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  Pressable,
  ScrollView,
  StyleSheet,
  Switch,
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

type Props = NativeStackScreenProps<LovingBoardsStackParamList, 'ItemSheet'>;

const MIN_TOUCH = 44;

interface FormState {
  title: string;
  quantity: string;
  category: string;
  note: string;
  assigneeUserId: string;
  dueDate: string;
  isRecurring: boolean;
  saving: boolean;
}

type FormAction =
  | { type: 'SET_FIELD'; field: keyof FormState; value: string | boolean }
  | { type: 'SET_SAVING'; value: boolean };

function formReducer(state: FormState, action: FormAction): FormState {
  switch (action.type) {
    case 'SET_FIELD':
      return { ...state, [action.field]: action.value };
    case 'SET_SAVING':
      return { ...state, saving: action.value };
    default:
      return state;
  }
}

const ItemSheet: React.FC<Props> = ({ route, navigation }) => {
  const { boardId, itemId } = route.params;
  const isEditing = itemId !== undefined;
  const { t } = useTranslation();
  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);
  const { board, addItem, updateItem, deleteItem } = useBoard(boardId);

  const existingItem = isEditing
    ? board?.items.find(i => i.id === itemId)
    : undefined;

  const initialState: FormState = {
    title: existingItem?.title ?? '',
    quantity: existingItem?.quantity ?? '',
    category: existingItem?.category ?? '',
    note: existingItem?.note ?? '',
    assigneeUserId: existingItem?.assigneeUserId?.toString() ?? '',
    dueDate: existingItem?.expiryDate?.split('T')[0] ?? '',
    isRecurring: existingItem?.isRecurring ?? false,
    saving: false,
  };

  const [form, dispatch] = useReducer(formReducer, initialState);
  const [titleError, setTitleError] = useState<string | undefined>();

  const setField = useCallback(
    (field: keyof FormState, value: string | boolean) => {
      dispatch({ type: 'SET_FIELD', field, value });
      if (field === 'title' && typeof value === 'string' && value.trim()) {
        setTitleError(undefined);
      }
    },
    [],
  );

  const handleSave = useCallback(async () => {
    const trimmedTitle = form.title.trim();
    if (!trimmedTitle) {
      setTitleError(t('lovingBoards.itemSheet.titleRequired'));
      return;
    }

    dispatch({ type: 'SET_SAVING', value: true });
    try {
      const payload = {
        title: trimmedTitle,
        quantity: form.quantity.trim() || null,
        category: form.category.trim() || null,
        note: form.note.trim() || null,
        assigneeUserId: form.assigneeUserId.trim()
          ? Number(form.assigneeUserId.trim())
          : null,
        expiryDate: form.dueDate.trim() || null,
        isRecurring: form.isRecurring,
      };

      if (isEditing && itemId !== undefined) {
        await updateItem(itemId, payload);
      } else {
        await addItem(payload);
      }
      navigation.goBack();
    } catch {
      // error handled by hook
    } finally {
      dispatch({ type: 'SET_SAVING', value: false });
    }
  }, [form, isEditing, itemId, addItem, updateItem, navigation, t]);

  const handleDelete = useCallback(() => {
    if (!isEditing || itemId === undefined) return;
    Alert.alert(
      t('lovingBoards.itemSheet.delete'),
      t('lovingBoards.itemSheet.deleteConfirm'),
      [
        { text: t('common.cancel'), style: 'cancel' },
        {
          text: t('lovingBoards.itemSheet.delete'),
          style: 'destructive',
          onPress: async () => {
            try {
              await deleteItem(itemId);
              navigation.goBack();
            } catch {
              // error handled by hook
            }
          },
        },
      ],
    );
  }, [isEditing, itemId, deleteItem, navigation, t]);

  const memberIds = board?.memberUserIds ?? [];

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.content}
      keyboardShouldPersistTaps="handled"
      testID="item-sheet-screen"
    >
      <Text style={styles.screenTitle}>
        {t(
          isEditing
            ? 'lovingBoards.itemSheet.editTitle'
            : 'lovingBoards.itemSheet.addTitle',
        )}
      </Text>

      {/* Title */}
      <Text style={styles.label}>{t('lovingBoards.itemSheet.titleLabel')}</Text>
      <TextInput
        style={[styles.input, titleError && styles.inputError]}
        placeholder={t('lovingBoards.itemSheet.titlePlaceholder')}
        placeholderTextColor={colors.textTertiary}
        value={form.title}
        onChangeText={v => setField('title', v)}
        maxLength={500}
        autoFocus={!isEditing}
        testID="item-title-input"
      />
      {titleError && <Text style={styles.errorMsg}>{titleError}</Text>}

      {/* Quantity */}
      <Text style={styles.label}>{t('lovingBoards.itemSheet.quantity')}</Text>
      <TextInput
        style={styles.input}
        placeholder={t('lovingBoards.itemSheet.quantityPlaceholder')}
        placeholderTextColor={colors.textTertiary}
        value={form.quantity}
        onChangeText={v => setField('quantity', v)}
        maxLength={100}
        testID="item-quantity-input"
      />

      {/* Category */}
      <Text style={styles.label}>{t('lovingBoards.itemSheet.category')}</Text>
      <TextInput
        style={styles.input}
        placeholder={t('lovingBoards.itemSheet.categoryPlaceholder')}
        placeholderTextColor={colors.textTertiary}
        value={form.category}
        onChangeText={v => setField('category', v)}
        maxLength={200}
        testID="item-category-input"
      />

      {/* Note */}
      <Text style={styles.label}>{t('lovingBoards.itemSheet.note')}</Text>
      <TextInput
        style={[styles.input, styles.inputMultiline]}
        placeholder={t('lovingBoards.itemSheet.notePlaceholder')}
        placeholderTextColor={colors.textTertiary}
        value={form.note}
        onChangeText={v => setField('note', v)}
        maxLength={2000}
        multiline
        numberOfLines={3}
        testID="item-note-input"
      />

      {/* Assignee */}
      <Text style={styles.label}>{t('lovingBoards.itemSheet.assignee')}</Text>
      <TextInput
        style={styles.input}
        placeholder={t('lovingBoards.itemSheet.unassigned')}
        placeholderTextColor={colors.textTertiary}
        value={form.assigneeUserId}
        onChangeText={v => setField('assigneeUserId', v)}
        keyboardType="number-pad"
        maxLength={10}
        testID="item-assignee-input"
      />
      {memberIds.length > 0 && (
        <View style={styles.chipRow}>
          {memberIds.map(uid => (
            <Pressable
              key={uid}
              style={({ pressed }) => [
                styles.assigneeChip,
                form.assigneeUserId === String(uid) &&
                  styles.assigneeChipActive,
                pressed && styles.pressed,
              ]}
              onPress={() =>
                setField(
                  'assigneeUserId',
                  form.assigneeUserId === String(uid) ? '' : String(uid),
                )
              }
              accessibilityRole="button"
              accessibilityLabel={`${t('lovingBoards.boardDetail.assignedTo', {
                id: uid,
              })}`}
              testID={`item-assignee-chip-${uid}`}
            >
              <Text
                style={[
                  styles.assigneeChipText,
                  form.assigneeUserId === String(uid) &&
                    styles.assigneeChipTextActive,
                ]}
              >
                #{uid}
              </Text>
            </Pressable>
          ))}
        </View>
      )}

      {/* Due Date */}
      <Text style={styles.label}>{t('lovingBoards.itemSheet.dueDate')}</Text>
      <TextInput
        style={styles.input}
        placeholder={t('lovingBoards.itemSheet.dueDatePlaceholder')}
        placeholderTextColor={colors.textTertiary}
        value={form.dueDate}
        onChangeText={v => setField('dueDate', v)}
        maxLength={10}
        testID="item-duedate-input"
      />

      {/* Repeat Weekly */}
      <View style={styles.switchRow}>
        <Text style={styles.label}>
          {t('lovingBoards.itemSheet.repeatWeekly')}
        </Text>
        <Switch
          value={form.isRecurring}
          onValueChange={v => setField('isRecurring', v)}
          trackColor={{ false: colors.placeholder, true: colors.primaryLight }}
          thumbColor={form.isRecurring ? colors.primary : colors.textTertiary}
          testID="item-recurring-switch"
        />
      </View>

      {/* Action Buttons */}
      <View style={styles.actions}>
        <Pressable
          style={({ pressed }) => [
            styles.saveBtn,
            pressed && styles.pressed,
            form.saving && styles.disabled,
          ]}
          onPress={handleSave}
          disabled={form.saving}
          accessibilityRole="button"
          accessibilityLabel={t('lovingBoards.itemSheet.save')}
          testID="item-save-button"
        >
          {form.saving ? (
            <ActivityIndicator color={colors.textInverse} size="small" />
          ) : (
            <Text style={styles.saveText}>
              {t('lovingBoards.itemSheet.save')}
            </Text>
          )}
        </Pressable>

        <Pressable
          style={({ pressed }) => [styles.cancelBtn, pressed && styles.pressed]}
          onPress={() => navigation.goBack()}
          accessibilityRole="button"
          accessibilityLabel={t('common.cancel')}
          testID="item-cancel-button"
        >
          <Text style={styles.cancelText}>{t('common.cancel')}</Text>
        </Pressable>

        {isEditing && (
          <Pressable
            style={({ pressed }) => [
              styles.deleteBtn,
              pressed && styles.pressed,
            ]}
            onPress={handleDelete}
            accessibilityRole="button"
            accessibilityLabel={t('lovingBoards.itemSheet.delete')}
            testID="item-delete-button"
          >
            <Text style={styles.deleteText}>
              {t('lovingBoards.itemSheet.delete')}
            </Text>
          </Pressable>
        )}
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
      gap: spacing.xs,
      paddingBottom: spacing.xxl,
    },
    screenTitle: {
      ...t.typography.heading,
      marginBottom: spacing.md,
    },
    label: {
      ...t.typography.caption,
      color: t.colors.textSecondary,
      marginTop: spacing.sm,
      marginBottom: spacing.xs,
    },
    input: {
      minHeight: MIN_TOUCH,
      backgroundColor: t.colors.surface,
      borderWidth: 1,
      borderColor: t.colors.border,
      borderRadius: borderRadius.md,
      paddingHorizontal: spacing.md,
      ...t.typography.body,
    },
    inputMultiline: {
      minHeight: 80,
      paddingTop: spacing.sm,
      textAlignVertical: 'top',
    },
    inputError: {
      borderColor: t.colors.error,
    },
    errorMsg: {
      ...t.typography.error,
      marginTop: spacing.xs,
    },
    chipRow: {
      flexDirection: 'row',
      flexWrap: 'wrap',
      gap: spacing.sm,
      marginTop: spacing.sm,
    },
    assigneeChip: {
      minHeight: MIN_TOUCH,
      minWidth: MIN_TOUCH,
      paddingHorizontal: spacing.md,
      paddingVertical: spacing.sm,
      borderRadius: borderRadius.xl,
      borderWidth: 1,
      borderColor: t.colors.border,
      alignItems: 'center',
      justifyContent: 'center',
    },
    assigneeChipActive: {
      borderColor: t.colors.primary,
      backgroundColor: t.colors.primaryLight,
    },
    assigneeChipText: {
      ...t.typography.caption,
      color: t.colors.textSecondary,
    },
    assigneeChipTextActive: {
      color: t.colors.primary,
      fontWeight: '600',
    },
    switchRow: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      marginTop: spacing.sm,
      minHeight: MIN_TOUCH,
    },
    actions: {
      marginTop: spacing.xl,
      gap: spacing.md,
    },
    saveBtn: {
      minHeight: MIN_TOUCH + 4,
      borderRadius: borderRadius.md,
      backgroundColor: t.colors.primary,
      alignItems: 'center',
      justifyContent: 'center',
    },
    saveText: {
      color: t.colors.textInverse,
      fontWeight: '600',
      fontSize: 16,
    },
    cancelBtn: {
      minHeight: MIN_TOUCH,
      alignItems: 'center',
      justifyContent: 'center',
    },
    cancelText: {
      ...t.typography.link,
      color: t.colors.textSecondary,
    },
    deleteBtn: {
      minHeight: MIN_TOUCH,
      alignItems: 'center',
      justifyContent: 'center',
      marginTop: spacing.md,
    },
    deleteText: {
      ...t.typography.body,
      color: t.colors.error,
    },
    pressed: {
      opacity: 0.7,
    },
    disabled: {
      opacity: 0.5,
    },
  });

export default ItemSheet;
