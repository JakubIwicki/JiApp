module.exports = {
  preset: '@react-native/jest-preset',
  setupFilesAfterEnv: ['./jest.setup.ts'],
  testPathIgnorePatterns: ['storybook-test.spec.ts'],
  transformIgnorePatterns: [
    'node_modules/(?!(react-native|@react-native|@react-native-community|@react-navigation|react-native-track-player|react-native-reanimated)/)',
  ],
};
