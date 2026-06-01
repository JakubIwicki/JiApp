import type { JiModule } from './types';

export const moduleRegistry: JiModule[] = [];

export const registerModule = (mod: JiModule): void => {
  const existing = moduleRegistry.findIndex((m) => m.id === mod.id);
  if (existing >= 0) {
    moduleRegistry[existing] = mod;
  } else {
    moduleRegistry.push(mod);
  }
};

export const getModule = (id: string): JiModule | undefined =>
  moduleRegistry.find((m) => m.id === id);

export const getEnabledModules = (): JiModule[] =>
  moduleRegistry.filter((m) => m.enabled);

