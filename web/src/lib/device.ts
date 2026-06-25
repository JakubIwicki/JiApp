/**
 * Lightweight device detection — no external deps.
 * The only consumer is the Download section's device branch.
 */

/** Returns true when the visitor is on an Android device. */
export function isAndroid(): boolean {
  if (typeof navigator === "undefined") return false;
  return /android/i.test(navigator.userAgent);
}
