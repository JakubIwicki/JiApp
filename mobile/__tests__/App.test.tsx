/**
 * @format
 */

/**
 * @format
 */

import React from 'react';
import { render } from '@testing-library/react-native';

beforeAll(() => {
  jest.useFakeTimers();
});

afterAll(() => {
  jest.useRealTimers();
});

test('renders without crashing', () => {
  const App = require('../App').default;
  render(<App />);
});
