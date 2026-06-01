/**
 * Application configuration.
 *
 * API base URL. Override at build time by setting JIAPP_API_URL
 * (inlined by babel-plugin-transform-inline-environment-variables).
 *
 * Defaults to the Android emulator loopback (maps to host localhost).
 * For physical devices or production builds, set JIAPP_API_URL.
 */
export const API_BASE_URL: string =
  process.env.JIAPP_API_URL || 'https://192.168.100.105:6700/api/v1';
