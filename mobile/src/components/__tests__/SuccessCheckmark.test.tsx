import React from 'react';
import { render } from '@testing-library/react-native';
import SuccessCheckmark from '../SuccessCheckmark';

describe('SuccessCheckmark', () => {
  it('renders when visible is true', () => {
    const { getByTestId } = render(<SuccessCheckmark visible={true} />);
    expect(getByTestId('success-checkmark')).toBeTruthy();
  });

  it('does not render when visible is false', () => {
    const { queryByTestId } = render(<SuccessCheckmark visible={false} />);
    expect(queryByTestId('success-checkmark')).toBeNull();
  });

  it('renders checkmark symbol', () => {
    const { getByText } = render(<SuccessCheckmark visible={true} />);
    expect(getByText('✓')).toBeTruthy();
  });

  it('renders with default size 64', () => {
    const { getByTestId } = render(<SuccessCheckmark visible={true} />);
    const container = getByTestId('success-checkmark');
    // Should have width/height of 64
    const style = container.props.style;
    const stylesArray = Array.isArray(style) ? style : [style];
    const hasSize = stylesArray.some(
      (s: Record<string, unknown>) => s.width === 64 && s.height === 64,
    );
    expect(hasSize).toBe(true);
  });

  it('renders with custom size when provided', () => {
    const { getByTestId } = render(<SuccessCheckmark visible={true} size={80} />);
    const container = getByTestId('success-checkmark');
    const style = container.props.style;
    const stylesArray = Array.isArray(style) ? style : [style];
    const hasSize = stylesArray.some(
      (s: Record<string, unknown>) => s.width === 80 && s.height === 80,
    );
    expect(hasSize).toBe(true);
  });

  it('uses success color (#7A9A7E) background', () => {
    const { getByTestId } = render(<SuccessCheckmark visible={true} />);
    const container = getByTestId('success-checkmark');
    const style = container.props.style;
    const stylesArray = Array.isArray(style) ? style : [style];
    const hasSuccessBg = stylesArray.some(
      (s: Record<string, unknown>) => s.backgroundColor === '#7A9A7E',
    );
    expect(hasSuccessBg).toBe(true);
  });
});
