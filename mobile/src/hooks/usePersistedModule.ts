import { useCallback, useEffect, useState } from 'react';
import {
  getSelectedModule,
  saveSelectedModule as persistModule,
} from '../services/storageService';
import type { ModuleId } from '../navigation/types';

interface UsePersistedModuleResult {
  /** The last module the user landed in, persisted across launches. */
  persisted: ModuleId | null;
  /** True once the persisted value has been read (or failed to read). */
  resolved: boolean;
  saveSelectedModule: (moduleId: ModuleId) => Promise<void>;
}

const usePersistedModule = (): UsePersistedModuleResult => {
  const [persisted, setPersisted] = useState<ModuleId | null>(null);
  const [resolved, setResolved] = useState(false);

  useEffect(() => {
    let active = true;
    getSelectedModule()
      .then(value => {
        if (active) setPersisted(value);
      })
      .finally(() => {
        if (active) setResolved(true);
      });
    return () => {
      active = false;
    };
  }, []); // mount only

  const saveSelectedModule = useCallback(async (moduleId: ModuleId) => {
    await persistModule(moduleId);
  }, []);

  return { persisted, resolved, saveSelectedModule };
};

export default usePersistedModule;
