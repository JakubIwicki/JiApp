# Logo Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Integrate `mobile/public/logo.jpg` as an in-app brand element on SearchScreen and as the Android adaptive app icon.

**Architecture:** Two independent workstreams — a React Native `Logo` component rendered above `SearchBar` in the initial empty state, and ImageMagick-based icon generation producing adaptive (Android 8+) + legacy PNG launcher icons.

**Tech Stack:** React Native (TypeScript), ImageMagick, Android adaptive icons (XML + drawable), Jest + @testing-library/react-native

---

### Task 1: Create Logo Component

**Files:**
- Create: `mobile/src/components/Logo.tsx`
- Create: `mobile/src/components/Logo.stories.tsx`

- [ ] **Step 1: Write the Logo component**

```tsx
import React from 'react';
import { Image, StyleSheet, View } from 'react-native';

interface LogoProps {
  size?: number;
}

const Logo: React.FC<LogoProps> = ({ size = 80 }) => {
  return (
    <View style={styles.container} testID="logo-container">
      <Image
        source={require('../../public/logo.jpg')}
        style={[styles.logo, { width: size, height: size }]}
        resizeMode="contain"
        testID="logo-image"
      />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    alignItems: 'center',
    justifyContent: 'center',
  },
  logo: {
    // width and height set dynamically via size prop
  },
});

export default Logo;
```

- [ ] **Step 2: Write the Storybook story**

```tsx
import React from 'react';
import { View, StyleSheet } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import Logo from './Logo';

const meta: Meta<typeof Logo> = {
  title: 'Logo',
  component: Logo,
  decorators: [
    (Story) => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof Logo>;

export const Default: Story = {
  args: {},
};

export const Large: Story = {
  args: { size: 120 },
};

export const ExtraLarge: Story = {
  args: { size: 160 },
};

const styles = StyleSheet.create({
  decorator: {
    padding: 16,
    justifyContent: 'center',
    flex: 1,
    backgroundColor: '#F5F0EB',
  },
});
```

- [ ] **Step 3: Commit**

```bash
git add mobile/src/components/Logo.tsx mobile/src/components/Logo.stories.tsx
git commit -m "feat: add Logo component"
```

---

### Task 2: Logo Component Test

**Files:**
- Create: `mobile/src/components/__tests__/Logo.test.tsx`

- [ ] **Step 1: Write the test**

```tsx
import React from 'react';
import { render } from '@testing-library/react-native';
import Logo from '../Logo';

describe('Logo', () => {
  it('renders at default size 80', () => {
    const { getByTestId } = render(<Logo />);
    const image = getByTestId('logo-image');
    expect(image.props.style).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ width: 80, height: 80 }),
      ]),
    );
  });

  it('renders at custom size', () => {
    const { getByTestId } = render(<Logo size={120} />);
    const image = getByTestId('logo-image');
    expect(image.props.style).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ width: 120, height: 120 }),
      ]),
    );
  });

  it('renders with container testID', () => {
    const { getByTestId } = render(<Logo />);
    expect(getByTestId('logo-container')).toBeTruthy();
  });
});
```

- [ ] **Step 2: Run tests to verify they pass**

```sh
cd mobile && npx jest src/components/__tests__/Logo.test.tsx
```
Expected: 3 tests PASS

- [ ] **Step 3: Commit**

```bash
git add mobile/src/components/__tests__/Logo.test.tsx
git commit -m "test: add Logo component tests"
```

---

### Task 3: Integrate Logo into SearchScreen

**Files:**
- Modify: `mobile/src/screens/SearchScreen.tsx`

- [ ] **Step 1: Add Logo import**

Add after line 19 (`import FloatingParticles from '../components/FloatingParticles';`):
```tsx
import Logo from '../components/Logo';
```

- [ ] **Step 2: Add logoContainer style**

Add inside `StyleSheet.create` block:
```tsx
  logoContainer: {
    alignItems: 'center',
    paddingTop: spacing.lg,
    paddingBottom: spacing.md,
  },
```

- [ ] **Step 3: Insert Logo above SearchBar in initial state**

Replace the main return JSX (lines 170-175) with:
```tsx
  const showLogo = !isLoading && !error && results.length === 0 && lastQueryRef.current === '';

  return (
    <View style={commonStyles.screenContainer}>
      {showLogo && (
        <View style={styles.logoContainer}>
          <Logo />
        </View>
      )}
      <SearchBar onSearch={handleSearch} />
      <View style={styles.content}>{renderContent()}</View>
    </View>
  );
```

The `showLogo` variable goes before the `return` statement, after `handleRetry` (around line 102).

- [ ] **Step 4: Run existing SearchScreen tests to verify no regressions**

```sh
cd mobile && npx jest src/screens/__tests__/SearchScreen.test.tsx
```
Expected: all existing tests PASS (5 tests)

- [ ] **Step 5: Verify the Logo test still passes too**

```sh
cd mobile && npx jest src/components/__tests__/Logo.test.tsx
```
Expected: 3 tests PASS

- [ ] **Step 6: Commit**

```bash
git add mobile/src/screens/SearchScreen.tsx
git commit -m "feat: show Logo above search bar in initial empty state"
```

---

### Task 4: Generate Android App Icons from Logo

**Files:**
- Modify: `mobile/android/app/src/main/res/drawable/ic_launcher_foreground.png` (create)
- Modify: `mobile/android/app/src/main/res/mipmap-{density}/ic_launcher.png` (replace, 5 files)
- Modify: `mobile/android/app/src/main/res/mipmap-{density}/ic_launcher_round.png` (replace, 5 files)

- [ ] **Step 1: Create output directories**

```bash
mkdir -p mobile/android/app/src/main/res/drawable
```

- [ ] **Step 2: Generate centered square crop and all icon sizes**

```bash
cd mobile/android/app/src/main/res

# Extract the smaller dimension for a centered square crop
# 1440x1464 → smaller side is 1440 → crop to 1440x1440
SQUARE=$(identify -format '%[fx:min(w,h)]' ../../../../../../public/logo.jpg)

# Generate foreground (108dp x 108dp, centered square crop from logo)
convert ../../../../../../public/logo.jpg \
  -gravity center \
  -extent "${SQUARE}x${SQUARE}" \
  -resize 108x108 \
  drawable/ic_launcher_foreground.png

# Generate legacy launcher icons at each density
convert drawable/ic_launcher_foreground.png -resize 48x48  mipmap-mdpi/ic_launcher.png
convert drawable/ic_launcher_foreground.png -resize 72x72  mipmap-hdpi/ic_launcher.png
convert drawable/ic_launcher_foreground.png -resize 96x96  mipmap-xhdpi/ic_launcher.png
convert drawable/ic_launcher_foreground.png -resize 144x144 mipmap-xxhdpi/ic_launcher.png
convert drawable/ic_launcher_foreground.png -resize 192x192 mipmap-xxxhdpi/ic_launcher.png

# Round icons are the same content — copy each
for d in mdpi hdpi xhdpi xxhdpi xxxhdpi; do
  cp mipmap-$d/ic_launcher.png mipmap-$d/ic_launcher_round.png
done
```

- [ ] **Step 3: Commit**

```bash
git add mobile/android/app/src/main/res/drawable/ic_launcher_foreground.png \
  mobile/android/app/src/main/res/mipmap-mdpi/ic_launcher.png \
  mobile/android/app/src/main/res/mipmap-mdpi/ic_launcher_round.png \
  mobile/android/app/src/main/res/mipmap-hdpi/ic_launcher.png \
  mobile/android/app/src/main/res/mipmap-hdpi/ic_launcher_round.png \
  mobile/android/app/src/main/res/mipmap-xhdpi/ic_launcher.png \
  mobile/android/app/src/main/res/mipmap-xhdpi/ic_launcher_round.png \
  mobile/android/app/src/main/res/mipmap-xxhdpi/ic_launcher.png \
  mobile/android/app/src/main/res/mipmap-xxhdpi/ic_launcher_round.png \
  mobile/android/app/src/main/res/mipmap-xxxhdpi/ic_launcher.png \
  mobile/android/app/src/main/res/mipmap-xxxhdpi/ic_launcher_round.png
git commit -m "feat: replace default launcher icons with logo artwork"
```

---

### Task 5: Add Adaptive Icon XML Configuration

**Files:**
- Create: `mobile/android/app/src/main/res/values/ic_launcher_background.xml`
- Create: `mobile/android/app/src/main/res/mipmap-anydpi-v26/ic_launcher.xml`
- Create: `mobile/android/app/src/main/res/mipmap-anydpi-v26/ic_launcher_round.xml`

- [ ] **Step 1: Create the mipmap-anydpi-v26 directory**

```bash
mkdir -p mobile/android/app/src/main/res/mipmap-anydpi-v26
```

- [ ] **Step 2: Write the background color resource**

```xml
<?xml version="1.0" encoding="utf-8"?>
<resources>
    <color name="ic_launcher_background">#F5F0EB</color>
</resources>
```

Save to: `mobile/android/app/src/main/res/values/ic_launcher_background.xml`

- [ ] **Step 3: Write the adaptive icon definition**

```xml
<?xml version="1.0" encoding="utf-8"?>
<adaptive-icon xmlns:android="http://schemas.android.com/apk/res/android">
    <background android:drawable="@color/ic_launcher_background"/>
    <foreground android:drawable="@drawable/ic_launcher_foreground"/>
</adaptive-icon>
```

Save to: `mobile/android/app/src/main/res/mipmap-anydpi-v26/ic_launcher.xml`

- [ ] **Step 4: Write the round adaptive icon definition (same content)**

```xml
<?xml version="1.0" encoding="utf-8"?>
<adaptive-icon xmlns:android="http://schemas.android.com/apk/res/android">
    <background android:drawable="@color/ic_launcher_background"/>
    <foreground android:drawable="@drawable/ic_launcher_foreground"/>
</adaptive-icon>
```

Save to: `mobile/android/app/src/main/res/mipmap-anydpi-v26/ic_launcher_round.xml`

- [ ] **Step 5: Commit**

```bash
git add mobile/android/app/src/main/res/values/ic_launcher_background.xml \
  mobile/android/app/src/main/res/mipmap-anydpi-v26/ic_launcher.xml \
  mobile/android/app/src/main/res/mipmap-anydpi-v26/ic_launcher_round.xml
git commit -m "feat: add adaptive icon configuration for Android 8+"
```

---

### Task 6: Verification

- [ ] **Step 1: Run all mobile tests**

```sh
cd mobile && npx jest
```
Expected: all tests PASS, including the new Logo test

- [ ] **Step 2: Build Android APK**

```sh
cd mobile/android && ./gradlew assembleDebug
```
Expected: BUILD SUCCESSFUL with no resource errors

- [ ] **Step 3: Visual check — install on device/emulator, verify launcher icon and in-app logo**

This step requires a connected device or emulator. Confirm:
- Launcher icon shows the logo (not the default green robot)
- SearchScreen shows the logo above the search bar on first load
- Logo hides when a search is performed
