import React from 'react';
import { View, Text, Pressable, StyleSheet } from 'react-native';
import { useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';

interface WeekendNavigatorProps {
  weekLabel: string;
  onPrevious: () => void;
  onNext: () => void;
  onToday: () => void;
}

const WeekendNavigator: React.FC<WeekendNavigatorProps> = ({
  weekLabel,
  onPrevious,
  onNext,
  onToday,
}) => {
  const styles = useThemedStyles(makeStyles);
  return (
    <View style={styles.container}>
      <Pressable
        onPress={onPrevious}
        style={({ pressed }) => [
          styles.arrowButton,
          pressed && { opacity: 0.7 },
        ]}
        accessibilityLabel="Previous weekend"
        accessibilityRole="button"
      >
        <Text style={styles.arrowText}>{'‹'}</Text>
      </Pressable>

      <Pressable
        onPress={onToday}
        style={({ pressed }) => [
          styles.centerGroup,
          pressed && { opacity: 0.7 },
        ]}
      >
        <Text style={styles.labelText}>{weekLabel}</Text>
        <Text style={styles.todayLabel}>Today</Text>
      </Pressable>

      <Pressable
        onPress={onNext}
        style={({ pressed }) => [
          styles.arrowButton,
          pressed && { opacity: 0.7 },
        ]}
        accessibilityLabel="Next weekend"
        accessibilityRole="button"
      >
        <Text style={styles.arrowText}>{'›'}</Text>
      </Pressable>
    </View>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      paddingHorizontal: spacing.lg,
      paddingVertical: spacing.md,
      backgroundColor: t.colors.surface,
      borderBottomWidth: StyleSheet.hairlineWidth,
      borderBottomColor: t.colors.border,
    },
    arrowButton: {
      width: 44,
      height: 44,
      borderRadius: 22,
      backgroundColor: t.colors.primaryLight,
      alignItems: 'center',
      justifyContent: 'center',
    },
    arrowText: {
      fontSize: 28,
      color: t.colors.primary,
      lineHeight: 30,
    },
    centerGroup: {
      alignItems: 'center',
      flex: 1,
    },
    labelText: {
      ...t.typography.body,
      fontWeight: '600',
      color: t.colors.textPrimary,
    },
    todayLabel: {
      ...t.typography.caption,
      color: t.colors.primary,
      marginTop: 2,
    },
  });

export default WeekendNavigator;
