import React from 'react';
import { render } from '@testing-library/react-native';
import LoadingSpinner from './LoadingSpinner';
import { claudeLight } from '../styles/theme';

describe('LoadingSpinner', () => {
  it('renders an ActivityIndicator', () => {
    const { getByTestId } = render(<LoadingSpinner />);
    expect(getByTestId('loading-indicator')).toBeDefined();
  });

  it('renders text when text prop is provided', () => {
    const { getByText } = render(<LoadingSpinner text="Loading..." />);
    expect(getByText('Loading...')).toBeTruthy();
  });

  it('uses primary color by default', () => {
    const { getByTestId } = render(<LoadingSpinner />);
    const indicator = getByTestId('loading-indicator');
    expect(indicator.props.color).toBe(claudeLight.primary);
  });
});
