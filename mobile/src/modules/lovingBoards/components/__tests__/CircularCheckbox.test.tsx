import React from 'react';
import { fireEvent } from '@testing-library/react-native';
import { rtlRender } from '../../../../test/rtlUtils';
import type { BoardItemStatus } from '../../types/api';
import CircularCheckbox from '../CircularCheckbox';

const renderCheckbox = (
  status: BoardItemStatus,
  isOwnCompletion: boolean,
  onToggle = jest.fn(),
) =>
  rtlRender(
    <CircularCheckbox
      status={status}
      isOwnCompletion={isOwnCompletion}
      onToggle={onToggle}
      accessibilityLabel="Mark item"
      testID="item-check-1"
    />,
  );

describe('CircularCheckbox', () => {
  afterEach(() => {
    jest.clearAllMocks();
  });

  it('renders unchecked without a checkmark for a needed item', () => {
    const { getByTestId, queryByText } = renderCheckbox('Needed', true);

    expect(getByTestId('item-check-1').props.accessibilityState.checked).toBe(
      false,
    );
    expect(queryByText('✓')).toBeNull();
  });

  it('renders checked with a checkmark for a completed item completed by the current user', () => {
    const { getByTestId, getByText } = renderCheckbox('Completed', true);

    expect(getByTestId('item-check-1').props.accessibilityState.checked).toBe(
      true,
    );
    expect(getByText('✓')).toBeTruthy();
  });

  it('renders checked with a checkmark for a completed item not completed by the current user', () => {
    const { getByTestId, getByText } = renderCheckbox('Completed', false);

    expect(getByTestId('item-check-1').props.accessibilityState.checked).toBe(
      true,
    );
    expect(getByText('✓')).toBeTruthy();
  });

  it('announces a removed item as checked but shows no checkmark', () => {
    const { getByTestId, queryByText } = renderCheckbox('Removed', true);

    expect(getByTestId('item-check-1').props.accessibilityState.checked).toBe(
      true,
    );
    expect(queryByText('✓')).toBeNull();
  });

  it('fires onToggle exactly once when pressed', () => {
    const onToggle = jest.fn();
    const { getByTestId } = renderCheckbox('Needed', true, onToggle);

    fireEvent.press(getByTestId('item-check-1'));

    expect(onToggle).toHaveBeenCalledTimes(1);
  });
});
