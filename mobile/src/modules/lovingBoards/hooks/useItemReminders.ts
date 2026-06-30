import { useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import notifee, { TriggerType, AndroidImportance } from '@notifee/react-native';
import type { TimestampTrigger } from '@notifee/react-native';
import type { Item } from '../types/api';

const CHANNEL_ID = 'lovingboards-reminders';
const REMINDER_HOURS = 24;
const NOTIF_ID_PREFIX = 'lovingboard-';

const useItemReminders = (
  items: readonly Item[],
  boardName: string | undefined,
): void => {
  const { t } = useTranslation();
  const scheduleKeyRef = useRef<string>('');

  // Set up permission + channel on mount
  useEffect(() => {
    notifee.requestPermission();
    notifee.createChannel({
      id: CHANNEL_ID,
      name: t('lovingBoards.reminders.channelName'),
      importance: AndroidImportance.DEFAULT,
    });
  }, [t]); // mount only — t is stable

  // Reconcile schedule when items or boardName change
  useEffect(() => {
    if (!boardName) return;

    const reconcile = async () => {
      const keyParts = items
        .filter(i => i.expiryDate && i.status === 'Needed')
        .map(i => `${i.id}:${i.expiryDate}:${i.status}`)
        .join('|');
      const scheduleKey = `${boardName}|${keyParts}`;

      if (scheduleKey === scheduleKeyRef.current) return;
      scheduleKeyRef.current = scheduleKey;

      const scheduled = await notifee.getTriggerNotifications();
      const existingIds = new Set(
        scheduled
          .map(n => n.notification?.id)
          .filter((id): id is string => !!id),
      );

      for (const item of items) {
        const notifId = `${NOTIF_ID_PREFIX}${item.id}`;

        if (item.status !== 'Needed' || !item.expiryDate) {
          if (existingIds.has(notifId)) {
            await notifee.cancelNotification(notifId);
          }
          continue;
        }

        const expiryDate = new Date(item.expiryDate);
        if (Number.isNaN(expiryDate.getTime())) {
          if (existingIds.has(notifId)) {
            await notifee.cancelNotification(notifId);
          }
          continue;
        }

        const now = Date.now();

        // Don't schedule for past expiry dates
        if (expiryDate.getTime() <= now) {
          if (existingIds.has(notifId)) {
            await notifee.cancelNotification(notifId);
          }
          continue;
        }

        const remindAt = expiryDate.getTime() - REMINDER_HOURS * 60 * 60 * 1000;

        // Don't schedule if reminder time already passed
        if (remindAt <= now) continue;

        // Cancel existing before re-scheduling
        if (existingIds.has(notifId)) {
          await notifee.cancelNotification(notifId);
        }

        const trigger: TimestampTrigger = {
          type: TriggerType.TIMESTAMP,
          timestamp: remindAt,
        };

        await notifee.createTriggerNotification(
          {
            id: notifId,
            title: t('lovingBoards.reminders.notificationTitle', {
              itemTitle: item.title,
              boardName,
            }),
            body: item.note ?? undefined,
            android: {
              channelId: CHANNEL_ID,
              smallIcon: 'ic_launcher',
            },
          },
          trigger,
        );
      }
    };

    reconcile();
  }, [items, boardName, t]);
};

export default useItemReminders;
