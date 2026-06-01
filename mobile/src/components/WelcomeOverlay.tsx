import React, { useCallback, useEffect, useMemo, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { StyleSheet, Text, useWindowDimensions, View } from 'react-native';
import Animated, {
  useSharedValue,
  withSpring,
  withTiming,
  withDelay,
  withRepeat,
  withSequence,
  interpolate,
  useAnimatedStyle,
} from 'react-native-reanimated';
import { animation, colors, typography } from '../styles/theme';

interface Particle {
  id: string;
  x: number;
  size: number;
  color: string;
}

interface ParticleViewProps {
  particle: Particle;
  screenHeight: number;
}

interface Props {
  displayName: string | null;
  type: 'welcome' | 'farewell' | 'greeting';
  onComplete: () => void;
}

const PARTICLE_COLORS = ['#C0B8AE', '#DDD6CE', '#8B7E74', '#A0988E'];
const PARTICLE_COUNT = 10;

let particleIdCounter = 0;

const ParticleView: React.FC<ParticleViewProps> = React.memo(({ particle, screenHeight }) => {
  const animY = useSharedValue(Math.random() * 300);
  const animOpacity = useSharedValue(Math.random() * 0.3 + 0.1);

  useEffect(() => {
    animY.value = withRepeat(
      withSequence(
        withDelay(
          Math.random() * 2000,
          withTiming(-80, {
            duration: animation.duration.ambient + Math.random() * 2000,
          }),
        ),
      ),
      -1,
      false,
    );
    animOpacity.value = withRepeat(
      withSequence(
        withDelay(
          Math.random() * 2000,
          withSequence(
            withTiming(0, { duration: animation.duration.ambient * 0.8 }),
            withTiming(Math.random() * 0.3 + 0.1, { duration: animation.duration.ambient * 0.2 }),
          ),
        ),
      ),
      -1,
      false,
    );
  }, [animY, animOpacity]);

  const animatedStyle = useAnimatedStyle(() => ({
    transform: [{ translateY: animY.value }],
    opacity: animOpacity.value,
  }));

  return (
    <View style={[styles.particleWrapper, { left: particle.x }]}>
      <Animated.View
        style={[
          styles.particle,
          {
            width: particle.size,
            height: particle.size,
            borderRadius: particle.size / 2,
            backgroundColor: particle.color,
          },
          animatedStyle,
        ]}
      />
    </View>
  );
});

const WelcomeOverlay: React.FC<Props> = ({ displayName, type, onComplete }) => {
  const { t } = useTranslation();
  const { width: screenWidth } = useWindowDimensions();
  const bgOpacity = useSharedValue(0);
  const textOpacity = useSharedValue(0);
  const textSlide = useSharedValue(12);
  const nameScale = useSharedValue(0.6);
  const nameOpacity = useSharedValue(0);
  const subtitleOpacity = useSharedValue(0);

  const completedRef = useRef(false);
  const onCompleteRef = useRef(onComplete);
  onCompleteRef.current = onComplete;

  const handleComplete = useCallback(() => {
    if (!completedRef.current) {
      completedRef.current = true;
      onCompleteRef.current();
    }
  }, []);

  const particles = useMemo(
    () =>
      Array.from({ length: PARTICLE_COUNT }, () => {
        const id = `particle-${particleIdCounter++}`;
        return {
          id,
          x: Math.random() * screenWidth,
          size: Math.random() * 6 + 3,
          color: PARTICLE_COLORS[Math.floor(Math.random() * PARTICLE_COLORS.length)],
        } as Particle;
      }),
    [screenWidth],
  );

  useEffect(() => {
    completedRef.current = false;
    const timeouts: ReturnType<typeof setTimeout>[] = [];

    const schedule = (fn: () => void, delayMs: number) => {
      const id = setTimeout(fn, delayMs);
      timeouts.push(id);
    };

    if (type === 'welcome') {
      // Background fade in
      schedule(() => { bgOpacity.value = withTiming(1, { duration: 200 }); }, 0);
      // Text fade in + slide
      schedule(() => {
        textOpacity.value = withTiming(1, { duration: 300 });
        textSlide.value = withTiming(0, { duration: 300 });
      }, 200);
      // Name scale + opacity
      schedule(() => {
        nameScale.value = withSpring(1, { stiffness: 170, damping: 14 });
        nameOpacity.value = withTiming(1, { duration: 400 });
      }, 500);
      // Subtitle
      schedule(() => { subtitleOpacity.value = withTiming(1, { duration: 250 }); }, 900);
      // Hold
      schedule(() => {
        bgOpacity.value = withTiming(0, { duration: 300 });
      }, 1400);
      // Complete
      schedule(() => { handleComplete(); }, 1700);
    } else if (type === 'greeting') {
      schedule(() => { bgOpacity.value = withTiming(1, { duration: 300 }); }, 0);
      schedule(() => {
        textOpacity.value = withTiming(1, { duration: 500 });
        textSlide.value = withTiming(0, { duration: 500 });
      }, 300);
      schedule(() => { bgOpacity.value = withTiming(0, { duration: 600 }); }, 800);
      schedule(() => { handleComplete(); }, 1400);
    } else {
      // Farewell
      schedule(() => { bgOpacity.value = withTiming(1, { duration: 300 }); }, 0);
      schedule(() => {
        textOpacity.value = withTiming(1, { duration: 500 });
        textSlide.value = withTiming(0, { duration: 500 });
      }, 300);
      schedule(() => { bgOpacity.value = withTiming(0, { duration: 600 }); }, 800);
      schedule(() => { handleComplete(); }, 1400);
    }

    return () => {
      for (let i = 0; i < timeouts.length; i++) {
        clearTimeout(timeouts[i]);
      }
    };
  }, [type, bgOpacity, textOpacity, textSlide, nameScale, nameOpacity, subtitleOpacity, handleComplete]);

  const bgAnimatedStyle = useAnimatedStyle(() => ({
    opacity: bgOpacity.value,
  }));

  const textAnimatedStyle = useAnimatedStyle(() => ({
    opacity: textOpacity.value,
    transform: [{ translateY: textSlide.value }],
  }));

  const nameAnimatedStyle = useAnimatedStyle(() => ({
    opacity: nameOpacity.value,
    transform: [{ scale: nameScale.value }],
  }));

  const subtitleAnimatedStyle = useAnimatedStyle(() => ({
    opacity: subtitleOpacity.value,
  }));

  const greeting = displayName ?? t('welcome.greeting');

  return (
    <View style={styles.container} pointerEvents="none">
      <Animated.View style={[styles.background, bgAnimatedStyle]} />

      {/* Particles */}
      {particles.map((p) => (
        <ParticleView key={p.id} particle={p} screenHeight={0} />
      ))}

      {/* Text content */}
      <View style={styles.textContainer}>
        {type === 'welcome' ? (
          <>
            <Animated.Text
              style={[
                styles.greeting,
                textAnimatedStyle,
              ]}>
              {t('welcome.welcomeBack')}
            </Animated.Text>
            <Animated.Text
              style={[
                styles.name,
                nameAnimatedStyle,
              ]}>
              {greeting}
            </Animated.Text>
            <Animated.Text style={[styles.subtitle, subtitleAnimatedStyle]}>
              {t('welcome.subtitle')}
            </Animated.Text>
          </>
        ) : type === 'greeting' ? (
          <Animated.Text
            style={[
              styles.farewell,
              textAnimatedStyle,
            ]}>
            {t('welcome.greeting')}
          </Animated.Text>
        ) : (
          <Animated.Text
            style={[
              styles.farewell,
              textAnimatedStyle,
            ]}>
            {t('welcome.farewell')}
          </Animated.Text>
        )}
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    justifyContent: 'center',
    alignItems: 'center',
    zIndex: 100,
  },
  background: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: colors.background,
  },
  particleWrapper: {
    position: 'absolute',
    bottom: 0,
  },
  particle: {
    position: 'absolute',
  },
  textContainer: {
    alignItems: 'center',
    paddingHorizontal: 32,
  },
  greeting: {
    fontSize: typography.bodySmall.fontSize,
    color: colors.textSecondary,
    letterSpacing: 0.5,
    marginBottom: 8,
  },
  name: {
    fontSize: 36,
    fontWeight: '700',
    color: colors.textPrimary,
    textAlign: 'center',
    marginBottom: 16,
  },
  subtitle: {
    fontSize: typography.bodySmall.fontSize,
    color: colors.textTertiary,
    letterSpacing: 0.3,
  },
  farewell: {
    fontSize: 28,
    fontWeight: '600',
    color: colors.textPrimary,
    letterSpacing: 0.3,
  },
});

export default WelcomeOverlay;
