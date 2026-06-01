import { StyleSheet } from 'react-native';

// ─── Color Palette Types ─────────────────────────────────────────────────────
export type ColorPalette = { readonly [K in keyof typeof colorsLight]: string };

// ─── Light Palette (Wabi-Sabi) ───────────────────────────────────────────────
export const colorsLight = {
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

// ─── Dark Palette (Midnight Wabi-Sabi) ───────────────────────────────────────
export const colorsDark: ColorPalette = {
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

// ─── Backwards-compatible default export ─────────────────────────────────────
export const colors = colorsLight;

// ─── Typography ──────────────────────────────────────────────────────────────
export const typography = {
  title: {
    fontSize: 28,
    fontWeight: '700' as const,
    color: colors.textPrimary,
  },
  heading: {
    fontSize: 18,
    fontWeight: '700' as const,
    color: colors.textPrimary,
  },
  body: {
    fontSize: 16,
    color: colors.textPrimary,
  },
  bodySmall: {
    fontSize: 14,
    color: colors.textTertiary,
  },
  caption: {
    fontSize: 13,
    color: colors.textSecondary,
  },
  label: {
    fontSize: 12,
    color: colors.textSecondary,
  },
  link: {
    fontSize: 14,
    color: colors.primary,
  },
  error: {
    fontSize: 14,
    color: colors.error,
    textAlign: 'center' as const,
  },
  monospace: {
    fontSize: 13,
    color: colors.textTertiary,
    fontFamily: 'monospace',
    textAlign: 'center' as const,
  },
} as const;

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

// ─── Tab Bar ─────────────────────────────────────────────────────────────────
export const tabBar = {
  activeColor: colors.primary,
  inactiveColor: '#C0B8AE',
  height: 56,
  iconSize: 22,
  labelSize: 9,
} as const;

// ─── Common Styles ───────────────────────────────────────────────────────────
export const commonStyles = StyleSheet.create({
  // Screen-level containers
  screenContainer: {
    flex: 1,
    backgroundColor: colors.background,
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
    backgroundColor: colors.surface,
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
    backgroundColor: colors.separator,
    marginLeft: spacing.lg,
  },

  // Section headers
  sectionHeader: {
    fontSize: 13,
    fontWeight: '600',
    color: colors.textSecondary,
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
    color: colors.textSecondary,
  },

  // Error
  apiError: {
    color: colors.error,
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
    color: colors.primary,
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
    color: colors.textDescription,
  },

  // Action buttons row
  actionButtons: {
    flexDirection: 'row',
    gap: 12,
  },
  linkButton: {
    backgroundColor: colors.primary,
    borderRadius: borderRadius.md,
    paddingHorizontal: 20,
    paddingVertical: 10,
  },
});
