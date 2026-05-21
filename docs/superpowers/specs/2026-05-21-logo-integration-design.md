# Logo Integration Design

**Date**: 2026-05-21
**Status**: approved

## Overview

Integrate `mobile/public/logo.jpg` as both the Android app icon and an in-app brand element on the main (Search) screen.

## Decisions Made

- **Placement**: Above the search bar, centered, in the initial empty state only
- **Size**: ~80px wide (subtle brand presence)
- **App icon**: Adaptive icon (Android 8+) with legacy PNG fallback

## Section 1: Logo Component

### New: `mobile/src/components/Logo.tsx`

```tsx
// Props: size (default 80)
// Renders <Image source={require('../../public/logo.jpg')} />
// Centered in a View container
// Aspect ratio preserved via resizeMode="contain"
```

### New: `mobile/src/components/Logo.stories.tsx`

Stories at default 80px, plus 120px and 160px variants for visual verification.

## Section 2: SearchScreen Changes

### Modify: `mobile/src/screens/SearchScreen.tsx`

Insert `<Logo />` above `<SearchBar />`, only in the initial state (no query, no results, no loading, no error). When the user searches or results appear, the logo hides.

The 🎵 emoji stays in the empty state for now — the logo above the search bar is the primary brand element.

```tsx
// In the main return, before SearchBar:
<View style={commonStyles.screenContainer}>
  {!isLoading && !error && results.length === 0 && lastQueryRef.current === '' && (
    <View style={styles.logoContainer}>
      <Logo />
    </View>
  )}
  <SearchBar onSearch={handleSearch} />
  <View style={styles.content}>{renderContent()}</View>
</View>
```

New style:
```tsx
logoContainer: {
  alignItems: 'center',
  paddingTop: spacing.lg,
  paddingBottom: spacing.md,
},
```

## Section 3: App Icon

### Image Processing (ImageMagick)

Steps:
1. Detect logo dimensions: `identify logo.jpg`
2. Extract centered square crop (use the smaller dimension)
3. Generate foreground PNG: 108x108dp with artwork in 72dp safe zone (~66% scale)
4. Generate legacy PNGs at 5 densities: 48x48 (mdpi), 72x72 (hdpi), 96x96 (xhdpi), 144x144 (xxhdpi), 192x192 (xxxhdpi)

### Files to create/modify

| File | Action | Purpose |
|------|--------|---------|
| `drawable/ic_launcher_foreground.png` | New | 108dp foreground layer |
| `values/ic_launcher_background.xml` | New | Background color (`#F5F0EB`) |
| `mipmap-anydpi-v26/ic_launcher.xml` | New | Adaptive icon definition |
| `mipmap-anydpi-v26/ic_launcher_round.xml` | New | Round adaptive variant |
| `mipmap-{density}/ic_launcher.png` | Replace | Legacy fallback (5 files) |
| `mipmap-{density}/ic_launcher_round.png` | Replace | Legacy round fallback (5 files) |

No changes to `AndroidManifest.xml` — already references `@mipmap/ic_launcher`.

### Background color

Use `#F5F0EB` (app's `colors.background`) — the wabi-sabi warm off-white. This complements the colorful logo artwork.

## Section 4: Testing

- **Logo component**: Jest snapshot test verifying render at default and custom sizes
- **SearchScreen**: Existing tests should still pass; verify logo appears in empty state via existing `testID` patterns
- **App icon**: Visual verification — build APK, install on device/emulator, check launcher appearance

## Files Summary

| Action | File |
|--------|------|
| CREATE | `mobile/src/components/Logo.tsx` |
| CREATE | `mobile/src/components/Logo.stories.tsx` |
| CREATE | `mobile/src/components/__tests__/Logo.test.tsx` |
| MODIFY | `mobile/src/screens/SearchScreen.tsx` |
| CREATE | `mobile/android/.../drawable/ic_launcher_foreground.png` |
| CREATE | `mobile/android/.../values/ic_launcher_background.xml` |
| CREATE | `mobile/android/.../mipmap-anydpi-v26/ic_launcher.xml` |
| CREATE | `mobile/android/.../mipmap-anydpi-v26/ic_launcher_round.xml` |
| REPLACE | `mobile/android/.../mipmap-{hdpi,mdpi,xhdpi,xxhdpi,xxxhdpi}/ic_launcher.png` |
| REPLACE | `mobile/android/.../mipmap-{hdpi,mdpi,xhdpi,xxhdpi,xxxhdpi}/ic_launcher_round.png` |
