import React, { useMemo } from 'react';
import { type ScrollViewProps, RefreshControl, ScrollView } from 'react-native';
import { useTheme } from '../context/ThemeContext';

interface Props extends ScrollViewProps {
  refreshing: boolean;
  onRefresh: () => void;
  refreshTestID?: string;
}

const RefreshableScrollView: React.FC<Props> = ({
  refreshing,
  onRefresh,
  refreshTestID,
  children,
  ...scrollViewProps
}) => {
  const { colors } = useTheme();

  const refreshControl = useMemo(
    () => (
      <RefreshControl
        refreshing={refreshing}
        onRefresh={onRefresh}
        testID={refreshTestID}
        tintColor={colors.primary}
      />
    ),
    [refreshing, onRefresh, refreshTestID, colors],
  );

  return (
    <ScrollView {...scrollViewProps} refreshControl={refreshControl}>
      {children}
    </ScrollView>
  );
};

export default RefreshableScrollView;
