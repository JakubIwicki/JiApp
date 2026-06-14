module.exports = {
  presets: [
    ['@babel/preset-typescript', { allowDeclareFields: true }],
    ['module:@react-native/babel-preset', { enableBabelRuntime: '^7.25.0' }],
  ],
  plugins: [
    'babel-plugin-transform-inline-environment-variables',
    'react-native-reanimated/plugin',
  ],
};
