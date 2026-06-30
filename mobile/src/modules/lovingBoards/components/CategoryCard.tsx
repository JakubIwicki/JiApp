import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';

export type CategoryTint = 'info' | 'success' | 'warning' | 'primary';

interface CategoryCardProps {
  readonly categoryName: string;
  readonly categoryEmoji: string;
  readonly itemCount: number;
  readonly tint: CategoryTint;
  readonly isCollapsed: boolean;
  readonly onToggle: () => void;
  readonly children: React.ReactNode;
  readonly accessibilityLabel: string;
}

const ICON_SIZE = 36;

const CategoryCard: React.FC<CategoryCardProps> = ({
  categoryName,
  categoryEmoji,
  itemCount,
  tint,
  isCollapsed,
  onToggle,
  children,
  accessibilityLabel,
}) => {
  const styles = useThemedStyles(makeStyles);
  const tintColor = useThemedStyles((t: Theme) => t.colors[tint]);

  return (
    <View style={styles.card}>
      <Pressable
        style={({ pressed }) => [styles.header, pressed && styles.pressed]}
        onPress={onToggle}
        accessibilityRole="button"
        accessibilityLabel={accessibilityLabel}
        accessibilityState={{ expanded: !isCollapsed }}
      >
        <View style={styles.headerLeft}>
          <View
            style={[styles.iconBadge, { backgroundColor: `${tintColor}1F` }]}
          >
            <Text style={styles.iconText}>{categoryEmoji}</Text>
          </View>
          <Text style={styles.categoryName}>{categoryName}</Text>
          <View style={styles.countBadge}>
            <Text style={styles.countText}>{itemCount}</Text>
          </View>
        </View>
        <Text style={styles.chevron}>{isCollapsed ? '▶' : '▼'}</Text>
      </Pressable>

      {!isCollapsed && <View style={styles.items}>{children}</View>}
    </View>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    card: {
      backgroundColor: t.colors.surface,
      borderRadius: borderRadius.lg,
      boxShadow: '0 1px 4px rgba(26,26,26,0.04)',
      overflow: 'hidden',
    },
    header: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      paddingHorizontal: spacing.md,
      paddingVertical: spacing.sm,
      minHeight: 44,
    },
    headerLeft: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: spacing.sm,
    },
    iconBadge: {
      width: ICON_SIZE,
      height: ICON_SIZE,
      borderRadius: borderRadius.md,
      alignItems: 'center',
      justifyContent: 'center',
    },
    iconText: {
      fontSize: 18,
    },
    categoryName: {
      ...t.typography.body,
      fontWeight: '600',
      color: t.colors.textPrimary,
    },
    countBadge: {
      backgroundColor: t.colors.placeholder,
      borderRadius: borderRadius.sm,
      paddingHorizontal: spacing.sm,
      paddingVertical: 2,
    },
    countText: {
      ...t.typography.label,
      color: t.colors.textTertiary,
    },
    chevron: {
      fontSize: 12,
      color: t.colors.textTertiary,
    },
    items: {
      paddingHorizontal: spacing.sm,
      paddingBottom: spacing.sm,
      gap: spacing.xs,
    },
    pressed: {
      opacity: 0.7,
    },
  });

export default React.memo(CategoryCard);
