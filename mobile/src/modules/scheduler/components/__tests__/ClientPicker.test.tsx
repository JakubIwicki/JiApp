import React from 'react';
import { fireEvent } from '@testing-library/react-native';
import { composeStories } from '@storybook/react';
import * as stories from '../ClientPicker.stories';
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

const { WithClients, Empty, Loading } = composeStories(stories);

describe('ClientPicker', () => {
  it('renders the label and placeholder selector while closed', () => {
    const { getByText } = rtlRender(<WithClients />);

    expect(getByText('scheduler.clientPicker.client')).toBeTruthy();
    expect(getByText('scheduler.clientPicker.selectPlaceholder')).toBeTruthy();
  });

  it('does NOT show the client list while the dropdown is closed', () => {
    const { queryByText } = rtlRender(<WithClients />);

    expect(queryByText('Anna Kowalska')).toBeNull();
  });

  it('lists clients when the selector is opened', () => {
    const { getByLabelText, getByText } = rtlRender(<WithClients />);

    fireEvent.press(
      getByLabelText('scheduler.clientPicker.selectAccessibility'),
    );

    expect(getByText('Anna Kowalska')).toBeTruthy();
    expect(getByText('Marta Zielinska')).toBeTruthy();
    expect(getByText('Piotr Nowak')).toBeTruthy();
    expect(getByText('Joanna Wisniewska')).toBeTruthy();
  });

  it('calls onSelect and closes the dropdown when a client is chosen', () => {
    const onSelect = jest.fn();
    const { getByLabelText, getByText, queryByPlaceholderText } = rtlRender(
      <WithClients onSelect={onSelect} />,
    );

    fireEvent.press(
      getByLabelText('scheduler.clientPicker.selectAccessibility'),
    );
    fireEvent.press(getByText('Anna Kowalska'));

    expect(onSelect).toHaveBeenCalledWith(
      expect.objectContaining({ id: 1, name: 'Anna Kowalska' }),
    );
    expect(
      queryByPlaceholderText('scheduler.clientPicker.searchPlaceholder'),
    ).toBeNull();
  });

  it('shows the empty state with the create action when no clients exist', () => {
    const { getByLabelText, getByText, queryByText } = rtlRender(<Empty />);

    fireEvent.press(
      getByLabelText('scheduler.clientPicker.selectAccessibility'),
    );

    expect(getByText('scheduler.clientPicker.empty')).toBeTruthy();
    expect(getByText('scheduler.clientPicker.createWithName')).toBeTruthy();
    expect(queryByText('Anna Kowalska')).toBeNull();
  });

  it('shows a loading state instead of the list or empty state while loading', () => {
    const { getByLabelText, getByText, queryByText } = rtlRender(<Loading />);

    fireEvent.press(
      getByLabelText('scheduler.clientPicker.selectAccessibility'),
    );

    expect(getByText('common.loading')).toBeTruthy();
    expect(queryByText('scheduler.clientPicker.empty')).toBeNull();
    expect(queryByText('Anna Kowalska')).toBeNull();
  });
});
