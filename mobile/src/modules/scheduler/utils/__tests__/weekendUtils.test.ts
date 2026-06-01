import { getWeekendDates, formatWeekendLabel, isWeekend } from '../weekendUtils';

describe('getWeekendDates', () => {
  it('returns current weekend when date is Saturday', () => {
    // 2026-05-23 is a Saturday
    const result = getWeekendDates(new Date(2026, 4, 23));
    expect(result.saturday).toBe('2026-05-23');
    expect(result.sunday).toBe('2026-05-24');
  });

  it('returns current weekend when date is Sunday', () => {
    // 2026-05-24 is a Sunday
    const result = getWeekendDates(new Date(2026, 4, 24));
    expect(result.saturday).toBe('2026-05-23');
    expect(result.sunday).toBe('2026-05-24');
  });

  it('returns upcoming weekend when date is Monday', () => {
    // 2026-05-25 is a Monday
    const result = getWeekendDates(new Date(2026, 4, 25));
    expect(result.saturday).toBe('2026-05-30');
    expect(result.sunday).toBe('2026-05-31');
  });

  it('returns upcoming weekend when date is Wednesday', () => {
    // 2026-05-27 is a Wednesday
    const result = getWeekendDates(new Date(2026, 4, 27));
    expect(result.saturday).toBe('2026-05-30');
    expect(result.sunday).toBe('2026-05-31');
  });

  it('returns upcoming weekend when date is Friday', () => {
    // 2026-05-29 is a Friday
    const result = getWeekendDates(new Date(2026, 4, 29));
    expect(result.saturday).toBe('2026-05-30');
    expect(result.sunday).toBe('2026-05-31');
  });

  it('wraps correctly at month boundary', () => {
    // 2026-05-30 is Saturday, 2026-05-31 is Sunday
    const result = getWeekendDates(new Date(2026, 4, 28)); // Thursday May 28
    expect(result.saturday).toBe('2026-05-30');
    expect(result.sunday).toBe('2026-05-31');
  });

  it('handles year boundary', () => {
    // 2026-12-31 is a Thursday
    const result = getWeekendDates(new Date(2026, 11, 31));
    expect(result.saturday).toBe('2027-01-02');
    expect(result.sunday).toBe('2027-01-03');
  });
});

describe('formatWeekendLabel', () => {
  it('formats dates in same month', () => {
    expect(formatWeekendLabel('2026-05-23', '2026-05-24')).toBe('May 23-24, 2026');
  });

  it('formats dates across different months', () => {
    expect(formatWeekendLabel('2026-05-30', '2026-05-31')).toBe('May 30-31, 2026');
  });
});

describe('isWeekend', () => {
  it('returns true for Saturday', () => {
    expect(isWeekend(new Date(2026, 4, 23))).toBe(true);
  });

  it('returns true for Sunday', () => {
    expect(isWeekend(new Date(2026, 4, 24))).toBe(true);
  });

  it('returns false for Monday', () => {
    expect(isWeekend(new Date(2026, 4, 25))).toBe(false);
  });

  it('returns false for Wednesday', () => {
    expect(isWeekend(new Date(2026, 4, 27))).toBe(false);
  });
});
