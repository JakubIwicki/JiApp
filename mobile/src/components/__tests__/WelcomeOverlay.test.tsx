import React from 'react';
import { render } from '@testing-library/react-native';
import WelcomeOverlay from '../WelcomeOverlay';

jest.useFakeTimers();

jest.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

describe('WelcomeOverlay', () => {
  beforeEach(() => {
    jest.clearAllTimers();
  });

  it('renders welcome variant with display name', () => {
    const onComplete = jest.fn();
    const { getByText } = render(
      <WelcomeOverlay displayName="Jakub" type="welcome" onComplete={onComplete} />,
    );

    expect(getByText('welcome.welcomeBack')).toBeTruthy();
    expect(getByText('Jakub')).toBeTruthy();
    expect(getByText('welcome.subtitle')).toBeTruthy();
  });

  it('renders welcome variant with fallback when displayName is null', () => {
    const onComplete = jest.fn();
    const { getByText } = render(
      <WelcomeOverlay displayName={null} type="welcome" onComplete={onComplete} />,
    );

    expect(getByText('welcome.welcomeBack')).toBeTruthy();
    expect(getByText('welcome.greeting')).toBeTruthy();
  });

  it('renders farewell variant', () => {
    const onComplete = jest.fn();
    const { getByText, queryByText } = render(
      <WelcomeOverlay displayName="Jakub" type="farewell" onComplete={onComplete} />,
    );

    expect(getByText('welcome.farewell')).toBeTruthy();
    expect(queryByText('welcome.welcomeBack')).toBeNull();
    expect(queryByText('welcome.subtitle')).toBeNull();
  });

  it('renders greeting variant', () => {
    const onComplete = jest.fn();
    const { getByText, queryByText } = render(
      <WelcomeOverlay displayName={null} type="greeting" onComplete={onComplete} />,
    );

    expect(getByText('welcome.greeting')).toBeTruthy();
    expect(queryByText('welcome.welcomeBack')).toBeNull();
    expect(queryByText('welcome.subtitle')).toBeNull();
  });

  it('calls onComplete after greeting animation finishes', () => {
    const onComplete = jest.fn();
    render(
      <WelcomeOverlay displayName={null} type="greeting" onComplete={onComplete} />,
    );

    // Advance past greeting animation (~1.7s)
    jest.advanceTimersByTime(2000);
    jest.runAllTimers();

    expect(onComplete).toHaveBeenCalledTimes(1);
  });

  it('calls onComplete after welcome animation finishes', () => {
    const onComplete = jest.fn();
    render(
      <WelcomeOverlay displayName="Jakub" type="welcome" onComplete={onComplete} />,
    );

    // Advance past all animation sequences (~2.1s)
    jest.advanceTimersByTime(2500);
    // Flush any remaining microtasks
    jest.runAllTimers();

    expect(onComplete).toHaveBeenCalledTimes(1);
  });

  it('calls onComplete after farewell animation finishes', () => {
    const onComplete = jest.fn();
    render(
      <WelcomeOverlay displayName="Jakub" type="farewell" onComplete={onComplete} />,
    );

    // Advance past farewell animation (~700ms)
    jest.advanceTimersByTime(1000);
    jest.runAllTimers();

    expect(onComplete).toHaveBeenCalledTimes(1);
  });

  it('does not crash on unmount mid-animation', () => {
    const onComplete = jest.fn();
    const { unmount } = render(
      <WelcomeOverlay displayName="Jakub" type="welcome" onComplete={onComplete} />,
    );

    // Unmount after partial animation
    jest.advanceTimersByTime(300);
    expect(() => unmount()).not.toThrow();
  });
});
