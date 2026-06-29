import { StyleSheet } from 'react-native';

// ─── Color Palette Types ─────────────────────────────────────────────────────
export type ColorPalette = { readonly [K in keyof typeof wabiLight]: string };

// ─── Wabi-Sabi Light Palette ──────────────────────────────────────────────────
export const wabiLight = {
  // Core
  primary: '#8B7E74',
  primaryDark: '#6B5F52',
  primaryLight: '#F0EAE4',

  // Surfaces
  background: '#F5F0EB',
  surface: '#FFFFFF',

  // Semantic
  success: '#7A9A7E',
  successLight: '#EDF2EE',
  error: '#C1440E',
  errorLight: '#FDF0EB',
  info: '#5B7FA5',
  warning: '#B8860B',

  // Text
  textPrimary: '#2B2118',
  textSecondary: '#8B8682',
  textTertiary: '#A0998F',
  textDescription: '#8B8682',
  textInverse: '#FFFFFF',

  // Borders & separators
  border: '#DDD6CE',
  separator: '#E8E0D8',
  placeholder: '#F0EAE4',
  placeholderDark: '#DDD6CE',

  // Misc
  cardShadow: '#2B2118',
} as const;

// ─── Wabi-Sabi Dark Palette ───────────────────────────────────────────────────
export const wabiDark: ColorPalette = {
  // Core — brand slightly lightened for dark bg visibility
  primary: '#A09080',
  primaryDark: '#8B7E74',
  primaryLight: '#3A3530',

  // Surfaces
  background: '#1A1A2E',
  surface: '#2D2D44',

  // Semantic — slightly brightened for contrast on dark
  success: '#8BAE8E',
  successLight: '#2A3A2E',
  error: '#D45A2E',
  errorLight: '#3D1A10',
  info: '#6B9EC0',
  warning: '#D4A020',

  // Text — inverted for dark backgrounds
  textPrimary: '#F0E6DC',
  textSecondary: '#A09080',
  textTertiary: '#7A6E60',
  textDescription: '#A09080',
  textInverse: '#2B2118',

  // Borders & separators — lightened for visibility on dark bg
  border: '#3A3A52',
  separator: '#35354A',
  placeholder: '#2D2D44',
  placeholderDark: '#3A3A52',

  // Misc
  cardShadow: '#000000',
};

// ─── Lavender Light Palette ───────────────────────────────────────────────────
export const lavenderLight = {
  primary: '#7C6F9B',
  primaryDark: '#5C5278',
  primaryLight: '#ECE8F3',
  background: '#F4F1F9',
  surface: '#FFFFFF',
  success: '#6F9B82',
  successLight: '#EAF1EC',
  error: '#C2415A',
  errorLight: '#FBECEF',
  info: '#5B6FA5',
  warning: '#B8860B',
  textPrimary: '#2A2433',
  textSecondary: '#837C90',
  textTertiary: '#9A93A6',
  textDescription: '#837C90',
  textInverse: '#FFFFFF',
  border: '#DDD6E8',
  separator: '#E7E1F0',
  placeholder: '#ECE8F3',
  placeholderDark: '#DDD6E8',
  cardShadow: '#2A2433',
} as const;

// ─── Lavender Dark Palette ────────────────────────────────────────────────────
export const lavenderDark: ColorPalette = {
  primary: '#A99FC8',
  primaryDark: '#8478A8',
  primaryLight: '#36304A',
  background: '#17141F',
  surface: '#272235',
  success: '#8BAE96',
  successLight: '#2A3A2E',
  error: '#D45A78',
  errorLight: '#3A1622',
  info: '#7C8FC0',
  warning: '#D4A020',
  textPrimary: '#ECE6F3',
  textSecondary: '#A99FC0',
  textTertiary: '#7A7090',
  textDescription: '#A99FC0',
  textInverse: '#17141F',
  border: '#38324C',
  separator: '#322C44',
  placeholder: '#272235',
  placeholderDark: '#38324C',
  cardShadow: '#000000',
} as const;

// ─── Claude Light Palette ─────────────────────────────────────────────────────
export const claudeLight: ColorPalette = {
  // Core
  primary: '#3578C8',
  primaryDark: '#2A5E9E',
  primaryLight: '#E6F0FB',

  // Surfaces
  background: '#F5F5F5',
  surface: '#FFFFFF',

  // Semantic
  success: '#4F9D69',
  successLight: '#EAF3EC',
  error: '#D6455F',
  errorLight: '#FBECEF',
  info: '#8A63C8',
  warning: '#B8860B',

  // Text
  textPrimary: '#1A1A1A',
  textSecondary: '#5F5F5F',
  textTertiary: '#838383',
  textDescription: '#5F5F5F',
  textInverse: '#FFFFFF',

  // Borders & separators
  border: '#D8D8D8',
  separator: '#E8E8E8',
  placeholder: '#EDEDED',
  placeholderDark: '#D8D8D8',

  // Misc
  cardShadow: '#1A1A1A',
};

// ─── Claude Dark Palette ──────────────────────────────────────────────────────
export const claudeDark: ColorPalette = {
  // Core
  primary: '#AFD7FF',
  primaryDark: '#7FB5E8',
  primaryLight: '#1E2733',

  // Surfaces
  background: '#09090B',
  surface: '#161617',

  // Semantic
  success: '#87D787',
  successLight: '#1B2A1B',
  error: '#FF5F87',
  errorLight: '#2E1620',
  info: '#D7AFFF',
  warning: '#D7AF5F',

  // Text
  textPrimary: '#EDEDED',
  textSecondary: '#838383',
  textTertiary: '#5F5F5F',
  textDescription: '#838383',
  textInverse: '#09090B',

  // Borders & separators
  border: '#3A3A3A',
  separator: '#2E2E2E',
  placeholder: '#2E2E2E',
  placeholderDark: '#3A3A3A',

  // Misc
  cardShadow: '#000000',
};

// ─── Palette Registry ─────────────────────────────────────────────────────────
export const palettes = {
  claude: { light: claudeLight, dark: claudeDark },
  wabisabi: { light: wabiLight, dark: wabiDark },
  lavender: { light: lavenderLight, dark: lavenderDark },
} as const;

export type PaletteName = keyof typeof palettes;
export const DEFAULT_PALETTE: PaletteName = 'claude';

export type ThemeMode = 'system' | 'light' | 'dark';
export const DEFAULT_THEME_MODE: ThemeMode = 'system';

// ─── Factory: Typography ──────────────────────────────────────────────────────
export const makeTypography = (c: ColorPalette) =>
  ({
    title: {
      fontSize: 28,
      fontWeight: '700' as const,
      color: c.textPrimary,
    },
    heading: {
      fontSize: 18,
      fontWeight: '700' as const,
      color: c.textPrimary,
    },
    body: {
      fontSize: 16,
      color: c.textPrimary,
    },
    bodySmall: {
      fontSize: 14,
      color: c.textTertiary,
    },
    caption: {
      fontSize: 13,
      color: c.textSecondary,
    },
    label: {
      fontSize: 12,
      color: c.textSecondary,
    },
    link: {
      fontSize: 14,
      color: c.primary,
    },
    error: {
      fontSize: 14,
      color: c.error,
      textAlign: 'center' as const,
    },
    monospace: {
      fontSize: 13,
      color: c.textTertiary,
      fontFamily: 'monospace',
      textAlign: 'center' as const,
    },
  } as const);

// ─── Spacing ─────────────────────────────────────────────────────────────────
export const spacing = {
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 24,
  xxl: 32,
} as const;

// ─── Border Radius ───────────────────────────────────────────────────────────
export const borderRadius = {
  sm: 4,
  md: 8,
  lg: 10,
  xl: 30,
} as const;

// ─── Animation Tokens ────────────────────────────────────────────────────────
// ─── Z-Index Scale ────────────────────────────────────────────────────────────
// Keeps stacking contexts predictable and prevents escalation of arbitrary values.
export const zIndexScale = {
  dropdown: 10,
  sticky: 20,
  modal: 30,
  toast: 40,
  overlay: 50,
} as const;

export const animation = {
  duration: {
    fast: 150,
    normal: 250,
    slow: 400,
    ambient: 4000,
  },
  spring: {
    bouncy: {
      stiffness: 200,
      damping: 12,
    },
    gentle: {
      stiffness: 120,
      damping: 14,
    },
  },
  stagger: {
    itemDelay: 80,
    initialDelay: 100,
  },
} as const;

// ─── Factory: Tab Bar ────────────────────────────────────────────────────────
export const makeTabBar = (c: ColorPalette) =>
  ({
    activeColor: c.primary,
    inactiveColor: c.textTertiary,
    height: 56,
    iconSize: 22,
    labelSize: 9,
  } as const);

// ─── Factory: Common Styles ──────────────────────────────────────────────────
export const makeCommonStyles = (c: ColorPalette) =>
  StyleSheet.create({
    // Screen-level containers
    screenContainer: {
      flex: 1,
      backgroundColor: c.background,
    },
    scrollContent: {
      paddingVertical: spacing.lg,
    },
    authScrollContent: {
      flexGrow: 1,
      justifyContent: 'center',
      paddingHorizontal: spacing.xl,
      paddingVertical: spacing.xxl,
    },

    // Cards
    card: {
      backgroundColor: c.surface,
      borderRadius: borderRadius.lg,
      marginHorizontal: spacing.lg,
      overflow: 'hidden',
    },
    cardRow: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      paddingHorizontal: spacing.lg,
      paddingVertical: 14,
    },
    cardSeparator: {
      height: StyleSheet.hairlineWidth,
      backgroundColor: c.separator,
      marginLeft: spacing.lg,
    },

    // Section headers
    sectionHeader: {
      fontSize: 13,
      fontWeight: '600',
      color: c.textSecondary,
      textTransform: 'uppercase',
      letterSpacing: 0.5,
      marginHorizontal: spacing.lg,
      marginBottom: spacing.sm,
      marginTop: spacing.sm,
    },
    sectionContainer: {
      marginBottom: spacing.lg,
    },

    // Empty state
    emptyState: {
      paddingVertical: 24,
      alignItems: 'center',
    },
    emptyText: {
      fontSize: 14,
      color: c.textSecondary,
    },

    // Error
    apiError: {
      color: c.error,
      fontSize: 14,
      textAlign: 'center',
      marginBottom: spacing.md,
    },

    // Links
    linkContainer: {
      marginTop: 20,
      alignItems: 'center',
    },
    linkText: {
      color: c.primary,
      fontSize: 14,
    },

    // Center content
    centerContent: {
      alignItems: 'center',
      paddingVertical: spacing.xxl,
    },
    statusText: {
      marginTop: spacing.md,
      fontSize: 14,
      color: c.textDescription,
    },

    // Action buttons row
    actionButtons: {
      flexDirection: 'row',
      gap: 12,
    },
    linkButton: {
      backgroundColor: c.primary,
      borderRadius: borderRadius.md,
      paddingHorizontal: 20,
      paddingVertical: 10,
    },
  });

// ─── Theme Type ──────────────────────────────────────────────────────────────
export interface Theme {
  colors: ColorPalette;
  typography: ReturnType<typeof makeTypography>;
  commonStyles: ReturnType<typeof makeCommonStyles>;
  tabBar: ReturnType<typeof makeTabBar>;
  spacing: typeof spacing;
  borderRadius: typeof borderRadius;
}

// ─── Backwards-compatible static exports (built from default light palette) ──
export const colors = lavenderLight;
export const typography = makeTypography(colors);
export const commonStyles = makeCommonStyles(colors);
export const tabBar = makeTabBar(colors);

// ─── Backwards-compatible legacy aliases (existing tests + stories reference these) ──
export const colorsLight = wabiLight;
export const colorsDark = wabiDark;
