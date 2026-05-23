import React from 'react';
import { Text } from 'react-native';
import { render } from '@testing-library/react-native';
import HistorySection from '../HistorySection';

const renderItem = (item: { id: number; label: string }) => (
  <Text testID="section-item">{item.label}</Text>
);
const keyExtractor = (item: { id: number; label: string }) =>
  String(item.id);

describe('HistorySection', () => {
  it('renders title and empty text when items is empty', () => {
    const { getByText } = render(
      <HistorySection
        title="Test Section"
        items={[]}
        emptyText="Nothing here"
        renderItem={renderItem}
        keyExtractor={keyExtractor}
      />,
    );

    expect(getByText('Test Section')).toBeTruthy();
    expect(getByText('Nothing here')).toBeTruthy();
  });

  it('renders items when provided', () => {
    const items = [
      { id: 1, label: 'Item 1' },
      { id: 2, label: 'Item 2' },
    ];

    const { getAllByTestId, queryByText } = render(
      <HistorySection
        title="Test Section"
        items={items}
        emptyText="Nothing here"
        renderItem={renderItem}
        keyExtractor={keyExtractor}
      />,
    );

    expect(getAllByTestId('section-item')).toHaveLength(2);
    expect(queryByText('Nothing here')).toBeNull();
  });

  it('renders custom renderItem correctly', () => {
    const items = [{ id: 1, label: 'Custom Item' }];

    const { getByText } = render(
      <HistorySection
        title="Custom Section"
        items={items}
        emptyText="Empty"
        renderItem={(item) => <Text testID="custom">{item.label}</Text>}
        keyExtractor={keyExtractor}
      />,
    );

    expect(getByText('Custom Item')).toBeTruthy();
    expect(getByText('Custom Section')).toBeTruthy();
  });
});
