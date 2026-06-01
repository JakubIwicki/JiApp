# React Doctor False Positives

Note: The `rn-no-non-native-navigator` entry was removed after completing the `@react-navigation/stack` → `@react-navigation/native-stack` migration (28 files, 23 warnings resolved). The 3 remaining occurrences are from `@react-navigation/bottom-tabs`, a separate concern.

## exhaustive-deps
- **Count:** 19
- **Reason:** Most occurrences are intentional mount-only effects with `// mount only` eslint-disable comments, or effects that reference stable refs (abortRef.current) where react-doctor's static analysis does not recognize the eslint comments. The code is correct per React hooks rules.
- **Decision:** Suppress -- code is correct per React hooks rules.

## rn-prefer-reanimated
- **Count:** 8
- **Reason:** Migrating from Animated (react-native) to react-native-reanimated requires adding a new dependency and rewriting animation code across 8 components. The current Animated usage is functionally correct.
- **Decision:** Defer to dedicated migration PR.

## rn-no-non-native-navigator
- **Count:** 3
- **Reason:** `@react-navigation/bottom-tabs` is the only available tab navigator in react-navigation v6. The proposed `@react-navigation/native-tabs` does not exist on npm. These are false positives for the JS-based tab navigator.
- **Decision:** Not actionable -- no native alternative exists.

## no-event-handler (SearchBar)
- **Count:** 1
- **Reason:** SearchBar.tsx uses a prop-sync effect (`useEffect` to sync `initialValue` to internal state in uncontrolled mode). This is the canonical pattern for uncontrolled components with a reset-able initial value.
- **Decision:** Not actionable -- correct pattern for uncontrolled component with external reset.

## prefer-useReducer
- **Count:** 0 (RESOLVED)
- **Resolution:** All 3 flagged screens (CreateAppointmentScreen, RegisterScreen, LoginScreen) have been refactored to use useReducer for related state.

## jsx-no-jsx-as-prop
- **Count:** 0 (RESOLVED)
- **Resolution:** Both flagged files (HistoryScreen, DownloadsScreen) now use useMemo for the refreshControl JSX passed to ScrollView.

## no-event-handler (SuccessCheckmark)
- **Count:** 0 (RESOLVED)
- **Resolution:** SuccessCheckmark no longer accepts a `visible` prop. The parent controls visibility by rendering/not rendering the component. The animation triggers on mount only via a dependency-free useEffect.

## only-export-components
- **Count:** 0 (RESOLVED)
- **Resolution:** `SchedulerStackParamList` moved to `types/navigation.ts`, imported by navigator.tsx and all consuming screens.
