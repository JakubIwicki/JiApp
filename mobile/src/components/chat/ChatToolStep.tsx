import React, { useEffect } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import Animated, {
  useSharedValue,
  withTiming,
  useAnimatedStyle,
} from 'react-native-reanimated';
import { useTranslation } from 'react-i18next';
import { colors, spacing, borderRadius } from '../../styles/theme';
import type { ToolStep } from '../../types/chat';

interface ChatToolStepProps {
  readonly step: ToolStep;
}

// ── Tool code to i18n key mapping ──────────────────────────────────────────

const toolCodeMap: Record<string, string> = {
  search_youtube: 'searchYoutube',
  list_search_history: 'listSearchHistory',
  list_download_history: 'listDownloadHistory',
  offer_download: 'offerDownload',
};

// ── ChatToolStep ───────────────────────────────────────────────────────────

const ChatToolStep: React.FC<ChatToolStepProps> = ({ step }) => {
  const { t } = useTranslation();
  const isRunning = step.status === 'running';
  const toolKey = toolCodeMap[step.tool] ?? step.tool;

  const label = isRunning
    ? t(`chat.tool.${toolKey}.running`)
    : t(`chat.tool.${toolKey}.done`);

  // Fade-in + slight scale on mount
  const opacity = useSharedValue(0);
  const scale = useSharedValue(0.95);

  useEffect(() => {
    opacity.value = withTiming(1, { duration: 250 });
    scale.value = withTiming(1, { duration: 250 });
  }, [step.status, opacity, scale]);

  const animatedStyle = useAnimatedStyle(() => ({
    opacity: opacity.value,
    transform: [{ scale: scale.value }],
  }));

  return (
    <Animated.View
      style={[
        styles.pill,
        isRunning ? styles.runningPill : styles.donePill,
        animatedStyle,
      ]}
    >
      {!isRunning && <Text style={styles.checkmark}>{'✓'}</Text>}
      <Text
        style={[
          styles.label,
          isRunning ? styles.runningLabel : styles.doneLabel,
        ]}
      >
        {label}
      </Text>
      {isRunning && (
        <View style={styles.dotRow}>
          <View style={[styles.dot, styles.dot1]} />
          <View style={[styles.dot, styles.dot2]} />
          <View style={[styles.dot, styles.dot3]} />
        </View>
      )}
    </Animated.View>
  );
};

// ── Styles ─────────────────────────────────────────────────────────────────

const styles = StyleSheet.create({
  pill: {
    flexDirection: 'row',
    alignItems: 'center',
    alignSelf: 'flex-start',
    borderRadius: borderRadius.xl,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.xs,
    marginTop: spacing.xs,
  },
  runningPill: {
    backgroundColor: colors.successLight,
  },
  donePill: {
    backgroundColor: colors.primaryLight,
  },
  label: {
    fontSize: 13,
  },
  runningLabel: {
    color: colors.success,
  },
  doneLabel: {
    color: colors.textSecondary,
  },
  checkmark: {
    fontSize: 12,
    color: colors.textSecondary,
    marginRight: spacing.xs,
  },
  dotRow: {
    flexDirection: 'row',
    marginLeft: spacing.xs,
    gap: 2,
  },
  dot: {
    width: 4,
    height: 4,
    borderRadius: 2,
    backgroundColor: colors.success,
    opacity: 0.5,
  },
  dot1: {},
  dot2: {},
  dot3: {},
});

export default ChatToolStep;
