import React from 'react';
import { render, fireEvent } from '@testing-library/react-native';
import Button from '../Button';

describe('Button', () => {
  it('renders title text', () => {
    const { getByText } = render(
      <Button title="Log In" onPress={jest.fn()} />,
    );
    expect(getByText('Log In')).toBeTruthy();
  });

  it('fires onPress on press', () => {
    const onPress = jest.fn();
    const { getByTestId } = render(
      <Button title="Log In" onPress={onPress} />,
    );
    fireEvent.press(getByTestId('button'));
    expect(onPress).toHaveBeenCalledTimes(1);
  });

  it('shows ActivityIndicator when loading=true', () => {
    const { queryByTestId } = render(
      <Button title="Log In" onPress={jest.fn()} loading={true} />,
    );
    expect(queryByTestId('button-loading')).toBeTruthy();
  });

  it('does NOT show title text when loading=true', () => {
    const { queryByText } = render(
      <Button title="Log In" onPress={jest.fn()} loading={true} />,
    );
    expect(queryByText('Log In')).toBeNull();
  });

  it('does NOT fire onPress when disabled=true', () => {
    const onPress = jest.fn();
    const { getByTestId } = render(
      <Button title="Log In" onPress={onPress} disabled={true} />,
    );
    fireEvent.press(getByTestId('button'));
    expect(onPress).not.toHaveBeenCalled();
  });

  it('does NOT fire onPress when loading=true', () => {
    const onPress = jest.fn();
    const { getByTestId } = render(
      <Button title="Log In" onPress={onPress} loading={true} />,
    );
    fireEvent.press(getByTestId('button'));
    expect(onPress).not.toHaveBeenCalled();
  });
});
