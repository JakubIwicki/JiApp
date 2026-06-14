module.exports = {
  presets: [
    ['@babel/preset-typescript', { allowDeclareFields: true }],
    'module:@react-native/babel-preset',
  ],
  plugins: [
    'babel-plugin-transform-inline-environment-variables',
    'react-native-reanimated/plugin',
  ],
};
