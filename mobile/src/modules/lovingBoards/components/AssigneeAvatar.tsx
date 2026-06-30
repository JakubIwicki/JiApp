import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';

interface AssigneeAvatarProps {
  readonly userId: number;
  readonly size?: number;
}

const AVATAR_COLORS = ['info', 'success', 'primary'] as const;

function pickColor(userId: number, t: Theme): string {
  const idx = Math.abs(userId) % AVATAR_COLORS.length;
  return t.colors[AVATAR_COLORS[idx]];
}

function initials(userId: number): string {
  const s = String(userId);
  if (s.length >= 2) return s.slice(0, 2).toUpperCase();
  return s.toUpperCase();
}

const AssigneeAvatar: React.FC<AssigneeAvatarProps> = ({
  userId,
  size = 26,
}) => {
  const styles = useThemedStyles(makeStyles);

  const bgColor = useThemedStyles((t: Theme) => pickColor(userId, t));

  return (
    <View style={styles.arrow}>
      <Text style={styles.arrowText}>→</Text>
      <View
        style={[
          styles.circle,
          {
            width: size,
            height: size,
            borderRadius: size / 2,
            backgroundColor: bgColor,
          },
        ]}
      >
        <Text style={[styles.initials, { fontSize: size * 0.42 }]}>
          {initials(userId)}
        </Text>
      </View>
    </View>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    arrow: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: 2,
    },
    arrowText: {
      fontSize: 10,
      color: t.colors.textTertiary,
    },
    circle: {
      alignItems: 'center',
      justifyContent: 'center',
    },
    initials: {
      color: t.colors.textInverse,
      fontWeight: '700',
    },
  });

export default React.memo(AssigneeAvatar);
