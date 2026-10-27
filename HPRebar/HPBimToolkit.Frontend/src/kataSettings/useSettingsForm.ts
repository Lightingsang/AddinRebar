import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { errorText } from '../bridge/errorText';
import { setIn } from './formPath';
import type { KataSettingsError, KataSettingsForm, KataSettingsFormChoices, KataTheme, SettingsBridge } from './settingsContracts';

/** How long the page waits after the last keystroke before asking the host what is wrong. */
export const VALIDATE_DELAY_MS = 300;

export type SaveOutcome = { saved: true; fileFailed: boolean } | { saved: false; errors: KataSettingsError[] };

export interface SettingsController {
  /** Null until the host has answered settings.load. */
  form: KataSettingsForm | null;
  choices: KataSettingsFormChoices | null;
  /** Revit's theme: the page follows it until the viewer picks one. */
  hostTheme: KataTheme;
  notice: string | null;
  loadError: string | null;
  /** Wrong values by form path, as the host last reported them. */
  errors: Record<string, string>;
  /** The form differs from what the settings file holds. */
  dirty: boolean;
  /** What the settings file holds (the form as last loaded or saved), to mark the values changed since. */
  savedForm: KataSettingsForm | null;
  busy: boolean;
  /** Changes whenever the whole form is replaced (defaults), so inputs drop the text they were holding. */
  revision: number;
  update(path: string, value: unknown): void;
  defaults(): Promise<void>;
  save(): Promise<SaveOutcome>;
  close(): void;
}

/**
 * The settings form: loaded once, edited by path, checked by the host a moment after each change (the host's rules
 * are the only rules; a late answer for an older form is dropped), saved and closed through the bridge.
 */
export function useSettingsForm(bridge: SettingsBridge, notify: (text: string, tone?: 'info' | 'success' | 'error') => void): SettingsController {
  const [form, setForm] = useState<KataSettingsForm | null>(null);
  const [saved, setSaved] = useState<string>('');
  const [choices, setChoices] = useState<KataSettingsFormChoices | null>(null);
  const [hostTheme, setHostTheme] = useState<KataTheme>('light');
  const [notice, setNotice] = useState<string | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState(false);
  const [revision, setRevision] = useState(0);
  const checked = useRef(0);
  const timer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  useEffect(() => {
    const off = bridge.on('settings.theme', (event) => setHostTheme(event.theme));
    bridge
      .request('settings.load')
      .then((state) => {
        setHostTheme(state.theme);
        setForm(state.snapshot.form);
        setSaved(JSON.stringify(state.snapshot.form));
        setChoices(state.snapshot.choices);
        setNotice(state.snapshot.notice);
      })
      .catch((error: unknown) => setLoadError(errorText(error)));
    return () => {
      off();
      clearTimeout(timer.current);
    };
  }, [bridge]);

  const showErrors = useCallback((list: KataSettingsError[]) => setErrors(Object.fromEntries(list.map((e) => [e.field, e.message]))), []);

  /** Asks the host about `next` after a pause; only the answer for the newest form is shown. */
  const scheduleCheck = useCallback(
    (next: KataSettingsForm) => {
      clearTimeout(timer.current);
      const serial = ++checked.current;
      timer.current = setTimeout(() => {
        bridge
          .request('settings.validate', next)
          .then((result) => {
            if (serial === checked.current) showErrors(result.errors);
          })
          .catch(() => undefined); // the save reports what the check could not
      }, VALIDATE_DELAY_MS);
    },
    [bridge, showErrors],
  );

  const update = useCallback(
    (path: string, value: unknown) => {
      if (!form) return;
      const next = setIn(form, path, value);
      setForm(next);
      scheduleCheck(next);
    },
    [form, scheduleCheck],
  );

  const defaults = useCallback(async () => {
    try {
      const snapshot = await bridge.request('settings.defaults');
      setForm(snapshot.form);
      setNotice(null);
      setRevision((r) => r + 1);
      scheduleCheck(snapshot.form);
    } catch (error) {
      notify(errorText(error), 'error');
    }
  }, [bridge, notify, scheduleCheck]);

  const save = useCallback(async (): Promise<SaveOutcome> => {
    if (!form) return { saved: false, errors: [] };
    clearTimeout(timer.current);
    checked.current++;
    setBusy(true);
    try {
      const result = await bridge.request('settings.save', form);
      showErrors(result.errors);
      if (!result.saved) return { saved: false, errors: result.errors };
      setSaved(JSON.stringify(form));
      setNotice(null);
      return { saved: true, fileFailed: result.fileFailed };
    } catch (error) {
      notify(errorText(error), 'error');
      return { saved: false, errors: [] };
    } finally {
      setBusy(false);
    }
  }, [bridge, form, notify, showErrors]);

  const close = useCallback(() => void bridge.request('settings.close').catch((error: unknown) => notify(errorText(error), 'error')), [bridge, notify]);

  const dirty = useMemo(() => form !== null && JSON.stringify(form) !== saved, [form, saved]);
  const savedForm = useMemo(() => (saved ? (JSON.parse(saved) as KataSettingsForm) : null), [saved]);

  return { form, choices, hostTheme, notice, loadError, errors, dirty, savedForm, busy, revision, update, defaults, save, close };
}
