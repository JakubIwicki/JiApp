// Default for Android emulator (10.0.2.2 → host localhost).
// Overwritten by build-apk.sh with the value from mobile/.env for physical devices.
// Do not commit your real IP here — keep it in mobile/.env (gitignored).
export const API_BASE_URL: string = 'https://10.0.2.2:6700/api/v1';

// Wake API URL — Lambda via API Gateway that starts the EC2 instance.
// Overwritten by build-apk.sh from mobile/.env (JIAPP_WAKE_API_URL).
export const WAKE_API_URL: string =
  'https://abc123.execute-api.eu-central-1.amazonaws.com';
