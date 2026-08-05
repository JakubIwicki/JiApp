import React from 'react';
import { composeStories } from '@storybook/react';
import * as stories from '../DayTotalFooter.stories';
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

const { PositiveNet, BreakEven } = composeStories(stories);

describe('DayTotalFooter', () => {
  it('renders revenue, expenses, and net totals', () => {
    const { getByText } = rtlRender(<PositiveNet />);

    expect(getByText('scheduler.dayTotalFooter.revenue')).toBeTruthy();
    expect(getByText('870 PLN')).toBeTruthy();
    expect(getByText('scheduler.dayTotalFooter.expenses')).toBeTruthy();
    expect(getByText('-185 PLN')).toBeTruthy();
    expect(getByText('scheduler.dayTotalFooter.net')).toBeTruthy();
    expect(getByText('685 PLN')).toBeTruthy();
  });

  it('renders a zero net as a breakeven day', () => {
    const { getByText } = rtlRender(<BreakEven />);

    expect(getByText('300 PLN')).toBeTruthy();
    expect(getByText('-300 PLN')).toBeTruthy();
    expect(getByText('0 PLN')).toBeTruthy();
  });
});
