import React, { useEffect } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import Animated, {
  useSharedValue,
  withTiming,
  withRepeat,
  useAnimatedStyle,
} from 'react-native-reanimated';
import Markdown from 'react-native-markdown-display';
import { colors, typography, spacing, borderRadius } from '../../styles/theme';
import type { ChatMessage } from '../../types/chat';

interface ChatBubbleProps {
  readonly message: ChatMessage;
}

// ── Markdown styles (only for assistant messages) ──────────────────────────

const markdownStyles = {
  body: { ...typography.body, color: colors.textPrimary },
  heading1: {
    fontSize: 20,
    fontWeight: '700' as const,
    color: colors.textPrimary,
    marginVertical: spacing.sm,
  },
  heading2: {
    fontSize: 18,
    fontWeight: '700' as const,
    color: colors.textPrimary,
    marginVertical: spacing.xs,
  },
  heading3: {
    fontSize: 16,
    fontWeight: '700' as const,
    color: colors.textPrimary,
    marginVertical: spacing.xs,
  },
  heading4: {
    fontSize: 15,
    fontWeight: '700' as const,
    color: colors.textPrimary,
  },
  heading5: {
    fontSize: 14,
    fontWeight: '700' as const,
    color: colors.textPrimary,
  },
  heading6: {
    fontSize: 13,
    fontWeight: '700' as const,
    color: colors.textPrimary,
  },
  strong: { fontWeight: '700' as const },
  em: { fontStyle: 'italic' as const },
  paragraph: { marginTop: 0, marginBottom: spacing.xs },
  bullet_list: {},
  ordered_list: {},
  list_item: { marginVertical: 2 },
  code_inline: {
    ...typography.monospace,
    backgroundColor: colors.placeholder,
    borderRadius: 3,
    paddingHorizontal: 4,
  },
  fence: {
    ...typography.monospace,
    backgroundColor: colors.placeholder,
    padding: spacing.sm,
    borderRadius: borderRadius.sm,
  },
  blockquote: {
    borderLeftWidth: 3,
    borderLeftColor: colors.border,
    paddingLeft: spacing.sm,
    marginVertical: spacing.xs,
  },
  link: { color: colors.primary, textDecorationLine: 'underline' as const },
};

// ── Typing indicator (gently pulsing dots) ─────────────────────────────────

const TypingDots: React.FC = () => {
  const opacity = useSharedValue(0.3);

  useEffect(() => {
    opacity.value = withRepeat(withTiming(1, { duration: 600 }), -1, true);
  }, [opacity]);

  const animatedStyle = useAnimatedStyle(() => ({
    opacity: opacity.value,
  }));

  return (
    <Animated.Text
      style={[styles.typingDots, animatedStyle]}
      testID="typing-indicator"
    >
      {'...'}
    </Animated.Text>
  );
};

// ── Streaming caret ────────────────────────────────────────────────────────

const StreamCaret: React.FC = () => {
  const opacity = useSharedValue(0.3);

  useEffect(() => {
    opacity.value = withRepeat(withTiming(1, { duration: 500 }), -1, true);
  }, [opacity]);

  const animatedStyle = useAnimatedStyle(() => ({
    opacity: opacity.value,
  }));

  return (
    <Animated.Text style={[styles.caret, animatedStyle]}>{'▍'}</Animated.Text>
  );
};

// ── ChatBubble ─────────────────────────────────────────────────────────────

const ChatBubble: React.FC<ChatBubbleProps> = ({ message }) => {
  const isUser = message.role === 'user';
  const isAssistant = message.role === 'assistant';
  const hasText = message.text.length > 0;
  const hasToolSteps = (message.toolSteps?.length ?? 0) > 0;
  const showTyping = message.pending && !hasText && !hasToolSteps;
  const showCaret = message.pending && hasText;

  // Entrance animation (fade + rise)
  const fadeAnim = useSharedValue(0);
  const slideAnim = useSharedValue(8);

  useEffect(() => {
    fadeAnim.value = withTiming(1, { duration: 300 });
    slideAnim.value = withTiming(0, { duration: 300 });
  }, [fadeAnim, slideAnim]);

  const animatedStyle = useAnimatedStyle(() => ({
    opacity: fadeAnim.value,
    transform: [{ translateY: slideAnim.value }],
  }));

  const bubbleStyle = [
    styles.base,
    isAssistant && styles.assistantBubble,
    isUser && styles.userBubble,
    animatedStyle,
  ];

  const textStyle = [
    typography.body,
    isAssistant && { color: colors.textPrimary },
    isUser && { color: colors.textPrimary },
  ];

  return (
    <View style={[styles.row, isAssistant ? styles.rowLeft : styles.rowRight]}>
      <Animated.View style={bubbleStyle}>
        {hasText &&
          (isAssistant ? (
            <Markdown style={markdownStyles}>{message.text}</Markdown>
          ) : (
            <Text style={textStyle}>{message.text}</Text>
          ))}
        {showTyping && (
          <View style={styles.typingRow}>
            <TypingDots />
          </View>
        )}
        <View style={styles.caretRow}>{showCaret && <StreamCaret />}</View>
      </Animated.View>
    </View>
  );
};

// ── Styles ─────────────────────────────────────────────────────────────────

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    marginVertical: 4,
    paddingHorizontal: spacing.lg,
  },
  rowLeft: {
    justifyContent: 'flex-start',
  },
  rowRight: {
    justifyContent: 'flex-end',
  },
  base: {
    maxWidth: '82%',
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.md,
  },
  assistantBubble: {
    backgroundColor: colors.surface,
    borderTopLeftRadius: borderRadius.lg,
    borderTopRightRadius: borderRadius.lg,
    borderBottomRightRadius: borderRadius.lg,
    borderBottomLeftRadius: borderRadius.sm,
    boxShadow: '0 1px 2px rgba(43,33,24,0.1)',
  },
  userBubble: {
    backgroundColor: colors.primaryLight,
    borderTopLeftRadius: borderRadius.lg,
    borderTopRightRadius: borderRadius.lg,
    borderBottomLeftRadius: borderRadius.lg,
    borderBottomRightRadius: borderRadius.sm,
  },
  typingRow: {
    flexDirection: 'row',
    alignItems: 'center',
    minHeight: 20,
  },
  typingDots: {
    fontSize: 14,
    color: colors.textTertiary,
    lineHeight: 20,
  },
  caretRow: {
    position: 'absolute',
    bottom: spacing.md,
    right: spacing.md,
  },
  caret: {
    fontSize: 14,
    color: colors.primary,
    lineHeight: 20,
  },
});

export default ChatBubble;
