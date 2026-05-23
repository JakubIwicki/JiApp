import React, { useEffect, useRef } from 'react';
import {
  Animated,
  Image,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from 'react-native';
import type { VideoItem } from '../types/api';
import { colors, borderRadius, spacing } from '../styles/theme';

interface VideoCardProps {
  video: VideoItem;
  onPress: (video: VideoItem) => void;
}

const VideoCard: React.FC<VideoCardProps> = ({ video, onPress }) => {
  const fadeAnim = useRef(new Animated.Value(0)).current;
  const slideAnim = useRef(new Animated.Value(8)).current;

  useEffect(() => {
    Animated.timing(fadeAnim, {
      toValue: 1,
      duration: 300,
      useNativeDriver: true,
    }).start();

    Animated.timing(slideAnim, {
      toValue: 0,
      duration: 300,
      useNativeDriver: true,
    }).start();
  }, [fadeAnim, slideAnim]);

  const handlePress = () => {
    onPress(video);
  };

  return (
    <TouchableOpacity
      onPress={handlePress}
      activeOpacity={0.7}
      testID="video-card"
      accessibilityRole="button"
    >
      <Animated.View
        style={[
          styles.card,
          {
            opacity: fadeAnim,
            transform: [{ translateY: slideAnim }],
          },
        ]}
      >
        {video.imageUrl ? (
          <Image
            source={{ uri: video.imageUrl }}
            style={styles.thumbnail}
            testID="video-thumbnail"
            resizeMode="cover"
          />
        ) : (
          <View
            style={[styles.thumbnail, styles.placeholder]}
            testID="video-thumbnail-placeholder"
          />
        )}
        <View style={styles.info}>
          <Text style={styles.title} numberOfLines={1}>
            {video.title}
          </Text>
          <Text style={styles.meta} numberOfLines={1}>
            {video.channelTitle || video.description || ''}
          </Text>
        </View>
      </Animated.View>
    </TouchableOpacity>
  );
};

const styles = StyleSheet.create({
  card: {
    flexDirection: 'row',
    backgroundColor: colors.surface,
    borderRadius: borderRadius.lg,
    marginHorizontal: spacing.lg,
    marginVertical: 6,
    overflow: 'hidden',
    shadowColor: colors.cardShadow,
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 2,
  },
  thumbnail: {
    width: 120,
    height: 90,
    backgroundColor: colors.primaryLight,
  },
  placeholder: {
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: colors.primaryLight,
  },
  info: {
    flex: 1,
    padding: 10,
    justifyContent: 'center',
  },
  title: {
    fontSize: 14,
    fontWeight: '700',
    color: colors.textPrimary,
    marginBottom: spacing.xs,
  },
  meta: {
    fontSize: 13,
    color: colors.textTertiary,
    marginTop: 2,
  },
});

export default VideoCard;
