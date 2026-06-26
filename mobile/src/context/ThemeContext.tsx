import React, {
  createContext,
  use,
  useCallback,
  useEffect,
  useMemo,
  useState,
} from 'react';
import { useColorScheme } from 'react-native';
import {
  palettes,
  DEFAULT_PALETTE,
  DEFAULT_THEME_MODE,
  makeTypography,
  makeCommonStyles,
  makeTabBar,
  spacing,
  borderRadius,
  type PaletteName,
  type Theme,
  type ThemeMode,
} from '../styles/theme';
import * as storageService from '../services/storageService';

interface ThemeContextValue extends Theme {
  palette: PaletteName;
  isDark: boolean;
  setPalette: (name: PaletteName) => Promise<void>;
  themeMode: ThemeMode;
  setThemeMode: (mode: ThemeMode) => Promise<void>;
}

const defaultColors = palettes[DEFAULT_PALETTE].light;
const defaultContextValue: ThemeContextValue = {
  colors: defaultColors,
  typography: makeTypography(defaultColors),
  commonStyles: makeCommonStyles(defaultColors),
  tabBar: makeTabBar(defaultColors),
  spacing,
  borderRadius,
  palette: DEFAULT_PALETTE,
  isDark: false,
  setPalette: async () => {},
  themeMode: DEFAULT_THEME_MODE,
  setThemeMode: async () => {},
};

const ThemeContext = createContext<ThemeContextValue>(defaultContextValue);

export const ThemeProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [palette, setPaletteState] = useState<PaletteName>(DEFAULT_PALETTE);
  const [themeMode, setThemeModeState] =
    useState<ThemeMode>(DEFAULT_THEME_MODE);
  const systemColorScheme = useColorScheme();
  const isDark =
    themeMode === 'system'
      ? systemColorScheme === 'dark'
      : themeMode === 'dark';

  useEffect(() => {
    const load = async () => {
      const stored = await storageService.getPalette();
      if (stored && stored in palettes) {
        setPaletteState(stored as PaletteName);
      }
      const storedMode = await storageService.getThemeMode();
      if (
        storedMode === 'system' ||
        storedMode === 'light' ||
        storedMode === 'dark'
      ) {
        setThemeModeState(storedMode as ThemeMode);
      }
    };
    load();
  }, []); // mount only

  const colors = palettes[palette][isDark ? 'dark' : 'light'];

  const theme = useMemo<Theme>(
    () => ({
      colors,
      typography: makeTypography(colors),
      commonStyles: makeCommonStyles(colors),
      tabBar: makeTabBar(colors),
      spacing,
      borderRadius,
    }),
    [colors],
  );

  const setPalette = useCallback(async (name: PaletteName) => {
    setPaletteState(name);
    await storageService.savePalette(name);
  }, []);

  const setThemeMode = useCallback(async (mode: ThemeMode) => {
    setThemeModeState(mode);
    await storageService.saveThemeMode(mode);
  }, []);

  const value = useMemo<ThemeContextValue>(
    () => ({
      ...theme,
      palette,
      isDark,
      setPalette,
      themeMode,
      setThemeMode,
    }),
    [theme, palette, isDark, setPalette, themeMode, setThemeMode],
  );

  return (
    <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
  );
};

export const useTheme = (): ThemeContextValue => use(ThemeContext);

export function useThemedStyles<T>(factory: (t: Theme) => T): T {
  const ctx = useTheme();
  const theme: Theme = useMemo(
    () => ({
      colors: ctx.colors,
      typography: ctx.typography,
      commonStyles: ctx.commonStyles,
      tabBar: ctx.tabBar,
      spacing: ctx.spacing,
      borderRadius: ctx.borderRadius,
    }),
    [
      ctx.colors,
      ctx.typography,
      ctx.commonStyles,
      ctx.tabBar,
      ctx.spacing,
      ctx.borderRadius,
    ],
  );
  return useMemo(() => factory(theme), [theme, factory]);
}
