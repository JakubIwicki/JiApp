import React from 'react';
import { Text } from 'react-native';
import { fireEvent } from '@testing-library/react-native';
import { rtlRender } from '../../../../test/rtlUtils';
import CategoryCard from '../CategoryCard';

const CHILD_TEXT = 'child-content';

const renderCard = (isCollapsed: boolean) => {
  const onToggle = jest.fn();
  const utils = rtlRender(
    <CategoryCard
      categoryName="Dairy"
      categoryEmoji="🥛"
      itemCount={3}
      tint="info"
      isCollapsed={isCollapsed}
      onToggle={onToggle}
      accessibilityLabel="Dairy category"
    >
      <Text>{CHILD_TEXT}</Text>
    </CategoryCard>,
  );
  return { ...utils, onToggle };
};

describe('CategoryCard', () => {
  afterEach(() => {
    jest.clearAllMocks();
  });

  it('renders the emoji, name, and item count', () => {
    const { getByText } = renderCard(false);

    expect(getByText('🥛')).toBeTruthy();
    expect(getByText('Dairy')).toBeTruthy();
    expect(getByText('3')).toBeTruthy();
  });

  it('renders children when expanded', () => {
    const { getByText } = renderCard(false);

    expect(getByText(CHILD_TEXT)).toBeTruthy();
  });

  it('renders the collapsed chevron and hides children when collapsed', () => {
    const { getByText, queryByText } = renderCard(true);

    expect(getByText('▶')).toBeTruthy();
    expect(queryByText('▼')).toBeNull();
    expect(queryByText(CHILD_TEXT)).toBeNull();
  });

  it('renders the expanded chevron when not collapsed', () => {
    const { getByText, queryByText } = renderCard(false);

    expect(getByText('▼')).toBeTruthy();
    expect(queryByText('▶')).toBeNull();
  });

  it('announces the expanded state to assistive tech', () => {
    const { getByLabelText } = renderCard(false);

    expect(
      getByLabelText('Dairy category').props.accessibilityState.expanded,
    ).toBe(true);
  });

  it('announces the collapsed state to assistive tech', () => {
    const { getByLabelText } = renderCard(true);

    expect(
      getByLabelText('Dairy category').props.accessibilityState.expanded,
    ).toBe(false);
  });

  it('fires onToggle exactly once when the header is pressed', () => {
    const { getByLabelText, onToggle } = renderCard(false);

    fireEvent.press(getByLabelText('Dairy category'));

    expect(onToggle).toHaveBeenCalledTimes(1);
  });
});
