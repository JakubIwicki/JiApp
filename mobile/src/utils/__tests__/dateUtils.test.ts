import { formatDate } from '../dateUtils';

describe('formatDate', () => {
  it('formats an ISO date string to DD.MM.YYYY', () => {
    expect(formatDate('2026-05-21T10:30:00Z')).toBe('21.05.2026');
  });

  it('pads single-digit day and month with leading zeros', () => {
    expect(formatDate('2026-01-05T08:15:00Z')).toBe('05.01.2026');
  });

  it('handles end of year dates', () => {
    expect(formatDate('2025-12-31T23:59:00Z')).toBe('31.12.2025');
  });

  it('handles beginning of year dates', () => {
    expect(formatDate('2026-01-01T00:00:00Z')).toBe('01.01.2026');
  });
});
