import { useEffect, useState } from 'react';
import { TooltipProvider } from '@/components/ui/tooltip';
import { useToast } from '../ui/Toast';
import { ElevationCard } from './ElevationCard';
import { KataFooter, KataWarnings } from './KataFooter';
import { KataHeader } from './KataHeader';
import { KataParameters } from './KataParameters';
import { KATA_STRINGS, KataLangContext, type KataLang } from './i18n';
import type { KataBridge, KataTheme } from './kataContracts';
import { useKata } from './useKata';

/**
 * The Kata Export window over KataExportViewModel: the title and run commands, the cells B3:B10 beside the run's
 * elevation, the warnings, and the commands that read, build and write. The cells written to the sheet are not
 * repeated here: the Kata workbook shows them.
 */
export function KataApp({ bridge }: { bridge: KataBridge }) {
  const notify = useToast();
  const kata = useKata(bridge, notify);
  const [lang, setLang] = useState<KataLang>(() => (readChoice(LANG_KEY) === 'en' ? 'en' : 'vi'));
  // The page follows Revit's theme until the user picks one with the header's switch.
  const [themeChoice, setThemeChoice] = useState<KataTheme | null>(() => {
    const saved = readChoice(THEME_KEY);
    return saved === 'light' || saved === 'dark' ? saved : null;
  });
  const hostState = kata.state;
  const theme = themeChoice ?? hostState?.theme ?? 'light';
  const state = hostState && hostState.theme !== theme ? { ...hostState, theme } : hostState;

  useEffect(() => {
    document.documentElement.classList.toggle('dark', theme === 'dark');
  }, [theme]);
  useEffect(() => {
    document.documentElement.lang = lang;
  }, [lang]);

  const pickLang = (next: KataLang) => {
    setLang(next);
    writeChoice(LANG_KEY, next);
  };
  const pickTheme = (next: KataTheme) => {
    setThemeChoice(next);
    writeChoice(THEME_KEY, next);
  };

  if (!state) {
    return (
      <div className="flex h-full bg-background">
        <div className="m-auto text-[13px] text-muted-foreground">{kata.loadError ?? KATA_STRINGS[lang].loading}</div>
      </div>
    );
  }

  return (
    <KataLangContext.Provider value={lang}>
      <TooltipProvider delayDuration={300}>
        <div className="flex h-full min-w-0 flex-col bg-background text-foreground">
          <KataHeader state={state} kata={kata} mock={!!bridge.isMock} lang={lang} onLang={pickLang} theme={theme} onTheme={pickTheme} />
          <main className="grid min-h-0 flex-1 grid-cols-[minmax(0,1fr)] gap-2.5 p-3 md:grid-cols-[232px_minmax(0,1fr)]">
            <KataParameters state={state} update={kata.update} />
            <ElevationCard state={state} kata={kata} />
          </main>
          <div className="shrink-0 px-3 pb-2">
            <KataWarnings warnings={state.warnings} />
          </div>
          <KataFooter state={state} kata={kata} />
        </div>
      </TooltipProvider>
    </KataLangContext.Provider>
  );
}

const LANG_KEY = 'hp.kata.lang';
const THEME_KEY = 'hp.kata.theme';

/** A viewer's own choice kept by the page; storage can be missing or refused, then the choice is simply not kept. */
function readChoice(key: string): string | null {
  try {
    return window.localStorage.getItem(key);
  } catch {
    return null;
  }
}

function writeChoice(key: string, value: string): void {
  try {
    window.localStorage.setItem(key, value);
  } catch {
    // storage refused (private mode, policy): the choice lasts for this window only
  }
}
