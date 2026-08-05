/**
 * Returns the Saturday and Sunday of the weekend containing `reference`
 * (defaults to now), formatted as YYYY-MM-DD.
 *
 * Tests and mocks that need a stable weekend pass a fixed reference date so
 * their expectations never depend on the wall clock.
 */
export function getThisWeekend(reference: Date = new Date()): {
  saturday: string;
  sunday: string;
} {
  const dayOfWeek = reference.getDay();
  // 0=Sun, 1=Mon, ..., 6=Sat
  const daysUntilSaturday = dayOfWeek === 6 ? 0 : (6 - dayOfWeek + 7) % 7;
  const saturday = new Date(reference);
  saturday.setDate(reference.getDate() + daysUntilSaturday);

  const sunday = new Date(saturday);
  sunday.setDate(saturday.getDate() + 1);

  const fmt = (d: Date) => {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  };

  return { saturday: fmt(saturday), sunday: fmt(sunday) };
}
