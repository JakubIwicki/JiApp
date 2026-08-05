import React from 'react';
import { composeStories } from '@storybook/react';
import * as stories from '../DayColumn.stories';
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

const { WithAppointments, Empty } = composeStories(stories);

describe('DayColumn', () => {
  it('renders the day label and the today marker for the current day', () => {
    const { getByText } = rtlRender(<WithAppointments />);

    expect(getByText('Saturday')).toBeTruthy();
    expect(getByText('•')).toBeTruthy();
  });

  it('renders appointments and expenses for the day', () => {
    const { getByText } = rtlRender(<WithAppointments />);

    expect(getByText('Anna Kowalska')).toBeTruthy();
    expect(getByText('Piotr Nowak')).toBeTruthy();
    expect(getByText('Fuel')).toBeTruthy();
    expect(getByText('Food')).toBeTruthy();
  });

  it('does NOT show the empty message when items exist', () => {
    const { queryByText } = rtlRender(<WithAppointments />);

    expect(queryByText('scheduler.dayColumn.noItems')).toBeNull();
  });

  it('shows the empty message and no today marker for a day without items', () => {
    const { getByText, queryByText } = rtlRender(<Empty />);

    expect(getByText('Sunday')).toBeTruthy();
    expect(getByText('scheduler.dayColumn.noItems')).toBeTruthy();
    expect(queryByText('Anna Kowalska')).toBeNull();
    expect(queryByText('•')).toBeNull();
  });
});
