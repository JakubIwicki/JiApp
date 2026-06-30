import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';

interface ProgressStripProps {
  readonly done: number;
  readonly total: number;
}

const ProgressStrip: React.FC<ProgressStripProps> = ({ done, total }) => {
  const { t } = useTranslation();
  const styles = useThemedStyles(makeStyles);

  if (total === 0) return null;

  const fraction = Math.min(done / total, 1);

  return (
    <View style={styles.container}>
      <View style={styles.track}>
        <View
          style={[
            styles.fill,
            { width: `${fraction * 100}%` as unknown as number },
          ]}
        />
      </View>
      <Text style={styles.label}>
        {t('lovingBoards.boardDetail.progress', { done, total })}
      </Text>
    </View>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      flexDirection: 'row',
      alignItems: 'center',
      paddingHorizontal: spacing.lg,
      paddingVertical: spacing.sm,
      gap: spacing.sm,
      borderBottomWidth: StyleSheet.hairlineWidth,
      borderBottomColor: t.colors.separator,
    },
    track: {
      flex: 1,
      height: 8,
      borderRadius: borderRadius.sm,
      backgroundColor: t.colors.separator,
      overflow: 'hidden',
    },
    fill: {
      height: 8,
      borderRadius: borderRadius.sm,
      backgroundColor: t.colors.success,
    },
    label: {
      ...t.typography.label,
      color: t.colors.textTertiary,
    },
  });

export default ProgressStrip;
