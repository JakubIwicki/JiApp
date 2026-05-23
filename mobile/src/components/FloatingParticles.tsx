import React, { useEffect, useMemo, useRef } from 'react';
import { Animated, Easing, StyleSheet, View } from 'react-native';

interface FloatingParticlesProps {
  count?: number;
}

const PARTICLE_CHARS = ['🍃', '🌿', '🍂', '🌱'];
const DURATIONS = [4000, 5000, 6000];

interface Particle {
  char: string;
  x: number;
  duration: number;
  delay: number;
  animValue: Animated.Value;
}

function createParticle(index: number): Particle {
  return {
    char: PARTICLE_CHARS[index % PARTICLE_CHARS.length],
    x: 10 + Math.random() * 80,
    duration: DURATIONS[index % DURATIONS.length],
    delay: index * 800 + Math.random() * 1000,
    animValue: new Animated.Value(0),
  };
}

const FloatingParticles: React.FC<FloatingParticlesProps> = ({
  count = 6,
}) => {
  const particles = useMemo(
    () => Array.from({ length: count }, (_, i) => createParticle(i)),
    [count],
  );

  const animRefs = useRef<Animated.CompositeAnimation[]>([]);

  useEffect(() => {
    animRefs.current.forEach((a) => a.stop());

    animRefs.current = particles.map((p) => {
      return Animated.loop(
        Animated.sequence([
          Animated.delay(p.delay),
          Animated.timing(p.animValue, {
            toValue: 1,
            duration: p.duration,
            easing: Easing.linear,
            useNativeDriver: true,
          }),
        ]),
      );
    });

    animRefs.current.forEach((a) => a.start());

    return () => {
      animRefs.current.forEach((a) => a.stop());
    };
  }, [particles]);

  return (
    <View style={styles.container} pointerEvents="none" testID="floating-particles">
      {particles.map((particle, index) => {
        const translateY = particle.animValue.interpolate({
          inputRange: [0, 1],
          outputRange: [0, -60],
        });

        const rotate = particle.animValue.interpolate({
          inputRange: [0, 0.5, 1],
          outputRange: ['0deg', '15deg', '-15deg'],
        });

        const opacity = particle.animValue.interpolate({
          inputRange: [0, 0.1, 0.9, 1],
          outputRange: [0, 0.6, 0.6, 0],
        });

        return (
          <Animated.View
            key={index}
            style={[
              styles.particle,
              {
                left: `${particle.x}%` as unknown as number,
                transform: [{ translateY }, { rotate }],
                opacity,
              },
            ]}
          >
            <Animated.Text style={styles.particleChar}>
              {particle.char}
            </Animated.Text>
          </Animated.View>
        );
      })}
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
