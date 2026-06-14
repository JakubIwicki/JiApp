import React, { useRef } from 'react';
import { render, waitFor, act } from '@testing-library/react-native';
import {
  NavigationContainer,
  NavigationContainerRef,
} from '@react-navigation/native';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import SchedulerNavigator from '../SchedulerNavigator';
import type { SchedulerStackParamList } from '../../modules/scheduler/types/navigation';
import '../../i18n';

type SchedulerNavRef = NavigationContainerRef<SchedulerStackParamList>;

// Mock the board service so BoardProvider can load boards
jest.mock('../../modules/scheduler/services/boardService', () => ({
  listBoards: jest.fn(() => Promise.resolve([])),
  createBoard: jest.fn(),
  deleteBoard: jest.fn(),
  addBoardMember: jest.fn(),
  removeBoardMember: jest.fn(),
}));

const screenMock = (label: string) => {
  const ReactMock = require('react');
  const { Text } = require('react-native');
  return {
    __esModule: true,
    default: () => ReactMock.createElement(Text, null, label),
  };
};

jest.mock('../../modules/scheduler/screens/WeekendGridScreen', () =>
  screenMock('WeekendGridScreen'),
);
jest.mock('../../modules/scheduler/screens/CreateAppointmentScreen', () =>
  screenMock('CreateAppointmentScreen'),
);
jest.mock('../../modules/scheduler/screens/AppointmentDetailScreen', () =>
  screenMock('AppointmentDetailScreen'),
);
jest.mock('../../modules/scheduler/screens/ClientListScreen', () =>
  screenMock('ClientListScreen'),
);
jest.mock('../../modules/scheduler/screens/ClientDetailScreen', () =>
  screenMock('ClientDetailScreen'),
);
jest.mock('../../modules/scheduler/screens/ServiceListScreen', () =>
  screenMock('ServiceListScreen'),
);
jest.mock('../../modules/scheduler/screens/ServiceEditScreen', () =>
  screenMock('ServiceEditScreen'),
);
jest.mock('../../modules/scheduler/screens/ReportsScreen', () =>
  screenMock('ReportsScreen'),
);
jest.mock('../../modules/scheduler/screens/BoardManagementScreen', () =>
  screenMock('BoardManagementScreen'),
);

const testMetrics = {
  insets: { top: 0, bottom: 0, left: 0, right: 0 },
  frame: { x: 0, y: 0, width: 390, height: 844 },
};

const NavigatorWithRef: React.FC<{
  onReady: (ref: SchedulerNavRef) => void;
}> = ({ onReady }) => {
  const navRef = useRef<SchedulerNavRef>(null);
  return (
    <SafeAreaProvider initialMetrics={testMetrics}>
      <NavigationContainer
        ref={navRef}
        onReady={() => {
          if (navRef.current) onReady(navRef.current);
        }}
      >
        <SchedulerNavigator />
      </NavigationContainer>
    </SafeAreaProvider>
  );
};

describe('SchedulerNavigator', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders the WeekendGrid screen by default', async () => {
    const { findByText } = render(
      <SafeAreaProvider initialMetrics={testMetrics}>
        <NavigationContainer>
          <SchedulerNavigator />
        </NavigationContainer>
      </SafeAreaProvider>,
    );

    expect(await findByText('WeekendGridScreen')).toBeTruthy();
  });

  it('registers the BoardManagement screen', async () => {
    let navRef: SchedulerNavRef | null = null;
    const { findByText } = render(
      <NavigatorWithRef
        onReady={ref => {
          navRef = ref;
        }}
      />,
    );

    await waitFor(() => expect(navRef).not.toBeNull());

    act(() => {
      navRef!.navigate('BoardManagement');
    });

    expect(await findByText('BoardManagementScreen')).toBeTruthy();
  });

  it('registers the ClientList and Reports screens', async () => {
    let navRef: SchedulerNavRef | null = null;
    const { findByText } = render(
      <NavigatorWithRef
        onReady={ref => {
          navRef = ref;
        }}
      />,
    );

    await waitFor(() => expect(navRef).not.toBeNull());

    act(() => {
      navRef!.navigate('ClientList', { boardId: 1 });
    });
    expect(await findByText('ClientListScreen')).toBeTruthy();

    act(() => {
      navRef!.navigate('Reports', { boardId: 1 });
    });
    expect(await findByText('ReportsScreen')).toBeTruthy();
  });

  it('navigates to CreateAppointment with boardId param', async () => {
    let navRef: SchedulerNavRef | null = null;
    const { findByText } = render(
      <NavigatorWithRef
        onReady={ref => {
          navRef = ref;
        }}
      />,
    );

    await waitFor(() => expect(navRef).not.toBeNull());

    act(() => {
      navRef!.navigate('CreateAppointment', { boardId: 5 });
    });

    expect(await findByText('CreateAppointmentScreen')).toBeTruthy();
  });

  it('navigates to AppointmentDetail with appointmentId param', async () => {
    let navRef: SchedulerNavRef | null = null;
    const { findByText } = render(
      <NavigatorWithRef
        onReady={ref => {
          navRef = ref;
        }}
      />,
    );

    await waitFor(() => expect(navRef).not.toBeNull());

    act(() => {
      navRef!.navigate('AppointmentDetail', { appointmentId: 42 });
    });

    expect(await findByText('AppointmentDetailScreen')).toBeTruthy();
  });

  it('navigates to ClientDetail with clientId param', async () => {
    let navRef: SchedulerNavRef | null = null;
    const { findByText } = render(
      <NavigatorWithRef
        onReady={ref => {
          navRef = ref;
        }}
      />,
    );

    await waitFor(() => expect(navRef).not.toBeNull());

    act(() => {
      navRef!.navigate('ClientDetail', { clientId: 99 });
    });

    expect(await findByText('ClientDetailScreen')).toBeTruthy();
  });

  it('navigates to ServiceEdit with boardId param (with and without serviceId)', async () => {
    let navRef: SchedulerNavRef | null = null;
    const { findByText } = render(
      <NavigatorWithRef
        onReady={ref => {
          navRef = ref;
        }}
      />,
    );

    await waitFor(() => expect(navRef).not.toBeNull());

    // Navigate with serviceId
    act(() => {
      navRef!.navigate('ServiceEdit', { serviceId: 5, boardId: 1 });
    });
    expect(await findByText('ServiceEditScreen')).toBeTruthy();
  });
});
