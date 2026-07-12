import React from 'react';
import { Text } from 'react-native';

// Stub for react-native-markdown-display — the real package contains Flow-
// annotated source files that Vite's bundler cannot parse. This stub renders
// markdown content as plain text so Storybook-web builds and renders markdown-
// using components without crashing.
const Markdown: React.FC<{ children?: string }> = ({ children }) =>
  React.createElement(Text, null, children ?? '');

export default Markdown;
