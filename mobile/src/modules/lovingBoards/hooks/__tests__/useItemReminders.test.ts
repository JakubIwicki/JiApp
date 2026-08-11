import { renderHook, act } from '@testing-library/react-native';
import notifee, { TriggerType, AndroidImportance } from '@notifee/react-native';
import useItemReminders from '../useItemReminders';
import type { Item } from '../../types/api';

jest.mock('@notifee/react-native', () => ({
  __esModule: true,
  default: {
    requestPermission: jest.fn(() => Promise.resolve({ authorized: true })),
    createChannel: jest.fn(() => Promise.resolve('')),
    getTriggerNotifications: jest.fn(() => Promise.resolve([])),
    cancelNotification: jest.fn(() => Promise.resolve()),
    createTriggerNotification: jest.fn(() => Promise.resolve('')),
  },
  TriggerType: { TIMESTAMP: 0, INTERVAL: 1 },
  AndroidImportance: { NONE: 0, MIN: 1, LOW: 2, DEFAULT: 3, HIGH: 4 },
}));

jest.mock('react-i18next', () => {
  const actual = jest.requireActual('react-i18next');
  return {
    ...actual,
    useTranslation: () => ({
      t: (key: string) => key,
    }),
  };
});

const REMINDER_HOURS = 24;

const mockRequestPermission = notifee.requestPermission as jest.Mock;
const mockCreateChannel = notifee.createChannel as jest.Mock;
const mockGetTriggerNotifications =
  notifee.getTriggerNotifications as jest.Mock;
const mockCancelNotification = notifee.cancelNotification as jest.Mock;
const mockCreateTriggerNotification =
  notifee.createTriggerNotification as jest.Mock;

const defaultGetTriggerNotifications =
  mockGetTriggerNotifications.getMockImplementation();

const makeItem = (id: number, overrides: Partial<Item> = {}): Item => ({
  id,
  boardId: 1,
  title: `Item ${id}`,
  quantity: null,
  category: null,
  note: null,
  assigneeUserId: null,
  expiryDate: null,
  isRecurring: false,
  status: 'Needed',
  addedByUserId: 1,
  completedByUserId: null,
  createdAt: '2026-01-01T00:00:00.000Z',
  updatedAt: '2026-01-01T00:00:00.000Z',
  removedAt: null,
  ...overrides,
});

/** Flush pending microtasks so the async reconcile settles */
const flushMicrotasks = async (count = 10): Promise<void> => {
  for (let i = 0; i < count; i++) {
    await act(async () => {});
  }
};

beforeEach(() => {
  jest.clearAllMocks();
  if (defaultGetTriggerNotifications) {
    mockGetTriggerNotifications.mockImplementation(
      defaultGetTriggerNotifications,
    );
  }
});

describe('useItemReminders', () => {
  it('creates trigger notifications for needed items with future due dates', async () => {
    const expiryDate = new Date(Date.now() + 48 * 60 * 60 * 1000);
    const expectedTimestamp =
      expiryDate.getTime() - REMINDER_HOURS * 60 * 60 * 1000;
    const items: Item[] = [
      makeItem(1, {
        title: 'Mleko',
        expiryDate: expiryDate.toISOString(),
        note: '2l',
      }),
      makeItem(2, {
        title: 'Chleb',
        expiryDate: new Date(Date.now() + 72 * 60 * 60 * 1000).toISOString(),
      }),
    ];

    renderHook(() => useItemReminders(items, 'Dom'));
    await flushMicrotasks();

    expect(mockRequestPermission).toHaveBeenCalledTimes(1);
    expect(mockCreateChannel).toHaveBeenCalledWith({
      id: 'lovingboards-reminders',
      name: 'lovingBoards.reminders.channelName',
      importance: AndroidImportance.DEFAULT,
    });
    expect(mockCreateTriggerNotification).toHaveBeenCalledTimes(2);
    expect(mockCreateTriggerNotification).toHaveBeenCalledWith(
      {
        id: 'lovingboard-1',
        title: 'lovingBoards.reminders.notificationTitle',
        body: '2l',
        android: {
          channelId: 'lovingboards-reminders',
          smallIcon: 'ic_launcher',
        },
      },
      { type: TriggerType.TIMESTAMP, timestamp: expectedTimestamp },
    );
  });

  it('does not create notifications for items without due dates or not needed', async () => {
    const items: Item[] = [
      makeItem(1, { expiryDate: null }),
      makeItem(2, {
        expiryDate: new Date(Date.now() + 48 * 60 * 60 * 1000).toISOString(),
        status: 'Completed',
      }),
    ];

    renderHook(() => useItemReminders(items, 'Dom'));
    await flushMicrotasks();

    expect(mockCreateTriggerNotification).not.toHaveBeenCalled();
  });

  it('cancels the existing notification when the item stops being needed', async () => {
    mockGetTriggerNotifications.mockResolvedValue([
      { notification: { id: 'lovingboard-1' } },
    ]);
    const futureDate = new Date(Date.now() + 48 * 60 * 60 * 1000).toISOString();
    const items: Item[] = [
      makeItem(1, { title: 'Mleko', expiryDate: futureDate }),
    ];

    const { rerender } = renderHook(
      (props: { items: readonly Item[] }) =>
        useItemReminders(props.items, 'Dom'),
      { initialProps: { items } },
    );
    await flushMicrotasks();
    mockCreateTriggerNotification.mockClear();
    mockCancelNotification.mockClear();

    rerender({ items: [{ ...items[0]!, status: 'Completed' }] });
    await flushMicrotasks();

    expect(mockCancelNotification).toHaveBeenCalledWith('lovingboard-1');
    expect(mockCreateTriggerNotification).not.toHaveBeenCalled();
  });

  it('does not re-schedule when re-rendered with an unchanged item list', async () => {
    const futureDate = new Date(Date.now() + 48 * 60 * 60 * 1000).toISOString();
    const items: Item[] = [
      makeItem(1, { title: 'Mleko', expiryDate: futureDate }),
    ];

    const { rerender } = renderHook(
      (props: { items: readonly Item[] }) =>
        useItemReminders(props.items, 'Dom'),
      { initialProps: { items } },
    );
    await flushMicrotasks();
    expect(mockCreateTriggerNotification).toHaveBeenCalledTimes(1);

    mockCreateTriggerNotification.mockClear();
    mockGetTriggerNotifications.mockClear();

    // New array reference with identical content — the scheduleKey guard holds
    rerender({ items: [{ ...items[0]! }] });
    await flushMicrotasks();

    expect(mockCreateTriggerNotification).not.toHaveBeenCalled();
    expect(mockGetTriggerNotifications).not.toHaveBeenCalled();
  });
});
