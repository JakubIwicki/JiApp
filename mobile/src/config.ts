/**
 * Application configuration.
 *
 * API base URL. For emulator the default works out of the box.
 * For physical devices, set JIAPP_API_URL at build time
 * (inlined by babel-plugin-transform-inline-environment-variables).
 *
 * Copy mobile/.env.example to mobile/.env and set your dev machine IP.
 */
export const API_BASE_URL: string =
  process.env.JIAPP_API_URL || 'https://10.0.2.2:6700/api/v1';
