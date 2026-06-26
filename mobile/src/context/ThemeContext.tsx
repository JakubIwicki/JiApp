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
  makeTypography,
  makeCommonStyles,
  makeTabBar,
  spacing,
  borderRadius,
  type PaletteName,
  type Theme,
} from '../styles/theme';
import * as storageService from '../services/storageService';

interface ThemeContextValue extends Theme {
  palette: PaletteName;
  isDark: boolean;
  setPalette: (name: PaletteName) => Promise<void>;
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
};

const ThemeContext = createContext<ThemeContextValue>(defaultContextValue);

export const ThemeProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [palette, setPaletteState] = useState<PaletteName>(DEFAULT_PALETTE);
  const systemColorScheme = useColorScheme();
  const isDark = systemColorScheme === 'dark';

  useEffect(() => {
    const load = async () => {
      const stored = await storageService.getPalette();
      if (stored && stored in palettes) {
        setPaletteState(stored as PaletteName);
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

  const value = useMemo<ThemeContextValue>(
    () => ({ ...theme, palette, isDark, setPalette }),
    [theme, palette, isDark, setPalette],
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
