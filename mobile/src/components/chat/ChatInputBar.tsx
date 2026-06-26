import React, { useCallback, useState } from 'react';
import { Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useTheme, useThemedStyles } from '../../context/ThemeContext';
import type { Theme } from '../../styles/theme';
import { spacing, borderRadius } from '../../styles/theme';

interface ChatInputBarProps {
  readonly onSend: (text: string) => void;
  readonly disabled?: boolean;
}

const ChatInputBar: React.FC<ChatInputBarProps> = ({
  onSend,
  disabled = false,
}) => {
  const { t } = useTranslation();
  const [text, setText] = useState('');
  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);

  const handleSend = useCallback(() => {
    const trimmed = text.trim();
    if (trimmed.length === 0 || disabled) return;
    onSend(trimmed);
    setText('');
  }, [text, disabled, onSend]);

  const canSend = text.trim().length > 0 && !disabled;

  return (
    <View style={styles.container}>
      <View style={styles.inputRow}>
        <TextInput
          style={styles.input}
          value={text}
          onChangeText={setText}
          placeholder={t('chat.inputPlaceholder')}
          placeholderTextColor={colors.placeholderDark}
          multiline
          maxLength={2000}
          textAlignVertical="center"
          editable={!disabled}
          returnKeyType="send"
          onSubmitEditing={handleSend}
          blurOnSubmit={false}
          testID="chat-input"
        />
        <Pressable
          onPress={handleSend}
          disabled={!canSend}
          style={({ pressed }) => [
            styles.sendButton,
            !canSend && styles.sendButtonDisabled,
            pressed && canSend && { opacity: 0.7 },
          ]}
          accessibilityRole="button"
          accessibilityLabel={t('chat.send')}
          testID="chat-send-button"
        >
          <Text style={[styles.sendIcon, !canSend && styles.sendIconDisabled]}>
            {'▲'}
          </Text>
        </Pressable>
      </View>
    </View>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      paddingHorizontal: spacing.lg,
      paddingVertical: spacing.sm,
      backgroundColor: t.colors.background,
    },
    inputRow: {
      flexDirection: 'row',
      alignItems: 'flex-end',
      backgroundColor: t.colors.surface,
      borderRadius: borderRadius.xl,
      borderWidth: 1,
      borderColor: t.colors.border,
      paddingHorizontal: spacing.md,
      paddingVertical: spacing.xs,
    },
    input: {
      flex: 1,
      fontSize: 16,
      color: t.colors.textPrimary,
      maxHeight: 100,
      paddingVertical: spacing.sm,
    },
    sendButton: {
      width: 36,
      height: 36,
      borderRadius: 18,
      backgroundColor: t.colors.primary,
      justifyContent: 'center',
      alignItems: 'center',
      marginLeft: spacing.sm,
      marginBottom: spacing.xs,
    },
    sendButtonDisabled: {
      backgroundColor: t.colors.border,
    },
    sendIcon: {
      fontSize: 14,
      color: t.colors.textInverse,
      fontWeight: '700',
    },
    sendIconDisabled: {
      color: t.colors.textTertiary,
    },
  });

export default ChatInputBar;
