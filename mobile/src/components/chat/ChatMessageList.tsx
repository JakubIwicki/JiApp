import React, { useCallback } from 'react';
import { FlatList, StyleSheet, View } from 'react-native';
import ChatBubble from './ChatBubble';
import ChatToolStep from './ChatToolStep';
import ChatVideoResults from './ChatVideoResults';
import ChatDownloadOffer from './ChatDownloadOffer';
import { spacing } from '../../styles/theme';
import type { ChatMessage } from '../../types/chat';
import type { VideoItem } from '../../types/api';

interface ChatMessageListProps {
  readonly messages: ChatMessage[];
  readonly onSelectVideo?: (video: VideoItem) => void;
  readonly onConfirmDownload?: (messageId: string) => void;
  readonly ListEmptyComponent?: React.ReactElement;
}

const ChatMessageList: React.FC<ChatMessageListProps> = ({
  messages,
  onSelectVideo,
  onConfirmDownload,
  ListEmptyComponent,
}) => {
  const keyExtractor = useCallback((item: ChatMessage) => item.id, []);

  const renderItem = useCallback(
    ({ item }: { item: ChatMessage }) => (
      <View style={styles.messageGroup}>
        <ChatBubble message={item} />
        {item.toolSteps &&
          item.toolSteps.map((step, idx) => (
            <View key={`${step.tool}-${idx}`} style={styles.toolRow}>
              <ChatToolStep step={step} />
            </View>
          ))}
        {item.videos && item.videos.length > 0 && (
          <ChatVideoResults
            videos={item.videos}
            onSelect={onSelectVideo ?? (() => {})}
          />
        )}
        {item.offer && (
          <ChatDownloadOffer
            offer={item.offer}
            status={item.offerStatus ?? 'idle'}
            onConfirm={
              onConfirmDownload ? () => onConfirmDownload(item.id) : () => {}
            }
          />
        )}
      </View>
    ),
    [onSelectVideo, onConfirmDownload],
  );

  return (
    <FlatList
      data={messages}
      keyExtractor={keyExtractor}
      renderItem={renderItem}
      contentContainerStyle={styles.listContent}
      inverted={false}
      keyboardShouldPersistTaps="handled"
      ListEmptyComponent={ListEmptyComponent}
    />
  );
};

const styles = StyleSheet.create({
  listContent: {
    paddingVertical: spacing.sm,
  },
  messageGroup: {
    marginBottom: spacing.xs,
  },
  toolRow: {
    paddingHorizontal: spacing.lg,
  },
});

export default ChatMessageList;
