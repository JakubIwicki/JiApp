import React from 'react';
import { composeStories } from '@storybook/react';
import * as stories from '../SummaryBar.stories';
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

describe('SummaryBar', () => {
  it('renders summed revenue, expenses, net profit, and the weekend split', () => {
    const { getByText } = rtlRender(<Default />);

    expect(getByText('scheduler.summaryBar.revenue')).toBeTruthy();
    expect(getByText('870 PLN')).toBeTruthy();
    expect(getByText('scheduler.summaryBar.expenses')).toBeTruthy();
    expect(getByText('185 PLN')).toBeTruthy();
    expect(getByText('scheduler.summaryBar.netProfit')).toBeTruthy();
    expect(getByText('685 PLN')).toBeTruthy();
    expect(getByText('scheduler.summaryBar.weekend')).toBeTruthy();
    expect(getByText('480 / 390')).toBeTruthy();
  });

  it('renders a negative net profit with a minus sign', () => {
    const { getByText } = rtlRender(
      <Default
        saturdayTotal={{ revenue: 100, expenses: 200, net: -100 }}
        sundayTotal={{ revenue: 0, expenses: 0, net: 0 }}
      />,
    );

    expect(getByText('-100 PLN')).toBeTruthy();
  });
});
