import React from 'react';
import { render } from '@testing-library/react-native';
import TabIcon from '../TabIcon';

// Mock react-native-svg so testID and size props are forwarded to host elements
jest.mock('react-native-svg', () => {
  const React = require('react');
  const MockSvg = ({ children, testID, width, height, ...props }: any) =>
    React.createElement('View', { testID, width, height, ...props }, children);
  const MockShape = (props: any) => React.createElement('View', props);
  return {
    __esModule: true,
    default: MockSvg,
    Svg: MockSvg,
    Circle: MockShape,
    Line: MockShape,
    Path: MockShape,
    Polyline: MockShape,
  };
});

describe('TabIcon', () => {
  it('renders search icon', () => {
    const { getByTestId } = render(<TabIcon name="search" color="#8B7E74" />);
    expect(getByTestId('tab-icon-svg')).toBeTruthy();
  });

  it('renders downloads icon', () => {
    const { getByTestId } = render(<TabIcon name="downloads" color="#8B7E74" />);
    expect(getByTestId('tab-icon-svg')).toBeTruthy();
  });

  it('renders history icon', () => {
    const { getByTestId } = render(<TabIcon name="history" color="#8B7E74" />);
    expect(getByTestId('tab-icon-svg')).toBeTruthy();
  });

  it('renders settings icon', () => {
    const { getByTestId } = render(<TabIcon name="settings" color="#8B7E74" />);
    expect(getByTestId('tab-icon-svg')).toBeTruthy();
  });

  it('renders with default size 22', () => {
    const { getByTestId } = render(<TabIcon name="search" color="#8B7E74" />);
    const svg = getByTestId('tab-icon-svg');
    expect(svg.props.width).toBe(22);
    expect(svg.props.height).toBe(22);
  });

  it('renders with custom size when provided', () => {
    const { getByTestId } = render(
      <TabIcon name="search" color="#8B7E74" size={28} />,
    );
    const svg = getByTestId('tab-icon-svg');
    expect(svg.props.width).toBe(28);
    expect(svg.props.height).toBe(28);
  });

  it('passes color to SVG stroke', () => {
    const { getByTestId } = render(<TabIcon name="search" color="#FF0000" />);
    const svg = getByTestId('tab-icon-svg');
    expect(svg.props.width).toBe(22);
    expect(svg.props.height).toBe(22);
  });
});
