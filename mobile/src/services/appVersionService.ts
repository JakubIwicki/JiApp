import { API_BASE_URL } from '../config';
import { AppVersionInfoSchema } from '../types/schemas';
import type { AppVersionInfo } from '../types/api';

// Pre-auth boot probe — deliberately NOT routed through apiClient (Bearer/
// refresh interceptors + 30s timeout are wrong for this gate). Caller fail-opens.
export const fetchAppVersionInfo = async (
  signal?: AbortSignal,
): Promise<AppVersionInfo> => {
  const response = await fetch(`${API_BASE_URL}/app/version`, { signal });

  if (!response.ok) {
    throw new Error(`Version check failed with HTTP ${response.status}`);
  }

  return AppVersionInfoSchema.parse(await response.json());
};
