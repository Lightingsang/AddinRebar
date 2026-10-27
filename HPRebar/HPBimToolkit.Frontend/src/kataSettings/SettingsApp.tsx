import { useEffect, useRef, useState, type KeyboardEvent } from 'react';
import { Check, Info, RotateCcw, SearchX, TriangleAlert } from 'lucide-react';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { Button } from '@/components/ui/button';
import { Kbd } from '@/components/ui/kbd';
import { useToast } from '../ui/Toast';
import { DetailTab } from './DetailTab';
import { FormContext, SearchContext, fold, idOf } from './fields';
import { tabOf } from './formPath';
import { SETTINGS_STRINGS, SettingsLangContext, type SettingsLang } from './i18n';
import { JointsTab } from './JointsTab';
import { NAV, type GroupId } from './navigation';
import { readChoice, SettingsHeader, writeChoice } from './SettingsHeader';
import type { KataTheme, SettingsBridge } from './settingsContracts';
import { SettingsNav } from './SettingsNav';
import { SpecialTab } from './SpecialTab';
import { useSettingsForm } from './useSettingsForm';
import './settings.css';

const LANG_KEY = 'hp.kata.lang';
const THEME_KEY = 'hp.kata.theme';

/** A section counts as the one being read once its top is this close to the top of the scroll area. */
const SPY_OFFSET_PX = 96;

/**
 * The Kata Settings window: Kata's three tabs as one scrolling page with an outline on the left and a search box. The
 * host checks every change and names the wrong fields; Accept saves and closes, Exit closes (asking first when
 * something was changed).
 */
export function SettingsApp({ bridge }: { bridge: SettingsBridge }) {
  const notify = useToast();
  const settings = useSettingsForm(bridge, notify);
  const [lang, setLang] = useState<SettingsLang>(() => (readChoice(LANG_KEY) === 'en' ? 'en' : 'vi'));
  const [themeChoice, setThemeChoice] = useState<KataTheme | null>(() => {
    const saved = readChoice(THEME_KEY);
    return saved === 'light' || saved === 'dark' ? saved : null;
  });
  const [search, setSearch] = useState('');
  const [active, setActive] = useState(NAV[0]!.sections[0]!.id);
  const [confirmExit, setConfirmExit] = useState(false);
  const scroller = useRef<HTMLElement>(null);
  const theme = themeChoice ?? settings.hostTheme;
  const t = SETTINGS_STRINGS[lang];

  useEffect(() => {
    document.documentElement.classList.toggle('dark', theme === 'dark');
  }, [theme]);
  useEffect(() => {
    document.documentElement.lang = lang;
  }, [lang]);

  const { form, choices, savedForm } = settings;
  if (!form || !choices) {
    return (
      <div className="flex h-full bg-background">
        <div className="m-auto text-[13px] text-muted-foreground">{settings.loadError ? t.loadFailed(settings.loadError) : t.loading}</div>
      </div>
    );
  }

  const errorFields = Object.keys(settings.errors);
  const groupErrors = Object.fromEntries(NAV.map((g) => [g.id, errorFields.filter((f) => tabOf(f) === g.id).length])) as Record<GroupId, number>;
  const changedCount = savedForm ? countDifferences(form, savedForm) : 0;

  // The section being read: the last one whose top has scrolled past the top of the page.
  const spy = () => {
    const area = scroller.current;
    if (!area) return;
    const top = area.getBoundingClientRect().top + SPY_OFFSET_PX;
    let current = NAV[0]!.sections[0]!.id;
    for (const section of area.querySelectorAll<HTMLElement>('[data-section]')) {
      if (section.offsetParent !== null && section.getBoundingClientRect().top <= top) current = section.id;
    }
    setActive(current);
  };
  const pick = (id: string) => {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    setActive(id);
  };

  const accept = async () => {
    const outcome = await settings.save();
    if (outcome.saved) {
      settings.close();
      return;
    }
    const first = outcome.errors[0];
    if (!first) return;
    setSearch('');
    // The wrong box is brought into view and focused once the rows hidden by a search are back.
    setTimeout(() => {
      const box = document.getElementById(idOf(first.field));
      box?.scrollIntoView({ block: 'center' });
      box?.focus();
    });
  };
  const exit = () => (settings.dirty ? setConfirmExit(true) : settings.close());
  const resetDefaults = async () => {
    await settings.defaults();
    notify(t.defaultsLoaded, 'info');
  };

  // Enter in a box accepts and Escape leaves, as the old dialog's default and cancel buttons did; open lists, dialogs
  // and the search box handle their own keys first.
  const onKeyDown = (e: KeyboardEvent<HTMLDivElement>) => {
    if (e.defaultPrevented || confirmExit) return;
    if (e.key === 'Enter' && e.target instanceof HTMLInputElement && e.target.type !== 'search') {
      e.preventDefault();
      void accept();
    } else if (e.key === 'Escape') {
      e.preventDefault();
      exit();
    }
  };

  return (
    <SettingsLangContext.Provider value={lang}>
      <FormContext.Provider value={settings}>
        <SearchContext.Provider value={fold(search.trim())}>
          <div className="flex h-full min-w-0 flex-col bg-background text-foreground" onKeyDown={onKeyDown}>
            <SettingsHeader
              mock={!!bridge.isMock}
              search={search}
              onSearch={setSearch}
              lang={lang}
              onLang={(next) => {
                setLang(next);
                writeChoice(LANG_KEY, next);
              }}
              theme={theme}
              onTheme={(next) => {
                setThemeChoice(next);
                writeChoice(THEME_KEY, next);
              }}
            />
            <div className="flex min-h-0 flex-1">
              <SettingsNav active={active} errors={groupErrors} onPick={pick} />
              <main ref={scroller} onScroll={spy} className="settings-scroll min-w-0 flex-1 overflow-y-auto" data-results>
                <div className="mx-auto grid max-w-3xl gap-6 px-5 pt-4 pb-8">
                  <div className="grid gap-2">
                    <p className="flex gap-2.5 rounded-xl border bg-card px-4 py-2.5 text-[12px] leading-relaxed text-muted-foreground shadow-xs">
                      <Info className="mt-0.5 size-4 shrink-0 text-accent-strong" aria-hidden="true" />
                      {t.intro}
                    </p>
                    {settings.notice && (
                      <div
                        role="status"
                        data-testid="settings-notice"
                        className="flex items-start gap-2.5 rounded-xl border border-warning/30 bg-warning/10 px-4 py-2.5 text-[12px] leading-relaxed text-warning"
                      >
                        <TriangleAlert className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
                        {settings.notice}
                      </div>
                    )}
                  </div>
                  {NAV.map((group) => (
                    <div key={group.id} data-group className="grid gap-3" role="region" aria-label={group.title(t)}>
                      <div className="px-1">
                        <h2 className="text-[16px] font-semibold tracking-tight">{group.title(t)}</h2>
                        <p className="text-[12px] text-muted-foreground">{group.description(t)}</p>
                      </div>
                      {group.id === 'detail' && <DetailTab detail={form.detail} choices={choices} />}
                      {group.id === 'special' && <SpecialTab special={form.special} />}
                      {group.id === 'joints' && <JointsTab joints={form.joints} choices={choices} />}
                    </div>
                  ))}
                  <p data-empty className="flex flex-col items-center gap-2 py-12 text-[13px] text-muted-foreground">
                    <SearchX className="size-8 opacity-50" aria-hidden="true" />
                    {t.noResults(search.trim())}
                  </p>
                </div>
              </main>
            </div>
            <footer className="flex shrink-0 items-center gap-2 border-t bg-glass px-5 py-2.5 backdrop-blur">
              <Button variant="ghost" size="sm" onClick={() => void resetDefaults()} disabled={settings.busy}>
                <RotateCcw />
                {t.defaults}
              </Button>
              <div className="flex min-w-0 flex-1 flex-wrap items-center gap-1.5 text-[11.5px] font-medium" aria-live="polite">
                {changedCount > 0 && (
                  <span className="flex items-center gap-1.5 rounded-full bg-primary/10 px-2.5 py-0.5 text-accent-strong">
                    <span className="size-1.5 rounded-full bg-primary" aria-hidden="true" />
                    {t.edited(changedCount)}
                  </span>
                )}
                {errorFields.length > 0 && <span className="rounded-full bg-destructive/10 px-2.5 py-0.5 text-destructive">{t.toFix(errorFields.length)}</span>}
              </div>
              <Button variant="outline" size="sm" onClick={exit}>
                {t.exit}
              </Button>
              <Button size="sm" onClick={() => void accept()} disabled={settings.busy} data-testid="settings-accept">
                <Check />
                {t.accept}
                <Kbd className="ml-0.5 h-4 bg-white/20 px-1 text-[10px] text-primary-foreground">Enter</Kbd>
              </Button>
            </footer>
          </div>
          <AlertDialog open={confirmExit} onOpenChange={setConfirmExit}>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>{t.discardTitle}</AlertDialogTitle>
                <AlertDialogDescription>{t.discardText}</AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>{t.stay}</AlertDialogCancel>
                <AlertDialogAction className="bg-destructive text-white hover:bg-destructive/90" onClick={() => settings.close()}>
                  {t.discard}
                </AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        </SearchContext.Provider>
      </FormContext.Provider>
    </SettingsLangContext.Provider>
  );
}

/** How many values (leaves of the form, table rows counted cell by cell) differ between two forms. */
function countDifferences(a: unknown, b: unknown): number {
  if (a !== null && b !== null && typeof a === 'object' && typeof b === 'object') {
    const keys = new Set([...Object.keys(a), ...Object.keys(b)]);
    let count = 0;
    for (const key of keys) count += countDifferences((a as Record<string, unknown>)[key], (b as Record<string, unknown>)[key]);
    return count;
  }
  return a === b ? 0 : 1;
}
