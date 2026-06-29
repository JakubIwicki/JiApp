import React, { useCallback, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useTheme } from '../context/ThemeContext';
import SegmentedControl from './SegmentedControl';
import type { ThemeMode } from '../styles/theme';

const MODES: { mode: ThemeMode; labelKey: string }[] = [
  { mode: 'system', labelKey: 'settings.themeModeSystem' },
  { mode: 'light', labelKey: 'settings.themeModeLight' },
  { mode: 'dark', labelKey: 'settings.themeModeDark' },
];

const ThemeModePicker: React.FC = () => {
  const { t } = useTranslation();
  const { themeMode, setThemeMode } = useTheme();

  const options = useMemo(
    () => MODES.map(m => ({ value: m.mode, label: t(m.labelKey) })),
    [t],
  );

  const handleChange = useCallback(
    (v: string) => {
      setThemeMode(v as ThemeMode);
    },
    [setThemeMode],
  );

  return (
    <SegmentedControl
      options={options}
      value={themeMode}
      onChange={handleChange}
      testID="theme-mode-picker-button"
    />
  );
};

export default ThemeModePicker;
