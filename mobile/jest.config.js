const path = require('path');

module.exports = {
  setupFilesAfterEnv: ['./jest.setup.ts'],
  testPathIgnorePatterns: ['storybook-test.spec.ts'],
  transformIgnorePatterns: [
    'node_modules/(?!(react-native|@react-native|@react-native-community|@react-navigation|react-native-track-player|react-native-reanimated|@react-native-async-storage|@sayem314|react-native-keyboard-controller|react-native-markdown-display|@storybook|storybook)/)',
  ],
  collectCoverageFrom: [
    'src/**/*.{ts,tsx}',
    '!**/*.d.ts',
    '!**/__mocks__/**',
    '!**/*.test.{ts,tsx}',
    '!**/*.stories.{ts,tsx}',
    '!src/navigation/**',
  ],
  // Regression guard (baseline 75.86% minus ~13), not a quality target.
  coverageThreshold: {
    global: {
      lines: 63,
    },
  },
  transform: {
    '^.+\\.(ts|tsx)$': 'babel-jest',
    '^.+\\.(js|jsx)$': 'babel-jest',
    '^.+\\.(bmp|gif|jpg|jpeg|mp4|png|psd|svg|webp)$': path.resolve(
      __dirname,
      'node_modules/@react-native/jest-preset/jest/assetFileTransformer.js',
    ),
  },
  moduleNameMapper: {
    '^react-native($|/.*)': `${path.dirname(
      require.resolve('react-native'),
    )}/$1`,
    '^@notifee/react-native$': '<rootDir>/__mocks__/notifee-stub.ts',
  },
  haste: {
    defaultPlatform: 'ios',
    platforms: ['android', 'ios', 'native'],
  },
  testEnvironment: path.resolve(
    __dirname,
    'node_modules/@react-native/jest-preset/jest/react-native-env.js',
  ),
  setupFiles: [
    path.resolve(
      __dirname,
      'node_modules/@react-native/jest-preset/jest/setup.js',
    ),
    './jest.setup.storybook.ts',
  ],
};
