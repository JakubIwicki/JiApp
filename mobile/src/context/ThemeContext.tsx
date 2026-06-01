import React, { createContext, use, useMemo } from 'react';
import { useColorScheme } from 'react-native';
import { colorsLight, colorsDark, type ColorPalette } from '../styles/theme';

interface ThemeContextValue {
  colors: ColorPalette;
  isDark: boolean;
}

const ThemeContext = createContext<ThemeContextValue>({
  colors: colorsLight,
  isDark: false,
});

export const ThemeProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const systemColorScheme = useColorScheme();
  const isDark = systemColorScheme === 'dark';

  const value = useMemo(
    () => ({
      colors: isDark ? colorsDark : colorsLight,
      isDark,
    }),
    [isDark],
  );

  return (
    <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
  );
};

export const useTheme = (): ThemeContextValue => use(ThemeContext);
