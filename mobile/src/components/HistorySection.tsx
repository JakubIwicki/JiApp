import React from 'react';
import { Text, View } from 'react-native';
import { commonStyles } from '../styles/theme';

interface HistorySectionProps<T> {
  title: string;
  items: T[];
  emptyText: string;
  renderItem: (item: T) => React.ReactNode;
  keyExtractor: (item: T) => string;
}

function HistorySection<T>({
  title,
  items,
  emptyText,
  renderItem,
  keyExtractor,
}: HistorySectionProps<T>): React.ReactElement {
  return (
    <View style={commonStyles.sectionContainer}>
      <Text style={commonStyles.sectionHeader}>{title}</Text>
      {items.length === 0 ? (
        <View style={commonStyles.emptyState}>
          <Text style={commonStyles.emptyText}>{emptyText}</Text>
        </View>
      ) : (
        items.map((item) => (
          <React.Fragment key={keyExtractor(item)}>
            {renderItem(item)}
          </React.Fragment>
        ))
      )}
    </View>
  );
}

export default HistorySection;
