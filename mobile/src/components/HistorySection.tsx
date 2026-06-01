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

function HistorySectionItem<T>({
  item,
  render,
}: {
  item: T;
  render: (item: T) => React.ReactNode;
}): React.ReactElement {
  return <>{render(item)}</>;
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
          <HistorySectionItem
            key={keyExtractor(item)}
            item={item}
            render={renderItem}
          />
        ))
      )}
    </View>
  );
}

export default HistorySection;
