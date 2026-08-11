import React from 'react';
import { act, fireEvent } from '@testing-library/react-native';
import { rtlRender } from '../../../../test/rtlUtils';
import Snackbar from '../Snackbar';

const AUTO_DISMISS_MS = 5000;

const renderSnackbar = (
  props: {
    message?: string;
    actionLabel?: string;
    onAction?: () => void;
    onDismiss?: () => void;
    durationMs?: number;
  } = {},
) => {
  const onDismiss = props.onDismiss ?? jest.fn();
  const utils = rtlRender(
    <Snackbar
      message={props.message ?? 'Item added'}
      actionLabel={props.actionLabel}
      onAction={props.onAction}
      onDismiss={onDismiss}
      durationMs={props.durationMs}
    />,
  );
  return { ...utils, onDismiss };
};

describe('Snackbar', () => {
  beforeEach(() => {
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it('renders the message', () => {
    const { getByText } = renderSnackbar({ message: 'Item added' });

    expect(getByText('Item added')).toBeTruthy();
  });

  it('auto-dismisses at exactly the default duration', () => {
    const { onDismiss } = renderSnackbar();

    act(() => {
      jest.advanceTimersByTime(AUTO_DISMISS_MS - 1);
    });
    expect(onDismiss).not.toHaveBeenCalled();

    act(() => {
      jest.advanceTimersByTime(1);
    });
    expect(onDismiss).toHaveBeenCalledTimes(1);
  });

  it('honours a custom duration', () => {
    const { onDismiss } = renderSnackbar({ durationMs: 1200 });

    act(() => {
      jest.advanceTimersByTime(1199);
    });
    expect(onDismiss).not.toHaveBeenCalled();

    act(() => {
      jest.advanceTimersByTime(1);
    });
    expect(onDismiss).toHaveBeenCalledTimes(1);
  });

  it('fires onAction exactly once when the action is pressed and cancels the auto-dismiss', () => {
    const onAction = jest.fn();
    const { getByTestId, getByLabelText, onDismiss } = renderSnackbar({
      actionLabel: 'Undo',
      onAction,
    });

    expect(getByLabelText('Undo')).toBeTruthy();

    fireEvent.press(getByTestId('snackbar-action'));

    expect(onAction).toHaveBeenCalledTimes(1);

    act(() => {
      jest.advanceTimersByTime(AUTO_DISMISS_MS);
    });
    expect(onDismiss).not.toHaveBeenCalled();
    expect(jest.getTimerCount()).toBe(0);
  });

  it('hides the action button when onAction is missing', () => {
    const { queryByTestId } = renderSnackbar({ actionLabel: 'Undo' });

    expect(queryByTestId('snackbar-action')).toBeNull();
  });

  it('hides the action button when actionLabel is missing', () => {
    const { queryByTestId } = renderSnackbar({ onAction: jest.fn() });

    expect(queryByTestId('snackbar-action')).toBeNull();
  });

  it('unmounting before the timeout fires no onDismiss and leaves no pending timer', () => {
    const { onDismiss, unmount } = renderSnackbar();

    expect(jest.getTimerCount()).toBe(1);

    unmount();
    expect(jest.getTimerCount()).toBe(0);
    expect(onDismiss).not.toHaveBeenCalled();

    act(() => {
      jest.advanceTimersByTime(AUTO_DISMISS_MS);
    });
    expect(onDismiss).not.toHaveBeenCalled();
    expect(jest.getTimerCount()).toBe(0);
  });
});
