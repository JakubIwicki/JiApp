import React from 'react';
import { rtlRender } from '../../../../test/rtlUtils';
import type { Client } from '../../types/api';
import ClientListScreen from '../ClientListScreen';

const mockNavigate = jest.fn();

jest.mock('@react-navigation/native-stack', () => {
  const ReactMock = require('react');
  return {
    createNativeStackNavigator: () => ({
      Navigator: ({ children }: { children: React.ReactNode }) =>
        ReactMock.createElement(ReactMock.Fragment, null, children),
      Screen: ({
        component: Component,
      }: {
        component?: React.ComponentType<unknown>;
      }) => (Component ? ReactMock.createElement(Component) : null),
    }),
  };
});

jest.mock('@react-navigation/native', () => {
  const actual = jest.requireActual('@react-navigation/native');
  return {
    ...actual,
    useNavigation: () => ({
      navigate: mockNavigate,
      setOptions: jest.fn(),
    }),
    useRoute: () => ({ params: { boardId: 1 } }),
  };
});

jest.mock('react-i18next', () => {
  const actual = jest.requireActual('react-i18next');
  return {
    ...actual,
    useTranslation: () => ({
      t: (key: string) => key,
    }),
  };
});

jest.mock('../../services/clientService', () =>
  jest.requireActual('../../services/__mocks__/clientService'),
);

import {
  withClients,
  withClientError,
  reset,
} from '../../services/__mocks__/clientService';

const makeClient = (
  id: number,
  name: string,
  phone: string | null = null,
): Client => ({
  id,
  boardId: 1,
  name,
  phone,
  notes: null,
});

describe('ClientListScreen', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    reset();
  });

  it('renders the clients returned by the service', async () => {
    withClients([
      makeClient(1, 'Anna Kowalska', '+48 601 111 222'),
      makeClient(2, 'Marta Zielinska'),
    ]);

    const { findByText, getByText } = rtlRender(<ClientListScreen />);

    expect(await findByText('Anna Kowalska')).toBeTruthy();
    expect(getByText('Marta Zielinska')).toBeTruthy();
    expect(getByText('+48 601 111 222')).toBeTruthy();
  });

  it('shows the empty state when loading clients fails', async () => {
    withClientError();

    const { findByText, queryByText } = rtlRender(<ClientListScreen />);

    expect(await findByText('scheduler.clientList.empty')).toBeTruthy();
    expect(queryByText('Anna Kowalska')).toBeNull();
  });
});
