import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import type { BoardItemStatus } from '../types/api';

interface CircularCheckboxProps {
  readonly status: BoardItemStatus;
  readonly isOwnCompletion: boolean;
  readonly onToggle: () => void;
  readonly accessibilityLabel: string;
  readonly testID: string;
}

const CHECK_SIZE = 26;
const MIN_TOUCH = 44;

const CircularCheckbox: React.FC<CircularCheckboxProps> = ({
  status,
  isOwnCompletion,
  onToggle,
  accessibilityLabel,
  testID,
}) => {
  const styles = useThemedStyles(makeStyles);
  const isCompleted = status === 'Completed';
  const isRemoved = status === 'Removed';

  const checked = isCompleted || isRemoved;

  return (
    <Pressable
      style={styles.hitArea}
      onPress={onToggle}
      accessibilityRole="checkbox"
      accessibilityState={{ checked }}
      accessibilityLabel={accessibilityLabel}
      testID={testID}
    >
      <View
        style={[
          styles.circle,
          isCompleted && isOwnCompletion && styles.circleChecked,
          isCompleted && !isOwnCompletion && styles.circleMidChecked,
        ]}
      >
        {isCompleted && (
          <Text
            style={[styles.checkmark, !isOwnCompletion && styles.checkmarkMid]}
          >
            ✓
          </Text>
        )}
      </View>
    </Pressable>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    hitArea: {
      minWidth: MIN_TOUCH,
      minHeight: MIN_TOUCH,
      alignItems: 'center',
      justifyContent: 'flex-start',
      paddingTop: 4,
    },
    circle: {
      width: CHECK_SIZE,
      height: CHECK_SIZE,
      borderRadius: CHECK_SIZE / 2,
      borderWidth: 2,
      borderColor: t.colors.border,
      alignItems: 'center',
      justifyContent: 'center',
    },
    circleChecked: {
      backgroundColor: t.colors.success,
      borderColor: t.colors.success,
    },
    circleMidChecked: {
      backgroundColor: t.colors.successLight,
      borderColor: t.colors.success,
    },
    checkmark: {
      color: t.colors.textInverse,
      fontSize: 14,
      fontWeight: '700',
      lineHeight: 16,
    },
    checkmarkMid: {
      opacity: 0.5,
    },
  });

export default React.memo(CircularCheckbox);
