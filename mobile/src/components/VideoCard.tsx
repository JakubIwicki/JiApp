import React, { useEffect } from 'react';
import {
  Image,
  Pressable,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import Animated, { useSharedValue, withTiming, useAnimatedStyle } from 'react-native-reanimated';
import type { VideoItem } from '../types/api';
import { colors, borderRadius, spacing } from '../styles/theme';

interface VideoCardProps {
  video: VideoItem;
  onPress: (video: VideoItem) => void;
}

const VideoCard: React.FC<VideoCardProps> = ({ video, onPress }) => {
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

  const handlePress = () => {
    onPress(video);
  };

  return (
    <Pressable
      onPress={handlePress}
      style={({ pressed }) => pressed && { opacity: 0.7 }}
      testID="video-card"
      accessibilityRole="button"
    >
      <Animated.View
        style={[
          styles.card,
          animatedStyle,
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
    </Pressable>
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
    boxShadow: '0 1px 2px rgba(43,33,24,0.1)',
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
