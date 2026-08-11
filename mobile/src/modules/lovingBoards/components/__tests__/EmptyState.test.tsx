import React from 'react';
import { fireEvent } from '@testing-library/react-native';
import { rtlRender } from '../../../../test/rtlUtils';
import EmptyState from '../EmptyState';

const renderEmptyState = (props: {
  subtitle?: string;
  actionLabel?: string;
  onAction?: () => void;
}) =>
  rtlRender(
    <EmptyState
      emoji="🛒"
      title="Nothing here yet"
      testID="board-empty"
      {...props}
    />,
  );

describe('EmptyState', () => {
  afterEach(() => {
    jest.clearAllMocks();
  });

  it('renders the emoji and title with the container test id', () => {
    const { getByText, getByTestId } = renderEmptyState({});

    expect(getByText('🛒')).toBeTruthy();
    expect(getByText('Nothing here yet')).toBeTruthy();
    expect(getByTestId('board-empty')).toBeTruthy();
  });

  it('renders the subtitle when provided', () => {
    const { getByText } = renderEmptyState({ subtitle: 'Add your first item' });

    expect(getByText('Add your first item')).toBeTruthy();
  });

  it('does not render a subtitle when none is provided', () => {
    const { queryByText } = renderEmptyState({});

    expect(queryByText('Add your first item')).toBeNull();
  });

  it('renders the action button and fires onAction exactly once when pressed', () => {
    const onAction = jest.fn();
    const { getByTestId, getByLabelText } = renderEmptyState({
      actionLabel: 'Add item',
      onAction,
    });

    fireEvent.press(getByTestId('board-empty-action'));

    expect(getByLabelText('Add item')).toBeTruthy();
    expect(onAction).toHaveBeenCalledTimes(1);
  });

  it('hides the action button when onAction is missing', () => {
    const { queryByTestId } = renderEmptyState({ actionLabel: 'Add item' });

    expect(queryByTestId('board-empty-action')).toBeNull();
  });

  it('hides the action button when actionLabel is missing', () => {
    const { queryByTestId } = renderEmptyState({ onAction: jest.fn() });

    expect(queryByTestId('board-empty-action')).toBeNull();
  });
});
