import React from 'react';
import { fireEvent } from '@testing-library/react-native';
import { composeStories } from '@storybook/react';
import * as stories from '../Button.stories';
import { rtlRender } from '../../test/rtlUtils';

const { Default, Disabled, Loading } = composeStories(stories);

describe('Button', () => {
  it('renders title text', () => {
    const { getByText } = rtlRender(<Default />);
    expect(getByText('Log In')).toBeTruthy();
  });

  it('fires onPress on press', () => {
    const onPress = jest.fn();
    const { getByTestId } = rtlRender(<Default onPress={onPress} />);
    fireEvent.press(getByTestId('button'));
    expect(onPress).toHaveBeenCalledTimes(1);
  });

  it('shows ActivityIndicator when loading=true', () => {
    const { queryByTestId } = rtlRender(<Loading />);
    expect(queryByTestId('button-loading')).toBeTruthy();
  });

  it('does NOT show title text when loading=true', () => {
    const { queryByText } = rtlRender(<Loading />);
    expect(queryByText('Log In')).toBeNull();
  });

  it('does NOT fire onPress when disabled=true', () => {
    const onPress = jest.fn();
    const { getByTestId } = rtlRender(<Disabled onPress={onPress} />);
    fireEvent.press(getByTestId('button'));
    expect(onPress).not.toHaveBeenCalled();
  });

  it('does NOT fire onPress when loading=true', () => {
    const onPress = jest.fn();
    const { getByTestId } = rtlRender(<Loading onPress={onPress} />);
    fireEvent.press(getByTestId('button'));
    expect(onPress).not.toHaveBeenCalled();
  });
});
