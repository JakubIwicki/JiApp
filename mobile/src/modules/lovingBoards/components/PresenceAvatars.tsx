import React, { useMemo } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useTheme, useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';

// ── Constants ──────────────────────────────────────────────────────────────

const MAX_VISIBLE = 3;
const AVATAR_SIZE = 28;
const OVERLAP = 10;
const DOT_SIZE = 8;

const AVATAR_COLOR_KEYS = ['primary', 'success', 'info', 'warning'] as const;

// ── Helpers ────────────────────────────────────────────────────────────────

function avatarAccent(userId: number, colors: Theme['colors']): string {
  return colors[AVATAR_COLOR_KEYS[userId % AVATAR_COLOR_KEYS.length]];
}

function avatarLabel(userId: number): string {
  return `#${String(userId % 100).padStart(2, '0')}`;
}

// ── Props ──────────────────────────────────────────────────────────────────

interface PresenceAvatarsProps {
  readonly userIds: number[];
}

// ── Component ──────────────────────────────────────────────────────────────

const PresenceAvatars: React.FC<PresenceAvatarsProps> = ({ userIds }) => {
  const { t } = useTranslation();
  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);

  const visible = useMemo(() => userIds.slice(0, MAX_VISIBLE), [userIds]);
  const overflow = userIds.length - MAX_VISIBLE;

  if (userIds.length === 0) return null;

  return (
    <Pressable
      style={styles.container}
      accessibilityRole="text"
      accessibilityLabel={t('lovingBoards.boardDetail.online', {
        count: userIds.length,
      })}
      testID="presence-avatars"
    >
      <View style={styles.avatarRow}>
        {visible.map((userId, index) => (
          <View
            key={userId}
            style={[
              styles.avatar,
              {
                backgroundColor: avatarAccent(userId, colors),
                marginLeft: index > 0 ? -OVERLAP : 0,
                zIndex: MAX_VISIBLE - index,
              },
            ]}
          >
            <Text style={styles.avatarText}>{avatarLabel(userId)}</Text>
            <View style={styles.dot} />
          </View>
        ))}
        {overflow > 0 && (
          <View style={[styles.avatar, styles.overflowAvatar]}>
            <Text style={styles.overflowText}>+{overflow}</Text>
          </View>
        )}
      </View>
      <Text style={styles.caption}>
        {t('lovingBoards.boardDetail.online', { count: userIds.length })}
      </Text>
    </Pressable>
  );
};

// ── Styles ─────────────────────────────────────────────────────────────────

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: spacing.sm,
      minHeight: 44,
      minWidth: 44,
      paddingHorizontal: spacing.sm,
    },
    avatarRow: {
      flexDirection: 'row',
    },
    avatar: {
      width: AVATAR_SIZE,
      height: AVATAR_SIZE,
      borderRadius: borderRadius.xl,
      alignItems: 'center',
      justifyContent: 'center',
      borderWidth: 1.5,
      borderColor: t.colors.background,
      position: 'relative',
    },
    avatarText: {
      fontSize: 9,
      fontWeight: '700',
      color: t.colors.textInverse,
    },
    dot: {
      position: 'absolute',
      bottom: -1,
      right: -1,
      width: DOT_SIZE,
      height: DOT_SIZE,
      borderRadius: DOT_SIZE / 2,
      backgroundColor: t.colors.success,
      borderWidth: 1.5,
      borderColor: t.colors.background,
    },
    overflowAvatar: {
      backgroundColor: t.colors.textTertiary,
    },
    overflowText: {
      fontSize: 9,
      fontWeight: '700',
      color: t.colors.textInverse,
    },
    caption: {
      fontSize: 12,
      color: t.colors.textTertiary,
      fontWeight: '500',
    },
  });

export default PresenceAvatars;
