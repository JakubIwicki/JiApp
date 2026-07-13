/**
 * Creates a mock function that is jest.fn() when jest is available (tests),
 * or a plain passthrough function otherwise (storybook-web / Vite).
 *
 * The return type exposes jest.Mock members optionally so downstream code
 * guarded by `typeof jest !== 'undefined'` can call .mockClear() etc.
 */
type AnyFn = (...args: any[]) => any;

export interface MockFn<T extends AnyFn> extends Function {
  (...args: Parameters<T>): ReturnType<T>;
  mockClear: () => void;
  mock: {
    calls: any[][];
    results: any[];
    instances: any[];
    contexts: any[];
    lastCall: any[];
  };
  mockImplementation: (fn: T) => void;
  mockResolvedValue: (value: Awaited<ReturnType<T>>) => void;
  mockRejectedValue: (error: any) => void;
  mockReturnValue: (value: ReturnType<T>) => void;
}

function noop() {}

export function createMockFn<T extends AnyFn>(_impl: T): MockFn<T> {
  if (typeof jest !== 'undefined') {
    return jest.fn(_impl) as unknown as MockFn<T>;
  }
  // Attach no-op mock members so TypeScript doesn't complain about optional
  // calls inside `typeof jest !== 'undefined'` guards in reset() functions.
  const fn = _impl as unknown as Record<string, unknown>;
  fn.mockClear = noop;
  fn.mockImplementation = noop;
  fn.mockResolvedValue = noop;
  fn.mockRejectedValue = noop;
  fn.mockReturnValue = noop;
  fn.mock = {
    calls: [],
    results: [],
    instances: [],
    contexts: [],
    lastCall: [],
  };
  return fn as unknown as MockFn<T>;
}
