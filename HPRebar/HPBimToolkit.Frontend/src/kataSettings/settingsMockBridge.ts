import { EventHub } from '../bridge/eventHub';
import type {
  KataSettingsError,
  KataSettingsForm,
  KataSettingsSnapshot,
  KataTheme,
  SettingsBridge,
  SettingsCommandMap,
  SettingsEventMap,
} from './settingsContracts';

/**
 * Stands in for Revit in a normal browser and in the tests: opens a snapshot (a fixture written by HPRebar.Core.Tests),
 * keeps what was saved and closed. It does not know the host's rules: an empty number is refused, anything else only
 * when `refuse` says so.
 */
export class SettingsMockBridge implements SettingsBridge {
  readonly isMock = true;
  readonly calls: { command: keyof SettingsCommandMap; payload: unknown }[] = [];
  saved: KataSettingsForm | null = null;
  closed = false;

  private readonly events = new EventHub<SettingsEventMap>();
  private readonly latencyMs: number;
  private readonly theme: KataTheme;
  private snapshot: KataSettingsSnapshot;
  private readonly defaults: KataSettingsSnapshot;
  private readonly refuse: KataSettingsError[];
  private readonly fileFails: boolean;

  /**
   * @param options.snapshot What the settings file holds when the page opens.
   * @param options.defaults What "Mặc định" answers (the snapshot itself when absent).
   * @param options.refuse Errors the host would report for every form.
   * @param options.fileFails The settings file cannot be written.
   */
  constructor(options: {
    snapshot: KataSettingsSnapshot;
    defaults?: KataSettingsSnapshot;
    latencyMs?: number;
    theme?: KataTheme;
    refuse?: KataSettingsError[];
    fileFails?: boolean;
  }) {
    this.snapshot = options.snapshot;
    this.defaults = options.defaults ?? options.snapshot;
    this.latencyMs = options.latencyMs ?? 120;
    this.theme = options.theme ?? 'light';
    this.refuse = options.refuse ?? [];
    this.fileFails = options.fileFails ?? false;
  }

  on: SettingsBridge['on'] = (event, handler) => this.events.on(event, handler);

  /** Revit's theme changed while the window is open. */
  emitTheme(theme: KataTheme): void {
    this.events.emit('settings.theme', { theme });
  }

  async request<C extends keyof SettingsCommandMap & string>(command: C, payload?: SettingsCommandMap[C]['request']): Promise<SettingsCommandMap[C]['response']> {
    this.calls.push({ command, payload });
    await new Promise((resolve) => setTimeout(resolve, this.latencyMs));
    return this.answer(command, payload) as SettingsCommandMap[C]['response'];
  }

  private answer(command: keyof SettingsCommandMap, payload: unknown): unknown {
    switch (command) {
      case 'settings.load':
        return { theme: this.theme, snapshot: this.snapshot };
      case 'settings.validate':
        return { errors: this.check(payload as KataSettingsForm) };
      case 'settings.save': {
        const form = payload as KataSettingsForm;
        const errors = this.check(form);
        if (errors.length > 0) return { saved: false, fileFailed: false, errors };
        this.saved = form;
        this.snapshot = { ...this.snapshot, form, notice: null };
        return { saved: true, fileFailed: this.fileFails, errors: [] };
      }
      case 'settings.defaults':
        return { ...this.defaults, notice: null };
      case 'settings.close':
        this.closed = true;
        return null;
    }
  }

  private check(form: KataSettingsForm): KataSettingsError[] {
    return [...emptyNumbers(form), ...this.refuse];
  }
}

/** A table row left empty, which the host ignores. */
const isBlankRow = (row: object) => Object.values(row).every((v) => v === null || v === 0);

/** Every number left empty, by its form path. */
function emptyNumbers(value: unknown, path = ''): KataSettingsError[] {
  if (value === null) return [{ field: path, message: 'Nhập một số.' }];
  if (Array.isArray(value)) return value.flatMap((item, i) => (isBlankRow(item as object) ? [] : emptyNumbers(item, `${path}[${i}]`)));
  if (typeof value === 'object') return Object.entries(value as object).flatMap(([key, item]) => emptyNumbers(item, path ? `${path}.${key}` : key));
  return [];
}
