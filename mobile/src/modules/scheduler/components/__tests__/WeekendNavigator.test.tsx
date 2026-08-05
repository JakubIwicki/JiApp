import React from 'react';
import { fireEvent } from '@testing-library/react-native';
import { composeStories } from '@storybook/react';
import * as stories from '../WeekendNavigator.stories';
import { rtlRender } from '../../../../test/rtlUtils';

jest.mock('react-i18next', () => {
  const actual = jest.requireActual('react-i18next');
  return {
    ...actual,
    useTranslation: () => ({
      t: (key: string) => key,
    }),
  };
});

const { Default } = composeStories(stories);

describe('WeekendNavigator', () => {
  it('renders the week label and the today caption', () => {
    const { getByText } = rtlRender(<Default />);

    expect(getByText('2026-05-30 / 2026-05-31')).toBeTruthy();
    expect(getByText('scheduler.weekendNavigator.today')).toBeTruthy();
  });

  it('calls onPrevious when the previous arrow is pressed', () => {
    const onPrevious = jest.fn();
    const { getByLabelText } = rtlRender(<Default onPrevious={onPrevious} />);

    fireEvent.press(
      getByLabelText('scheduler.weekendNavigator.previousAccessibility'),
    );

    expect(onPrevious).toHaveBeenCalledTimes(1);
  });

  it('calls onNext when the next arrow is pressed', () => {
    const onNext = jest.fn();
    const { getByLabelText } = rtlRender(<Default onNext={onNext} />);

    fireEvent.press(
      getByLabelText('scheduler.weekendNavigator.nextAccessibility'),
    );

    expect(onNext).toHaveBeenCalledTimes(1);
  });

  it('calls onToday when the center label is pressed', () => {
    const onToday = jest.fn();
    const { getByText } = rtlRender(<Default onToday={onToday} />);

    fireEvent.press(getByText('2026-05-30 / 2026-05-31'));

    expect(onToday).toHaveBeenCalledTimes(1);
  });
});
