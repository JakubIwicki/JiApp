import React, { useEffect, useMemo } from 'react';
import { StyleSheet, View } from 'react-native';
import Animated, {
  useSharedValue,
  withTiming,
  withRepeat,
  withDelay,
  interpolate,
  Easing,
  useAnimatedStyle,
} from 'react-native-reanimated';

interface FloatingParticlesProps {
  count?: number;
}

const PARTICLE_CHARS = ['🍃', '🌿', '🍂', '🌱'];
const DURATIONS = [4000, 5000, 6000];

let particleIdCounter = 0;

interface ParticleData {
  id: string;
  char: string;
  x: number;
  duration: number;
  delay: number;
}

interface ParticleItemProps {
  particle: ParticleData;
}

const ParticleItem: React.FC<ParticleItemProps> = React.memo(({ particle }) => {
  const animValue = useSharedValue(0);

  useEffect(() => {
    const { delay, duration } = particle;
    animValue.value = withRepeat(
      withDelay(delay, withTiming(1, { duration, easing: Easing.linear })),
      -1,
      false,
    );
    // No cleanup needed — shared value lifecycle is component-bound
  }, [particle.delay, particle.duration, animValue]);

  const animatedStyle = useAnimatedStyle(() => {
    const translateY = interpolate(animValue.value, [0, 1], [0, -60]);
    const rotate = interpolate(animValue.value, [0, 0.5, 1], [0, 15, -15]);
    const opacity = interpolate(animValue.value, [0, 0.1, 0.9, 1], [0, 0.6, 0.6, 0]);
    return {
      transform: [{ translateY }, { rotate: `${rotate}deg` }],
      opacity,
    };
  });

  return (
    <Animated.View
      style={[
        styles.particle,
        {
          left: `${particle.x}%` as unknown as number,
        },
        animatedStyle,
      ]}
    >
      <Animated.Text style={styles.particleChar}>
        {particle.char}
      </Animated.Text>
    </Animated.View>
  );
});

const FloatingParticles: React.FC<FloatingParticlesProps> = ({
  count = 6,
}) => {
  const particles = useMemo(
    () => Array.from({ length: count }, (_, i) => ({
      id: `particle-${particleIdCounter++}-${i}`,
      char: PARTICLE_CHARS[i % PARTICLE_CHARS.length],
      x: 10 + Math.random() * 80,
      duration: DURATIONS[i % DURATIONS.length],
      delay: i * 800 + Math.random() * 1000,
    })),
    [count],
  );

  return (
    <View style={styles.container} pointerEvents="none" testID="floating-particles">
      {particles.map((particle) => (
        <ParticleItem key={particle.id} particle={particle} />
      ))}
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
  },
  particle: {
    position: 'absolute',
    bottom: 0,
    width: 24,
    height: 24,
    justifyContent: 'center',
    alignItems: 'center',
  },
  particleChar: {
    fontSize: 16,
  },
});

export default FloatingParticles;
