// Default for Android emulator (10.0.2.2 → host localhost).
// Overwritten by build-apk.sh with the value from mobile/.env for physical devices.
// Do not commit your real IP here — keep it in mobile/.env (gitignored).
export const API_BASE_URL: string = 'https://10.0.2.2:6700/api/v1';
