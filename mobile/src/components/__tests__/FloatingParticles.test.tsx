import React from 'react';
import { render } from '@testing-library/react-native';
import FloatingParticles from '../FloatingParticles';

describe('FloatingParticles', () => {
  it('renders without crashing', () => {
    const { getByTestId } = render(<FloatingParticles />);
    expect(getByTestId('floating-particles')).toBeTruthy();
  });

  it('renders the correct number of particles by default', () => {
    const { getByTestId } = render(<FloatingParticles />);
    const container = getByTestId('floating-particles');
    // Should have 6 Animated.View children (one per particle)
    const children = container.props.children;
    expect(Array.isArray(children)).toBe(true);
    expect(children.length).toBe(6);
  });

  it('renders custom count when count prop is provided', () => {
    const { getByTestId } = render(<FloatingParticles count={4} />);
    const container = getByTestId('floating-particles');
    const children = container.props.children;
    expect(Array.isArray(children)).toBe(true);
    expect(children.length).toBe(4);
  });

  it('has pointerEvents none', () => {
    const { getByTestId } = render(<FloatingParticles />);
    expect(getByTestId('floating-particles').props.pointerEvents).toBe('none');
  });
});
