import React, { useMemo } from 'react';
import {
  type ScrollViewProps,
  RefreshControl,
  ScrollView,
} from 'react-native';
import { colors } from '../styles/theme';

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
  const refreshControl = useMemo(
    () => (
      <RefreshControl
        refreshing={refreshing}
        onRefresh={onRefresh}
        testID={refreshTestID}
        tintColor={colors.primary}
      />
    ),
    [refreshing, onRefresh, refreshTestID],
  );

  return (
    <ScrollView {...scrollViewProps} refreshControl={refreshControl}>
      {children}
    </ScrollView>
  );
};

export default RefreshableScrollView;
