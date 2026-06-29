import React, { useCallback } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useThemedStyles } from '../context/ThemeContext';
import type { Theme } from '../styles/theme';

interface SegmentedOption {
  value: string;
  label: string;
}

interface Props {
  options: SegmentedOption[];
  value: string;
  onChange: (value: string) => void;
  testID?: string;
  accessibilityLabel?: string;
}

const SegmentedControl: React.FC<Props> = ({
  options,
  value,
  onChange,
  testID,
  accessibilityLabel,
}) => {
  const styles = useThemedStyles(makeStyles);

  const handlePress = useCallback(
    (optionValue: string) => {
      if (optionValue !== value) {
        onChange(optionValue);
      }
    },
    [value, onChange],
  );

  return (
    <View
      style={styles.track}
      testID={testID}
      accessibilityLabel={accessibilityLabel}
    >
      {options.map(option => {
        const isActive = value === option.value;
        return (
          <Pressable
            key={option.value}
            onPress={() => handlePress(option.value)}
            accessibilityRole="button"
            accessibilityState={{ selected: isActive }}
            accessibilityLabel={option.label}
            style={({ pressed }) => [
              styles.option,
              isActive && styles.optionActive,
              pressed && !isActive && { opacity: 0.7 },
            ]}
          >
            <Text
              style={[
                styles.label,
                isActive ? styles.labelActive : styles.labelInactive,
              ]}
            >
              {option.label}
            </Text>
          </Pressable>
        );
      })}
    </View>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    track: {
      flexDirection: 'row',
      backgroundColor: t.colors.primaryLight,
      borderRadius: 10,
      padding: 3,
    },
    option: {
      flex: 1,
      paddingVertical: 8,
      borderRadius: 8,
      alignItems: 'center',
      justifyContent: 'center',
    },
    optionActive: {
      backgroundColor: t.colors.primary,
    },
    label: {
      fontSize: 14,
      fontWeight: '600',
      textAlign: 'center',
    },
    labelActive: {
      color: t.colors.textInverse,
    },
    labelInactive: {
      color: t.colors.textTertiary,
    },
  });

export default SegmentedControl;
