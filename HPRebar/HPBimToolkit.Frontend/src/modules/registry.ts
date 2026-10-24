import type { IconName } from '../ui/Icon';

/**
 * The toolkit's modules. A new module adds an entry here and its page in App's route switch; the bridge, UI kit and
 * theme are shared. Modules that are not built yet show in the navigation as "Sắp có".
 */
export interface ModuleEntry {
  id: string;
  label: string;
  icon: IconName;
  isAvailable: boolean;
}

export const modules: ModuleEntry[] = [
  { id: 'dashboard', label: 'Dashboard', icon: 'layers', isAvailable: true },
  { id: 'elements', label: 'Element Explorer', icon: 'search', isAvailable: false },
  { id: 'parameters', label: 'Parameter Manager', icon: 'tool', isAvailable: false },
  { id: 'reports', label: 'Reports', icon: 'document', isAvailable: false },
];

export const defaultModuleId = 'dashboard';

/** Reads `#/<module>`; anything unknown or unavailable falls back to the dashboard. */
export function moduleFromHash(hash: string): string {
  const id = hash.replace(/^#\/?/, '');
  return modules.some((module) => module.id === id && module.isAvailable) ? id : defaultModuleId;
}
