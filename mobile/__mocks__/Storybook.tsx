import React from 'react';
import { View } from 'react-native';

interface StorybookMockProps {
  children?: React.ReactNode;
}

const Storybook: React.FC<StorybookMockProps> = ({ children }) => {
  return <View>{children}</View>;
};

export default Storybook;
