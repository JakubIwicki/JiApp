import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react-native';
import ConnectionFailureOverlay from '../ConnectionFailureOverlay';

jest.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

jest.mock('react-native-safe-area-context', () => ({
  useSafeAreaInsets: () => ({ top: 44, bottom: 34, left: 0, right: 0 }),
}));

const renderOverlay = (visible: boolean, onTimeout: () => void = jest.fn()) => {
  return render(
    <ConnectionFailureOverlay visible={visible} onTimeout={onTimeout} />,
  );
};

describe('ConnectionFailureOverlay', () => {
  beforeEach(() => {
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it('renders nothing when visible is false', () => {
    renderOverlay(false);
    expect(screen.queryByTestId('connection-error-overlay')).toBeNull();
  });

  it('renders when visible is true', () => {
    renderOverlay(true);
    expect(screen.getByTestId('connection-error-overlay')).toBeTruthy();
  });

  it('displays warning icon', () => {
    renderOverlay(true);
    expect(screen.getByTestId('warning-icon')).toBeTruthy();
  });

  it('displays translated title', () => {
    renderOverlay(true);
    expect(screen.getByText('connectionError.title')).toBeTruthy();
  });

  it('displays translated message', () => {
    renderOverlay(true);
    expect(screen.getByText('connectionError.message')).toBeTruthy();
  });

  it('displays close app button', () => {
    renderOverlay(true);
    expect(screen.getByTestId('close-app-button')).toBeTruthy();
    expect(screen.getByText('connectionError.closeApp')).toBeTruthy();
  });

  it('calls onTimeout when close button is pressed', () => {
    const onTimeout = jest.fn();
    const { getByTestId } = render(
      <ConnectionFailureOverlay visible={true} onTimeout={onTimeout} />,
    );

    fireEvent.press(getByTestId('close-app-button'));
    expect(onTimeout).toHaveBeenCalledTimes(1);
  });

  it('does not call onTimeout from timer (no auto-close)', () => {
    const onTimeout = jest.fn();
    renderOverlay(true, onTimeout);

    jest.advanceTimersByTime(10000);
    expect(onTimeout).not.toHaveBeenCalled();
  });

  it('calls onTimeout only once on repeated presses', () => {
    const onTimeout = jest.fn();
    const { getByTestId } = render(
      <ConnectionFailureOverlay visible={true} onTimeout={onTimeout} />,
    );

    fireEvent.press(getByTestId('close-app-button'));
    fireEvent.press(getByTestId('close-app-button'));
    fireEvent.press(getByTestId('close-app-button'));

    expect(onTimeout).toHaveBeenCalledTimes(1);
  });

  it('cleans up animation timeouts on unmount', () => {
    const onTimeout = jest.fn();
    const { unmount } = renderOverlay(true, onTimeout);

    unmount();

    jest.advanceTimersByTime(10000);
    expect(onTimeout).not.toHaveBeenCalled();
  });
});
