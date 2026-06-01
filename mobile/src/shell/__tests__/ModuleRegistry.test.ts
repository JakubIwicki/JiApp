import { moduleRegistry, getModule, getEnabledModules, registerModule } from '../ModuleRegistry';
import type { JiModule } from '../types';

describe('ModuleRegistry', () => {
  beforeEach(() => {
    while (moduleRegistry.length > 0) moduleRegistry.pop();
    registerModule({
      id: 'yt-downloader',
      name: 'modules.ytdownloader',
      icon: 'search',
      component: null as unknown as React.ComponentType<any>,
      enabled: true,
    });
  });

  describe('getModule', () => {
    it('returns the module when it exists', () => {
      const mod = getModule('yt-downloader');
      expect(mod).toBeDefined();
      expect(mod?.id).toBe('yt-downloader');
      expect(mod?.name).toBe('modules.ytdownloader');
      expect(mod?.icon).toBe('search');
      expect(mod?.enabled).toBe(true);
    });

    it('returns undefined when module does not exist', () => {
      expect(getModule('non-existent')).toBeUndefined();
    });
  });

  describe('getEnabledModules', () => {
    it('returns only enabled modules', () => {
      const enabled = getEnabledModules();
      expect(enabled.length).toBe(1);
      expect(enabled[0].id).toBe('yt-downloader');
    });

    it('excludes disabled modules', () => {
      registerModule({
        id: 'pdfsuite',
        name: 'modules.pdfsuite',
        icon: 'settings',
        component: null as unknown as React.ComponentType<any>,
        enabled: false,
      });
      const enabled = getEnabledModules();
      expect(enabled.length).toBe(1);
      expect(enabled.find((m) => m.id === 'pdfsuite')).toBeUndefined();
    });
  });

  describe('registerModule', () => {
    it('replaces duplicate module by id', () => {
      registerModule({
        id: 'yt-downloader',
        name: 'updated',
        icon: 'search',
        component: null as unknown as React.ComponentType<any>,
        enabled: false,
      });
      expect(moduleRegistry.length).toBe(1);
      const mod = getModule('yt-downloader');
      expect(mod?.name).toBe('updated');
      expect(mod?.enabled).toBe(false);
    });
  });
});
