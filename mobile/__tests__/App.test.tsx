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

it('renders without crashing', () => {
  const App = require('../App').default;
  const { toJSON } = render(<App />);

  expect(toJSON()).not.toBeNull();
});
