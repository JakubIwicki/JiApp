import React from 'react';
import { render } from '@testing-library/react-native';
import { StyleSheet } from 'react-native';
import Logo from './Logo';

describe('Logo', () => {
  it('renders a container View', () => {
    const { getByTestId } = render(<Logo />);
    expect(getByTestId('logo-container')).toBeDefined();
  });

  it('renders an Image with testID', () => {
    const { getByTestId } = render(<Logo />);
    expect(getByTestId('logo-image')).toBeDefined();
  });

  it('uses default size of 80', () => {
    const { getByTestId } = render(<Logo />);
    const image = getByTestId('logo-image');
    const flattened = StyleSheet.flatten(image.props.style);
    expect(flattened.width).toBe(80);
    expect(flattened.height).toBe(80);
  });

  it('applies custom size prop', () => {
    const { getByTestId } = render(<Logo size={120} />);
    const image = getByTestId('logo-image');
    const flattened = StyleSheet.flatten(image.props.style);
    expect(flattened.width).toBe(120);
    expect(flattened.height).toBe(120);
  });
});
