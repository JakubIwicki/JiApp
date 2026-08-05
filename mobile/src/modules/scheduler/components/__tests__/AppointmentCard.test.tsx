import React from 'react';
import { fireEvent } from '@testing-library/react-native';
import { composeStories } from '@storybook/react';
import * as stories from '../AppointmentCard.stories';
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

const { MensHaircut, WomensService, WithDescription } = composeStories(stories);

const appointmentDescription =
  'Pelna koloryzacja z pasemkami. Klientka chce zrobic tez delikatne fale. Poprzednia wizyta byla 6 tygodni temu. Nalezy przygotowac farbe w odcieniu 7.3.';

describe('AppointmentCard', () => {
  it('renders client name, service, time range, price, and status', () => {
    const { getByText } = rtlRender(<MensHaircut />);

    expect(getByText('Anna Kowalska')).toBeTruthy();
    expect(getByText('Strzyzenie meskie')).toBeTruthy();
    expect(getByText('09:00-09:30')).toBeTruthy();
    expect(getByText('60 PLN')).toBeTruthy();
    expect(getByText('scheduler.status.created')).toBeTruthy();
  });

  it('renders a WomensService appointment with its own values', () => {
    const { getByText } = rtlRender(<WomensService />);

    expect(getByText('Marta Zielinska')).toBeTruthy();
    expect(getByText('Stylizacja wieczorowa')).toBeTruthy();
    expect(getByText('10:00-11:30')).toBeTruthy();
    expect(getByText('200 PLN')).toBeTruthy();
  });

  it('does NOT render a location when the appointment has none', () => {
    const { queryByText } = rtlRender(<WomensService />);

    expect(queryByText('Salon Warszawa')).toBeNull();
  });

  it('does NOT render the appointment description on the card', () => {
    const { getByText, queryByText } = rtlRender(<WithDescription />);

    expect(getByText('Katarzyna Adamczyk')).toBeTruthy();
    expect(getByText('Koloryzacja pelna')).toBeTruthy();
    expect(queryByText(appointmentDescription)).toBeNull();
  });

  it('fires onPress with the appointment when pressed', () => {
    const onPress = jest.fn();
    const { getByLabelText } = rtlRender(<MensHaircut onPress={onPress} />);

    fireEvent.press(getByLabelText('Anna Kowalska, Strzyzenie meskie, 09:00'));

    expect(onPress).toHaveBeenCalledTimes(1);
  });

  it('exposes an accessible label combining client, service, and start time', () => {
    const { getByLabelText } = rtlRender(<MensHaircut />);

    expect(
      getByLabelText('Anna Kowalska, Strzyzenie meskie, 09:00'),
    ).toBeTruthy();
  });
});
