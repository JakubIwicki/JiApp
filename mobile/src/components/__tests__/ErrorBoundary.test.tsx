import React from 'react';
import { Text } from 'react-native';
import { render, fireEvent } from '@testing-library/react-native';
import ErrorBoundary from '../ErrorBoundary';

const BuggyComponent: React.FC = () => {
  throw new Error('Test error from buggy component');
};

const SafeComponent: React.FC = () => {
  return <Text testID="safe-child">Safe content</Text>;
};

describe('ErrorBoundary', () => {
  beforeEach(() => {
    jest.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  it('renders children when there is no error', () => {
    const { getByTestId } = render(
      <ErrorBoundary>
        <SafeComponent />
      </ErrorBoundary>,
    );
    expect(getByTestId('safe-child')).toBeTruthy();
  });

  it('renders fallback UI when a child component throws', () => {
    const { getByTestId, queryByTestId } = render(
      <ErrorBoundary>
        <BuggyComponent />
      </ErrorBoundary>,
    );
    expect(getByTestId('error-boundary')).toBeTruthy();
    expect(queryByTestId('safe-child')).toBeNull();
  });

  it('renders a retry button in the fallback UI', () => {
    const { getByTestId } = render(
      <ErrorBoundary>
        <BuggyComponent />
      </ErrorBoundary>,
    );
    expect(getByTestId('error-boundary-retry')).toBeTruthy();
  });

  it('resets error state when retry button is pressed (child re-throws, fallback reappears)', () => {
    const { getByTestId } = render(
      <ErrorBoundary>
        <BuggyComponent />
      </ErrorBoundary>,
    );
    // Fallback should be visible
    expect(getByTestId('error-boundary')).toBeTruthy();

    fireEvent.press(getByTestId('error-boundary-retry'));

    // After retry, the child re-renders and throws again, so fallback reappears
    expect(getByTestId('error-boundary')).toBeTruthy();
  });
});
