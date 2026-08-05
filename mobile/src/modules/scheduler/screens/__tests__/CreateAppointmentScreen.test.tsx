import React from 'react';
import { Alert } from 'react-native';
import { fireEvent, waitFor } from '@testing-library/react-native';
import { rtlRender } from '../../../../test/rtlUtils';
import type { Client } from '../../types/api';
import CreateAppointmentScreen from '../CreateAppointmentScreen';

const mockNavigate = jest.fn();
const mockGoBack = jest.fn();
const alertSpy = jest.spyOn(Alert, 'alert').mockImplementation(() => {});

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
      goBack: mockGoBack,
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

jest.mock('../../services/appointmentService', () =>
  jest.requireActual('../../services/__mocks__/appointmentService'),
);
jest.mock('../../services/clientService', () =>
  jest.requireActual('../../services/__mocks__/clientService'),
);
jest.mock('../../services/serviceCatalogService', () =>
  jest.requireActual('../../services/__mocks__/serviceCatalogService'),
);

import {
  createAppointment,
  withCreateAppointmentError,
  reset as resetAppointments,
} from '../../services/__mocks__/appointmentService';
import {
  withClients,
  reset as resetClients,
} from '../../services/__mocks__/clientService';
import { reset as resetServices } from '../../services/__mocks__/serviceCatalogService';

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

describe('CreateAppointmentScreen', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    alertSpy.mockClear();
    resetAppointments();
    resetClients();
    resetServices();
  });

  it('submits the appointment and returns to the previous screen', async () => {
    withClients([makeClient(1, 'Anna Kowalska', '+48 601 111 222')]);

    const { findByText, getByText, getByLabelText } = rtlRender(
      <CreateAppointmentScreen />,
    );

    await findByText('Strzyzenie meskie');
    fireEvent.press(
      getByLabelText('scheduler.clientPicker.selectAccessibility'),
    );
    fireEvent.press(getByText('Anna Kowalska'));
    fireEvent.press(getByText('Strzyzenie meskie'));
    fireEvent.press(getByText('scheduler.createAppointment.submit'));

    await waitFor(() => {
      expect(createAppointment).toHaveBeenCalledWith(
        expect.objectContaining({ boardId: 1, clientId: 1, serviceId: 1 }),
      );
    });
    expect(mockGoBack).toHaveBeenCalled();
  });

  it('short-circuits before calling the service when no client or service is selected', async () => {
    withClients([makeClient(1, 'Anna Kowalska')]);
    withCreateAppointmentError();

    const { findByText, getByText } = rtlRender(<CreateAppointmentScreen />);

    await findByText('Strzyzenie meskie');
    fireEvent.press(getByText('scheduler.createAppointment.submit'));

    expect(createAppointment).not.toHaveBeenCalled();
    expect(alertSpy).toHaveBeenCalledWith(
      'scheduler.validation',
      'scheduler.createAppointment.validationSelect',
    );
  });

  it('shows an error alert when the appointment creation fails', async () => {
    withClients([makeClient(1, 'Anna Kowalska')]);
    withCreateAppointmentError(new Error('Backend down'));

    const { findByText, getByText, getByLabelText } = rtlRender(
      <CreateAppointmentScreen />,
    );

    await findByText('Strzyzenie meskie');
    fireEvent.press(
      getByLabelText('scheduler.clientPicker.selectAccessibility'),
    );
    fireEvent.press(getByText('Anna Kowalska'));
    fireEvent.press(getByText('Strzyzenie meskie'));
    fireEvent.press(getByText('scheduler.createAppointment.submit'));

    await waitFor(() => {
      expect(createAppointment).toHaveBeenCalled();
    });
    expect(alertSpy).toHaveBeenCalledWith('scheduler.error', 'Backend down');
    expect(mockGoBack).not.toHaveBeenCalled();
  });
});
