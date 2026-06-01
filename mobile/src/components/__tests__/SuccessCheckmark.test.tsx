import React from 'react';
import { render } from '@testing-library/react-native';
import SuccessCheckmark from '../SuccessCheckmark';

describe('SuccessCheckmark', () => {
  it('renders the checkmark', () => {
    const { getByTestId } = render(<SuccessCheckmark />);
    expect(getByTestId('success-checkmark')).toBeTruthy();
  });

  it('renders checkmark symbol', () => {
    const { getByText } = render(<SuccessCheckmark />);
    expect(getByText('✓')).toBeTruthy();
  });

  it('renders with default size 64', () => {
    const { getByTestId } = render(<SuccessCheckmark />);
    const container = getByTestId('success-checkmark');
    const style = container.props.style;
    const stylesArray = Array.isArray(style) ? style : [style];
    const hasSize = stylesArray.some(
      (s: Record<string, unknown>) => s.width === 64 && s.height === 64,
    );
    expect(hasSize).toBe(true);
  });

  it('renders with custom size when provided', () => {
    const { getByTestId } = render(<SuccessCheckmark size={80} />);
    const container = getByTestId('success-checkmark');
    const style = container.props.style;
    const stylesArray = Array.isArray(style) ? style : [style];
    const hasSize = stylesArray.some(
      (s: Record<string, unknown>) => s.width === 80 && s.height === 80,
    );
    expect(hasSize).toBe(true);
  });

  it('uses success color (#7A9A7E) background', () => {
    const { getByTestId } = render(<SuccessCheckmark />);
    const container = getByTestId('success-checkmark');
    const style = container.props.style;
    const stylesArray = Array.isArray(style) ? style : [style];
    const hasSuccessBg = stylesArray.some(
      (s: Record<string, unknown>) => s.backgroundColor === '#7A9A7E',
    );
    expect(hasSuccessBg).toBe(true);
  });
});
