import React, { useCallback } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';
import type { Item } from '../types/api';
import CircularCheckbox from './CircularCheckbox';
import AssigneeAvatar from './AssigneeAvatar';
import PillBadge from './PillBadge';

interface ItemRowProps {
  readonly item: Item;
  readonly currentUserId?: number;
  readonly onToggle: (item: Item) => void;
  readonly onEdit: (item: Item) => void;
  readonly onRemove: (item: Item) => void;
}

const MIN_TOUCH = 44;

function parseDueDate(expiryDate: string | null): Date | null {
  if (!expiryDate) return null;
  const d = new Date(expiryDate);
  return Number.isNaN(d.getTime()) ? null : d;
}

function getDueUrgency(
  expiryDate: string | null,
): 'none' | 'tomorrow' | 'overdue' {
  const due = parseDueDate(expiryDate);
  if (!due) return 'none';
  const now = new Date();
  now.setHours(0, 0, 0, 0);
  due.setHours(0, 0, 0, 0);
  if (due < now) return 'overdue';
  const tomorrow = new Date(now);
  tomorrow.setDate(tomorrow.getDate() + 1);
  if (due.getTime() === tomorrow.getTime()) return 'tomorrow';
  return 'none';
}

const ItemRow: React.FC<ItemRowProps> = ({
  item,
  currentUserId,
  onToggle,
  onEdit,
  onRemove,
}) => {
  const { t } = useTranslation();
  const styles = useThemedStyles(makeStyles);
  const isCompleted = item.status === 'Completed';
  const isRemoved = item.status === 'Removed';
  const isOwnCompletion =
    isCompleted &&
    currentUserId !== undefined &&
    item.completedByUserId === currentUserId;
  const urgency = getDueUrgency(item.expiryDate);

  const handleToggle = useCallback(() => onToggle(item), [item, onToggle]);
  const handleEdit = useCallback(() => onEdit(item), [item, onEdit]);
  const handleRemove = useCallback(() => onRemove(item), [item, onRemove]);

  if (isRemoved) {
    return (
      <View style={[styles.row, styles.rowRemoved]}>
        <View style={styles.itemBody}>
          <Text style={styles.titleRemoved} numberOfLines={2}>
            {item.title}
          </Text>
        </View>
      </View>
    );
  }

  return (
    <View style={styles.row}>
      <CircularCheckbox
        status={item.status}
        isOwnCompletion={isOwnCompletion}
        onToggle={handleToggle}
        accessibilityLabel={item.title}
        testID={`item-check-${item.id}`}
      />

      <Pressable
        style={styles.itemBody}
        onPress={handleEdit}
        accessibilityRole="button"
        accessibilityLabel={`${t('lovingBoards.boardDetail.edit')} ${
          item.title
        }`}
        testID={`item-body-${item.id}`}
      >
        <Text
          style={[styles.title, isCompleted && styles.titleDone]}
          numberOfLines={2}
        >
          {item.title}
        </Text>

        <Text style={styles.caption}>
          {isCompleted
            ? t('lovingBoards.boardDetail.boughtBy', {
                id: item.completedByUserId ?? 0,
              })
            : t('lovingBoards.boardDetail.addedBy', { id: item.addedByUserId })}
        </Text>
      </Pressable>

      {!isCompleted && (
        <View style={styles.pills}>
          {item.quantity && (
            <PillBadge
              text={t('lovingBoards.boardDetail.qty', { qty: item.quantity })}
            />
          )}
          {item.isRecurring && (
            <PillBadge
              text="🔁"
              variant="recurring"
              accessibilityLabel={t('lovingBoards.boardDetail.recurring')}
            />
          )}
          {urgency === 'tomorrow' && (
            <PillBadge
              text={`⚠ ${t('lovingBoards.boardDetail.dueTomorrow')}`}
              variant="warning"
            />
          )}
          {urgency === 'overdue' && (
            <PillBadge
              text={t('lovingBoards.boardDetail.overdue')}
              variant="error"
            />
          )}
          {item.assigneeUserId !== null && (
            <AssigneeAvatar userId={item.assigneeUserId} />
          )}
        </View>
      )}

      {isCompleted && item.assigneeUserId !== null && (
        <AssigneeAvatar userId={item.assigneeUserId} />
      )}

      <View style={styles.actions}>
        <Pressable
          style={styles.actionBtn}
          onPress={handleEdit}
          accessibilityRole="button"
          accessibilityLabel={`${t('lovingBoards.boardDetail.edit')} ${
            item.title
          }`}
          testID={`item-edit-${item.id}`}
        >
          <Text style={styles.actionIcon}>✏️</Text>
        </Pressable>
        <Pressable
          style={styles.actionBtn}
          onPress={handleRemove}
          accessibilityRole="button"
          accessibilityLabel={`${t('lovingBoards.boardDetail.remove')} ${
            item.title
          }`}
          testID={`item-remove-${item.id}`}
        >
          <Text style={styles.actionIcon}>🗑️</Text>
        </Pressable>
      </View>
    </View>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    row: {
      flexDirection: 'row',
      alignItems: 'flex-start',
      backgroundColor: t.colors.surface,
      borderRadius: borderRadius.md,
      paddingVertical: spacing.md,
      paddingLeft: spacing.xs,
      paddingRight: spacing.xs,
    },
    rowRemoved: {
      opacity: 0.5,
    },
    itemBody: {
      flex: 1,
      paddingHorizontal: spacing.sm,
      gap: 2,
      minHeight: MIN_TOUCH,
      justifyContent: 'center',
    },
    title: {
      fontSize: 14,
      fontWeight: '600',
      color: t.colors.textPrimary,
    },
    titleDone: {
      textDecorationLine: 'line-through',
      color: t.colors.textTertiary,
      fontWeight: '400',
    },
    titleRemoved: {
      fontSize: 14,
      color: t.colors.textTertiary,
      textDecorationLine: 'line-through',
    },
    caption: {
      fontSize: 11,
      color: t.colors.textTertiary,
    },
    pills: {
      flexDirection: 'row',
      flexWrap: 'wrap',
      alignItems: 'center',
      gap: spacing.xs,
      paddingRight: spacing.xs,
    },
    actions: {
      flexDirection: 'column',
      alignItems: 'center',
      gap: 2,
      paddingLeft: spacing.xs,
    },
    actionBtn: {
      minWidth: MIN_TOUCH,
      minHeight: 32,
      alignItems: 'center',
      justifyContent: 'center',
    },
    actionIcon: {
      fontSize: 14,
    },
  });

export default React.memo(ItemRow);
