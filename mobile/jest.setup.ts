// Built-in matchers from @testing-library/react-native (since v12.4+)
// replaces deprecated @testing-library/jest-native
import '@testing-library/react-native/build/matchers/extend-expect';

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

