import React from 'react';
import { render, fireEvent } from '@testing-library/react-native';
import ErrorMessage from '../ErrorMessage';

describe('ErrorMessage', () => {
  it('renders error message', () => {
    const { getByText } = render(
      <ErrorMessage message="Something went wrong" />,
    );
    expect(getByText('Something went wrong')).toBeTruthy();
  });

  it('renders retry button when onRetry is provided', () => {
    const { getByTestId, queryByTestId } = render(
      <ErrorMessage message="Error" onRetry={jest.fn()} />,
    );
    expect(getByTestId('error-retry-button')).toBeTruthy();
  });

  it('does not render retry button when onRetry is not provided', () => {
    const { queryByTestId } = render(
      <ErrorMessage message="Error" />,
    );
    expect(queryByTestId('error-retry-button')).toBeNull();
  });

  it('calls onRetry when retry button is pressed', () => {
    const onRetry = jest.fn();
    const { getByTestId } = render(
      <ErrorMessage message="Error" onRetry={onRetry} />,
    );
    fireEvent.press(getByTestId('error-retry-button'));
    expect(onRetry).toHaveBeenCalledTimes(1);
  });
});
