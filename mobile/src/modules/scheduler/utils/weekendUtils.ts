/**
 * Get the Saturday and Sunday date strings (YYYY-MM-DD) for the weekend
 * containing (or following) the given reference date.
 *
 * - If referenceDate is Saturday or Sunday, return that weekend.
 * - If Monday–Friday, return the upcoming weekend.
 */
export function getWeekendDates(referenceDate: Date): {
  saturday: string;
  sunday: string;
} {
  const dayOfWeek = referenceDate.getDay(); // 0=Sun, 1=Mon, ..., 6=Sat

  let saturday: Date;
  if (dayOfWeek === 6) {
    // Saturday
    saturday = new Date(referenceDate);
  } else if (dayOfWeek === 0) {
    // Sunday — go back to previous Saturday
    saturday = new Date(referenceDate);
    saturday.setDate(saturday.getDate() - 1);
  } else {
    // Monday–Friday — go forward to upcoming Saturday
    saturday = new Date(referenceDate);
    saturday.setDate(saturday.getDate() + (6 - dayOfWeek));
  }

  const sunday = new Date(saturday);
  sunday.setDate(saturday.getDate() + 1);

  return {
    saturday: formatDate(saturday),
    sunday: formatDate(sunday),
  };
}

function formatDate(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

/**
 * Format a human-readable weekend label, e.g. "May 24-25, 2026".
 */
export function formatWeekendLabel(saturday: string, sunday: string): string {
  const satParts = saturday.split('-').map(Number);
  const sunParts = sunday.split('-').map(Number);

  const satDate = new Date(satParts[0], satParts[1] - 1, satParts[2]);
  const sunDate = new Date(sunParts[0], sunParts[1] - 1, sunParts[2]);

  const monthNames = [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December',
  ];

  const month = monthNames[satDate.getMonth()];
  const satDay = satDate.getDate();
  const sunDay = sunDate.getDate();
  const year = satDate.getFullYear();

  // Same month: "May 24-25, 2026"
  // Different month: "May 31 - June 1, 2026"
  if (satDate.getMonth() === sunDate.getMonth()) {
    return `${month} ${satDay}-${sunDay}, ${year}`;
  }
  const sunMonth = monthNames[sunDate.getMonth()];
  return `${month} ${satDay} - ${sunMonth} ${sunDay}, ${year}`;
}

/**
 * Check if a date falls on a weekend (Saturday or Sunday).
 */
export function isWeekend(date: Date): boolean {
  const day = date.getDay();
  return day === 0 || day === 6;
}
