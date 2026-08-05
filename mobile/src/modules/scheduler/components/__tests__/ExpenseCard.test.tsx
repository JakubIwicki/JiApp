import React from 'react';
import { composeStories } from '@storybook/react';
import * as stories from '../ExpenseCard.stories';
import { rtlRender } from '../../../../test/rtlUtils';

const { Default, WithoutNote } = composeStories(stories);

describe('ExpenseCard', () => {
  it('renders the category, amount, and note', () => {
    const { getByText } = rtlRender(<Default />);

    expect(getByText('Fuel')).toBeTruthy();
    expect(getByText('-120 PLN')).toBeTruthy();
    expect(getByText('Paliwo dojazd do salonu Warszawa-Krakow')).toBeTruthy();
  });

  it('renders category and amount without a note when none exists', () => {
    const { getByText, queryByText } = rtlRender(<WithoutNote />);

    expect(getByText('Supplies')).toBeTruthy();
    expect(getByText('-89 PLN')).toBeTruthy();
    expect(queryByText('Paliwo dojazd do salonu Warszawa-Krakow')).toBeNull();
  });
});
