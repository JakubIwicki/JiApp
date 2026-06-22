import React, { useCallback } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useHeaderHeight } from '@react-navigation/elements';
import { useTranslation } from 'react-i18next';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { KeyboardAvoidingView } from 'react-native-keyboard-controller';
import type { ChatStackParamList } from '../navigation/types';
import type { VideoItem } from '../types/api';
import ChatMessageList from '../components/chat/ChatMessageList';
import ChatInputBar from '../components/chat/ChatInputBar';
import useChat from '../hooks/useChat';
import useScreenTitle from '../hooks/useScreenTitle';
import { colors, commonStyles, spacing, borderRadius } from '../styles/theme';

type ChatNavigationProp = NativeStackNavigationProp<ChatStackParamList, 'Chat'>;

const ChatEmptyState: React.FC<{
  greeting: string;
  examples: string[];
  onExamplePress: (text: string) => void;
}> = ({ greeting, examples, onExamplePress }) => (
  <View style={styles.emptyContainer}>
    <Text style={styles.emptyEmoji}>{'💬'}</Text>
    <Text style={styles.emptyGreeting}>{greeting}</Text>
    <View style={styles.exampleRow}>
      {examples.map((example, idx) => (
        <Text
          key={idx}
          style={styles.exampleChip}
          onPress={() => onExamplePress(example)}
        >
          {example}
        </Text>
      ))}
    </View>
  </View>
);

const ChatScreen: React.FC = () => {
  const { t } = useTranslation();
  const navigation = useNavigation<ChatNavigationProp>();
  const { messages, isStreaming, error, send, confirmDownload } = useChat();
  const headerHeight = useHeaderHeight();

  useScreenTitle('chat.title');

  const handleSelectVideo = useCallback(
    (video: VideoItem) => {
      navigation.navigate('Download', video);
    },
    [navigation],
  );

  const handleConfirmDownload = useCallback(
    (messageId: string) => {
      confirmDownload(messageId);
    },
    [confirmDownload],
  );

  const handleExamplePress = useCallback(
    (text: string) => {
      send(text);
    },
    [send],
  );

  const examples: string[] = t('chat.empty.examples', {
    returnObjects: true,
  }) as unknown as string[];

  return (
    <KeyboardAvoidingView
      style={commonStyles.screenContainer}
      behavior="padding"
      keyboardVerticalOffset={headerHeight}
    >
      <ChatMessageList
        messages={messages}
        onSelectVideo={handleSelectVideo}
        onConfirmDownload={handleConfirmDownload}
        ListEmptyComponent={
          <ChatEmptyState
            greeting={t('chat.empty.greeting')}
            examples={examples}
            onExamplePress={handleExamplePress}
          />
        }
      />
      {error ? (
        <Text style={[commonStyles.apiError, styles.errorBanner]}>{error}</Text>
      ) : null}
      <ChatInputBar onSend={send} disabled={isStreaming} />
    </KeyboardAvoidingView>
  );
};

const styles = StyleSheet.create({
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: spacing.xxl,
    paddingTop: 80,
  },
  emptyEmoji: {
    fontSize: 64,
    opacity: 0.25,
    marginBottom: spacing.lg,
  },
  emptyGreeting: {
    fontSize: 15,
    color: colors.textSecondary,
    textAlign: 'center',
    lineHeight: 22,
    marginBottom: spacing.xl,
  },
  exampleRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'center',
    gap: spacing.sm,
  },
  exampleChip: {
    fontSize: 13,
    color: colors.primary,
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
    backgroundColor: colors.primaryLight,
    borderRadius: borderRadius.xl,
    overflow: 'hidden',
  },
  errorBanner: {
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.sm,
  },
});

export default ChatScreen;
