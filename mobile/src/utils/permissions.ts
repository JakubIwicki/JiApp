import type { ModuleId } from '../navigation/types';

export const PERMISSION_TO_MODULE: Record<string, ModuleId> = {
  'scheduler.access': 'Scheduler',
  'ytdownloader.access': 'YtDownloader',
  'lovingboards.access': 'LovingBoards',
};

export function modulesFromPermissions(permissions: string[]): ModuleId[] {
  return permissions
    .map(p => PERMISSION_TO_MODULE[p])
    .filter((m): m is ModuleId => m !== undefined);
}

export function isAdminRole(roles: string[]): boolean {
  return roles.includes('Admin');
}
