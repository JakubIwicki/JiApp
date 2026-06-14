import React, { useCallback, useEffect, useMemo } from 'react';
import { ScrollView, StyleSheet, Text, View, Pressable } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import Animated, {
  useSharedValue,
  withSpring,
  withDelay,
  withTiming,
  useAnimatedStyle,
} from 'react-native-reanimated';
import Svg, { Circle, Line, Path, Polyline, Rect } from 'react-native-svg';
import useAuth from '../hooks/useAuth';
import {
  animation,
  borderRadius,
  colors,
  spacing,
  typography,
} from '../styles/theme';
import type { ModuleId } from '../navigation/types';

interface ModuleSelectionScreenProps {
  /** Called when a granted module card is tapped. */
  readonly onSelectModule?: (moduleId: ModuleId) => void;
}

interface ModuleMeta {
  readonly id: ModuleId;
  readonly nameKey: string;
  readonly descriptionKey: string;
  readonly accent: string;
}

// Order here defines the visual order of the cards.
const MODULE_META: readonly ModuleMeta[] = [
  {
    id: 'YtDownloader',
    nameKey: 'modules.ytDownloader.name',
    descriptionKey: 'modules.ytDownloader.description',
    accent: colors.info,
  },
  {
    id: 'Scheduler',
    nameKey: 'modules.scheduler.name',
    descriptionKey: 'modules.scheduler.description',
    accent: colors.success,
  },
];

const GLYPH_SIZE = 26;

const ModuleGlyph: React.FC<{ id: ModuleId; color: string }> = ({
  id,
  color,
}) => {
  if (id === 'YtDownloader') {
    return (
      <Svg
        width={GLYPH_SIZE}
        height={GLYPH_SIZE}
        viewBox="0 0 24 24"
        fill="none"
        testID="module-glyph-YtDownloader"
      >
        <Path
          d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"
          stroke={color}
          strokeWidth={2}
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <Polyline
          points="7 10 12 15 17 10"
          stroke={color}
          strokeWidth={2}
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <Line
          x1={12}
          y1={15}
          x2={12}
          y2={3}
          stroke={color}
          strokeWidth={2}
          strokeLinecap="round"
        />
      </Svg>
    );
  }

  return (
    <Svg
      width={GLYPH_SIZE}
      height={GLYPH_SIZE}
      viewBox="0 0 24 24"
      fill="none"
      testID="module-glyph-Scheduler"
    >
      <Rect
        x={3}
        y={4}
        width={18}
        height={17}
        rx={2}
        stroke={color}
        strokeWidth={2}
      />
      <Line x1={3} y1={9} x2={21} y2={9} stroke={color} strokeWidth={2} />
      <Line
        x1={8}
        y1={2}
        x2={8}
        y2={6}
        stroke={color}
        strokeWidth={2}
        strokeLinecap="round"
      />
      <Line
        x1={16}
        y1={2}
        x2={16}
        y2={6}
        stroke={color}
        strokeWidth={2}
        strokeLinecap="round"
      />
      <Circle cx={12} cy={15} r={1.6} fill={color} />
    </Svg>
  );
};

const ChevronGlyph: React.FC = () => (
  <Svg width={18} height={18} viewBox="0 0 24 24" fill="none">
    <Polyline
      points="9 6 15 12 9 18"
      stroke={colors.textTertiary}
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
    />
  </Svg>
);

interface ModuleCardProps {
  readonly meta: ModuleMeta;
  readonly index: number;
  readonly title: string;
  readonly description: string;
  readonly onPress: (moduleId: ModuleId) => void;
}

const ModuleCard: React.FC<ModuleCardProps> = ({
  meta,
  index,
  title,
  description,
  onPress,
}) => {
  const scale = useSharedValue(1);
  const enter = useSharedValue(0);

  useEffect(() => {
    const delay =
      animation.stagger.initialDelay + index * animation.stagger.itemDelay;
    enter.value = withDelay(
      delay,
      withTiming(1, { duration: animation.duration.normal }),
    );
  }, [enter, index]);

  const handlePressIn = useCallback(() => {
    scale.value = withSpring(0.96, animation.spring.bouncy);
  }, [scale]);

  const handlePressOut = useCallback(() => {
    scale.value = withSpring(1, animation.spring.bouncy);
  }, [scale]);

  const handlePress = useCallback(() => {
    onPress(meta.id);
  }, [onPress, meta.id]);

  const animatedStyle = useAnimatedStyle(() => ({
    opacity: enter.value,
    transform: [{ translateY: (1 - enter.value) * 16 }, { scale: scale.value }],
  }));

  return (
    <Animated.View style={animatedStyle}>
      <Pressable
        onPress={handlePress}
        onPressIn={handlePressIn}
        onPressOut={handlePressOut}
        style={({ pressed }) => [styles.card, pressed && styles.cardPressed]}
        accessibilityRole="button"
        accessibilityLabel={title}
        testID={`module-card-${meta.id}`}
      >
        <View
          style={[styles.glyphWrap, { backgroundColor: `${meta.accent}1A` }]}
        >
          <ModuleGlyph id={meta.id} color={meta.accent} />
        </View>
        <View style={styles.cardText}>
          <Text style={styles.cardTitle}>{title}</Text>
          <Text style={styles.cardDescription}>{description}</Text>
        </View>
        <ChevronGlyph />
      </Pressable>
    </Animated.View>
  );
};

const ModuleSelectionScreen: React.FC<ModuleSelectionScreenProps> = ({
  onSelectModule,
}) => {
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();
  const { availableModules, displayName } = useAuth();

  const grantedMeta = useMemo(
    () => MODULE_META.filter(m => availableModules.includes(m.id)),
    [availableModules],
  );

  const handleSelect = useCallback(
    (moduleId: ModuleId) => {
      onSelectModule?.(moduleId);
    },
    [onSelectModule],
  );

  const greeting = displayName
    ? t('modules.greeting', { name: displayName })
    : t('modules.greetingFallback');

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={[
        styles.content,
        { paddingTop: insets.top + spacing.xxl },
      ]}
      testID="module-selection-screen"
    >
      <View style={styles.header}>
        <Text style={styles.greeting} testID="module-greeting">
          {greeting}
        </Text>
        <Text style={styles.subtitle}>{t('modules.subtitle')}</Text>
      </View>

      <View style={styles.cards}>
        {grantedMeta.map((meta, index) => (
          <ModuleCard
            key={meta.id}
            meta={meta}
            index={index}
            title={t(meta.nameKey)}
            description={t(meta.descriptionKey)}
            onPress={handleSelect}
          />
        ))}
      </View>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  content: {
    paddingHorizontal: spacing.xl,
    paddingBottom: spacing.xxl,
  },
  header: {
    marginBottom: spacing.xxl,
  },
  greeting: {
    ...typography.title,
    marginBottom: spacing.sm,
  },
  subtitle: {
    ...typography.body,
    color: colors.textSecondary,
  },
  cards: {
    gap: spacing.lg,
  },
  card: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: colors.surface,
    borderRadius: borderRadius.lg,
    paddingVertical: spacing.lg,
    paddingHorizontal: spacing.lg,
    minHeight: 80,
    boxShadow: '0 6px 16px rgba(43,33,24,0.10)',
  },
  cardPressed: {
    opacity: 0.9,
  },
  glyphWrap: {
    width: 48,
    height: 48,
    borderRadius: borderRadius.lg,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: spacing.lg,
  },
  cardText: {
    flex: 1,
  },
  cardTitle: {
    ...typography.heading,
    marginBottom: spacing.xs,
  },
  cardDescription: {
    ...typography.bodySmall,
    color: colors.textSecondary,
  },
});

export default ModuleSelectionScreen;
