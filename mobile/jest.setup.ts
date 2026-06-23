// Built-in matchers from @testing-library/react-native (since v12.4+)
// replaces deprecated @testing-library/jest-native
import '@testing-library/react-native/build/matchers/extend-expect';

// Mock react-native-reanimated for Jest (ESM modules not parseable by Jest)
jest.mock('react-native-reanimated', () => {
  const { View, Text, ScrollView, Image } = require('react-native');
  return {
    __esModule: true,
    default: {
      View,
      Text,
      ScrollView,
      Image,
    },
    useSharedValue: (init: unknown) => ({ value: init }),
    useAnimatedStyle: (factory: () => Record<string, unknown>) => factory(),
    useDerivedValue: (factory: () => unknown) => ({ value: factory() }),
    withSpring: (toValue: unknown) => toValue,
    withTiming: (toValue: unknown) => toValue,
    withSequence: (...vals: unknown[]) => vals[vals.length - 1],
    withDelay: (_delay: unknown, value: unknown) => value,
    withRepeat: (value: unknown) => value,
    interpolate: (_val: unknown, _input: unknown[], output: unknown[]) =>
      output[output.length - 1],
    Easing: { linear: jest.fn(), ease: jest.fn(), bezier: jest.fn() },
    runOnJS:
      (fn: (...args: unknown[]) => unknown) =>
      (...args: unknown[]) =>
        fn(...args),
    cancelAnimation: jest.fn(),
    setUpTests: jest.fn(),
    Extrapolation: { CLAMP: 'clamp', EXTEND: 'extend', IDENTITY: 'identity' },
    Extrapolate: { CLAMP: 'clamp', EXTEND: 'extend', IDENTITY: 'identity' },
    createAnimatedComponent: (comp: React.ComponentType) => comp,
  };
});

// Mock native gesture handler (used by react-navigation stack)
jest.mock('react-native-gesture-handler', () => {
  const actual = jest.requireActual('react-native-gesture-handler/jestSetup');
  return {
    ...actual,
    GestureHandlerRootView: 'GestureHandlerRootView',
    Swipeable: 'Swipeable',
    DrawerLayout: 'DrawerLayout',
    State: {},
    PanGestureHandler: 'PanGestureHandler',
    TapGestureHandler: 'TapGestureHandler',
    LongPressGestureHandler: 'LongPressGestureHandler',
    PinchGestureHandler: 'PinchGestureHandler',
    RotationGestureHandler: 'RotationGestureHandler',
    FlingGestureHandler: 'FlingGestureHandler',
    NativeViewGestureHandler: 'NativeViewGestureHandler',
    ScrollView: 'ScrollView',
    gestureHandlerRootHOC: jest.fn(),
    Directions: {},
  };
});

// Mock react-native-blob-util
jest.mock('react-native-blob-util', () => ({
  __esModule: true,
  default: {
    fs: {
      dirs: {
        DownloadDir: '/storage/emulated/0/Download',
        DocumentDir: '/storage/emulated/0/Documents',
        CacheDir: '/cache',
      },
    },
    config: jest.fn(() => ({
      fetch: jest.fn(() =>
        Promise.resolve({
          path: jest.fn(() => '/storage/emulated/0/Download/file.mp3'),
        }),
      ),
    })),
    fetch: jest.fn(),
  },
  ReactNativeBlobUtil: {
    fs: {
      dirs: {
        DownloadDir: '/storage/emulated/0/Download',
        DocumentDir: '/storage/emulated/0/Documents',
        CacheDir: '/cache',
      },
    },
    config: jest.fn(() => ({
      fetch: jest.fn(() =>
        Promise.resolve({
          path: jest.fn(() => '/storage/emulated/0/Download/file.mp3'),
        }),
      ),
    })),
    fetch: jest.fn(),
  },
}));

// Mock encrypted storage
jest.mock('react-native-encrypted-storage', () => ({
  setItem: jest.fn(() => Promise.resolve()),
  getItem: jest.fn(() => Promise.resolve(null)),
  removeItem: jest.fn(() => Promise.resolve()),
  clear: jest.fn(() => Promise.resolve()),
}));

// Mock async storage
jest.mock('@react-native-async-storage/async-storage', () => ({
  setItem: jest.fn(() => Promise.resolve()),
  getItem: jest.fn(() => Promise.resolve(null)),
  removeItem: jest.fn(() => Promise.resolve()),
  clear: jest.fn(() => Promise.resolve()),
}));

// Mock react-native-localize
jest.mock('react-native-localize', () => ({
  getLocales: jest.fn(() => [{ languageCode: 'en', countryCode: 'US' }]),
  getNumberFormatSettings: jest.fn(() => ({
    decimalSeparator: '.',
    groupingSeparator: ',',
  })),
  getCountry: jest.fn(() => 'US'),
  getCurrencies: jest.fn(() => ['USD']),
  getTemperatureUnit: jest.fn(() => 'celsius'),
  getTimeZone: jest.fn(() => 'America/New_York'),
  uses24HourClock: jest.fn(() => false),
  usesMetricSystem: jest.fn(() => true),
  addEventListener: jest.fn(),
  removeEventListener: jest.fn(),
}));

// Mock @sayem314/react-native-keep-awake (native module imports cause cascade)
jest.mock('@sayem314/react-native-keep-awake', () => ({
  __esModule: true,
  activateKeepAwake: jest.fn(),
  deactivateKeepAwake: jest.fn(),
  default: {
    activateKeepAwake: jest.fn(),
    deactivateKeepAwake: jest.fn(),
  },
}));

// Mock react-native-keyboard-controller (native module)
jest.mock('react-native-keyboard-controller', () => {
  const { createElement, Fragment } = require('react');
  const { View } = require('react-native');

  const KeyboardAvoidingView: React.FC<{
    readonly children?: React.ReactNode;
    readonly behavior?: string;
    readonly keyboardVerticalOffset?: number;
    readonly style?: object;
  }> = ({ children, style }) => createElement(View, { style }, children);

  const KeyboardProvider: React.FC<{ readonly children?: React.ReactNode }> = ({
    children,
  }) => createElement(Fragment, null, children);

  return {
    __esModule: true,
    KeyboardAvoidingView,
    KeyboardProvider,
    KeyboardStickyView: KeyboardAvoidingView,
    KeyboardAwareScrollView: KeyboardAvoidingView,
    KeyboardToolbar: () => null,
    DefaultKeyboardToolbarTheme: {},
    useKeyboardAnimation: jest.fn(() => ({
      progress: { value: 0 },
      height: { value: 0 },
    })),
    useReanimatedKeyboardAnimation: jest.fn(() => ({
      progress: { value: 0 },
      height: { value: 0 },
    })),
    useWindowDimensions: jest.fn(() => ({ width: 390, height: 844 })),
  };
});
