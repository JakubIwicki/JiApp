import React, { useCallback } from 'react';
import { StyleSheet, View } from 'react-native';
import VideoCard from '../VideoCard';
import { spacing } from '../../styles/theme';
import type { VideoItem } from '../../types/api';

interface ChatVideoResultsProps {
  readonly videos: VideoItem[];
  readonly onSelect: (video: VideoItem) => void;
}

const ChatVideoResults: React.FC<ChatVideoResultsProps> = ({
  videos,
  onSelect,
}) => {
  const renderVideo = useCallback(
    (video: VideoItem) => (
      <View key={video.videoId} style={styles.videoWrapper}>
        <VideoCard video={video} onPress={onSelect} />
      </View>
    ),
    [onSelect],
  );

  return <View style={styles.container}>{videos.map(renderVideo)}</View>;
};

const styles = StyleSheet.create({
  container: {
    marginTop: spacing.sm,
  },
  videoWrapper: {
    marginVertical: 2,
  },
});

export default ChatVideoResults;
