import React from 'react';
import Svg, { Circle, Line, Path, Polyline } from 'react-native-svg';

interface TabIconProps {
  name: 'search' | 'downloads' | 'history' | 'settings';
  color: string;
  size?: number;
}

const SearchIcon: React.FC<{ color: string; size: number }> = ({
  color,
  size,
}) => (
  <Svg
    width={size}
    height={size}
    viewBox="0 0 24 24"
    fill="none"
    testID="tab-icon-svg"
  >
    <Circle
      cx={10.5}
      cy={10.5}
      r={7}
      stroke={color}
      strokeWidth={2}
      strokeLinecap="round"
    />
    <Line
      x1={15.5}
      y1={15.5}
      x2={21}
      y2={21}
      stroke={color}
      strokeWidth={2}
      strokeLinecap="round"
    />
  </Svg>
);

const DownloadsIcon: React.FC<{ color: string; size: number }> = ({
  color,
  size,
}) => (
  <Svg
    width={size}
    height={size}
    viewBox="0 0 24 24"
    fill="none"
    testID="tab-icon-svg"
  >
    <Path
      d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"
      stroke={color}
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
    />
    <Polyline
      points="7 10 12 15 17 10"
      stroke={color}
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
    />
    <Line
      x1={12}
      y1={15}
      x2={12}
      y2={3}
      stroke={color}
      strokeWidth={2}
      strokeLinecap="round"
    />
  </Svg>
);

const HistoryIcon: React.FC<{ color: string; size: number }> = ({
  color,
  size,
}) => (
  <Svg
    width={size}
    height={size}
    viewBox="0 0 24 24"
    fill="none"
    testID="tab-icon-svg"
  >
    <Circle
      cx={12}
      cy={12}
      r={9}
      stroke={color}
      strokeWidth={2}
      strokeLinecap="round"
    />
    <Polyline
      points="12 6 12 12 16 14"
      stroke={color}
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
    />
  </Svg>
);

const SettingsIcon: React.FC<{ color: string; size: number }> = ({
  color,
  size,
}) => (
  <Svg
    width={size}
    height={size}
    viewBox="0 0 24 24"
    fill="none"
    testID="tab-icon-svg"
  >
    <Circle
      cx={12}
      cy={12}
      r={4}
      stroke={color}
      strokeWidth={2}
      strokeLinecap="round"
    />
    <Path
      d="M12 2v2m0 16v2m10-10h-2M4 12H2m17.07-7.07l-1.41 1.41M6.34 17.66l-1.41 1.41m14.14 0l-1.41-1.41M6.34 6.34L4.93 4.93"
      stroke={color}
      strokeWidth={2}
      strokeLinecap="round"
    />
  </Svg>
);

const iconComponents: Record<
  TabIconProps['name'],
  React.FC<{ color: string; size: number }>
> = {
  search: SearchIcon,
  downloads: DownloadsIcon,
  history: HistoryIcon,
  settings: SettingsIcon,
};

const TabIcon: React.FC<TabIconProps> = ({ name, color, size = 22 }) => {
  const IconComponent = iconComponents[name];
  return <IconComponent color={color} size={size} />;
};

export default TabIcon;
