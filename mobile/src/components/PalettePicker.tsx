import React, { useCallback, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useTheme } from '../context/ThemeContext';
import SegmentedControl from './SegmentedControl';
import type { PaletteName } from '../styles/theme';

const PalettePicker: React.FC = () => {
  const { t } = useTranslation();
  const { palette, setPalette } = useTheme();

  const options = useMemo(
    () => [
      { value: 'claude' as const, label: t('settings.themeClaude') },
      { value: 'lavender' as const, label: t('settings.themeLavender') },
      { value: 'wabisabi' as const, label: t('settings.themeEarthy') },
    ],
    [t],
  );

  const handleChange = useCallback(
    (v: string) => {
      setPalette(v as PaletteName);
    },
    [setPalette],
  );

  return (
    <SegmentedControl
      options={options}
      value={palette}
      onChange={handleChange}
      testID="palette-picker-button"
    />
  );
};

export default PalettePicker;
