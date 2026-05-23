import React from 'react';

// Web-safe mock for react-native-svg that renders real DOM <svg> elements.
// The real module crashes Vite's dependency optimizer because its Fabric
// native components import codegenNativeComponent from react-native, which
// doesn't exist in react-native-web.
//
// This mock produces visible SVG shapes in Storybook web while keeping the
// same component API that TabIcon and SearchBar expect.

type SvgProps = {
  width?: number | string;
  height?: number | string;
  viewBox?: string;
  fill?: string;
  stroke?: string;
  strokeWidth?: number | string;
  strokeLinecap?: 'butt' | 'round' | 'square';
  strokeLinejoin?: 'miter' | 'round' | 'bevel';
  children?: React.ReactNode;
  style?: any;
  testID?: string;
};

type ShapeProps = {
  cx?: number;
  cy?: number;
  r?: number;
  x1?: number;
  y1?: number;
  x2?: number;
  y2?: number;
  d?: string;
  points?: string;
  stroke?: string;
  strokeWidth?: number | string;
  strokeLinecap?: 'butt' | 'round' | 'square';
  strokeLinejoin?: 'miter' | 'round' | 'bevel';
  fill?: string;
  children?: React.ReactNode;
  style?: any;
  testID?: string;
};

export const Svg: React.FC<SvgProps> = ({
  width,
  height,
  viewBox,
  fill,
  stroke,
  strokeWidth,
  strokeLinecap,
  strokeLinejoin,
  children,
  style,
  testID,
}) => (
  <svg
    data-testid={testID}
    width={width}
    height={height}
    viewBox={viewBox}
    fill={fill}
    stroke={stroke}
    strokeWidth={strokeWidth as any}
    strokeLinecap={strokeLinecap}
    strokeLinejoin={strokeLinejoin}
    style={{ display: 'block', ...style }}
  >
    {children}
  </svg>
);

export const Circle: React.FC<ShapeProps> = (props) => (
  <circle
    cx={props.cx}
    cy={props.cy}
    r={props.r}
    fill={props.fill}
    stroke={props.stroke}
    strokeWidth={props.strokeWidth as any}
    strokeLinecap={props.strokeLinecap}
    strokeLinejoin={props.strokeLinejoin}
    data-testid={props.testID}
  />
);

export const Path: React.FC<ShapeProps> = (props) => (
  <path
    d={props.d}
    fill={props.fill}
    stroke={props.stroke}
    strokeWidth={props.strokeWidth as any}
    strokeLinecap={props.strokeLinecap}
    strokeLinejoin={props.strokeLinejoin}
    data-testid={props.testID}
  />
);

export const Line: React.FC<ShapeProps> = (props) => (
  <line
    x1={props.x1}
    y1={props.y1}
    x2={props.x2}
    y2={props.y2}
    fill={props.fill}
    stroke={props.stroke}
    strokeWidth={props.strokeWidth as any}
    strokeLinecap={props.strokeLinecap}
    strokeLinejoin={props.strokeLinejoin}
    data-testid={props.testID}
  />
);

export const Polyline: React.FC<ShapeProps> = (props) => (
  <polyline
    points={props.points}
    fill={props.fill}
    stroke={props.stroke}
    strokeWidth={props.strokeWidth as any}
    strokeLinecap={props.strokeLinecap}
    strokeLinejoin={props.strokeLinejoin}
    data-testid={props.testID}
  />
);

export const G: React.FC<ShapeProps> = ({ children }) => <g>{children}</g>;

export default Svg;
