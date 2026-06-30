// Stub for @notifee/react-native — redirects via moduleNameMapper to prevent
// import-time module-evaluation crash (the real module creates a native module
// instance at scope level, which throws in Jest).
const notifee = {
  requestPermission: jest.fn(() => Promise.resolve({ authorized: true })),
  createChannel: jest.fn(() => Promise.resolve('')),
  getTriggerNotifications: jest.fn(() => Promise.resolve([])),
  cancelNotification: jest.fn(() => Promise.resolve()),
  createTriggerNotification: jest.fn(() => Promise.resolve('')),
};

export default notifee;

export const TriggerType = {
  TIMESTAMP: 0,
  INTERVAL: 1,
};

export const AndroidImportance = {
  NONE: 0,
  MIN: 1,
  LOW: 2,
  DEFAULT: 3,
  HIGH: 4,
};
