import { Moon, Search, SlidersHorizontal, Sun, X } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { useT, type SettingsLang } from './i18n';
import type { KataTheme } from './settingsContracts';

interface SettingsHeaderProps {
  mock: boolean;
  search: string;
  onSearch(text: string): void;
  lang: SettingsLang;
  onLang(lang: SettingsLang): void;
  theme: KataTheme;
  onTheme(theme: KataTheme): void;
}

/** The window's title, the search box and the language and light/dark switches (shared with the Kata Export page). */
export function SettingsHeader({ mock, search, onSearch, lang, onLang, theme, onTheme }: SettingsHeaderProps) {
  const t = useT();
  const dark = theme === 'dark';
  return (
    <header className="flex shrink-0 flex-wrap items-center gap-x-3 gap-y-2 border-b bg-card px-5 py-3">
      <span className="grid size-9 shrink-0 place-items-center rounded-xl bg-primary text-primary-foreground shadow-sm">
        <SlidersHorizontal className="size-[18px]" aria-hidden="true" />
      </span>
      <div className="min-w-0">
        <div className="flex items-center gap-2">
          <h1 className="truncate text-[15px] leading-tight font-semibold">{t.title}</h1>
          {mock && <Badge variant="secondary">{t.sampleData}</Badge>}
        </div>
        <p className="truncate text-[12px] text-muted-foreground">{t.subtitle}</p>
      </div>
      <div className="ml-auto flex items-center gap-2">
        <div className="relative">
          <Search className="pointer-events-none absolute top-1/2 left-2.5 size-3.5 -translate-y-1/2 text-muted-foreground" aria-hidden="true" />
          <input
            type="search"
            aria-label={t.searchLabel}
            placeholder={t.search}
            value={search}
            onChange={(e) => onSearch(e.target.value)}
            onKeyDown={(e) => {
              // Escape clears the box first; with nothing in it, it leaves the window like anywhere else.
              if (e.key === 'Escape' && search) {
                e.preventDefault();
                onSearch('');
              }
            }}
            className="h-8 w-44 rounded-lg border border-input bg-background/70 pr-7 pl-8 text-[12.5px] shadow-xs outline-none transition-[width,box-shadow] focus-visible:outline-none placeholder:text-muted-foreground focus:w-56 focus:border-ring focus:ring-[3px] focus:ring-ring/35 [&::-webkit-search-cancel-button]:hidden"
          />
          {search && (
            <button type="button" aria-label={t.clearSearch} className="absolute top-1/2 right-1.5 grid size-5 -translate-y-1/2 place-items-center rounded text-muted-foreground hover:text-foreground" onClick={() => onSearch('')}>
              <X className="size-3.5" />
            </button>
          )}
        </div>
        <div role="group" aria-label={t.language} className="flex items-center rounded-lg bg-secondary p-0.5 ring-1 ring-border ring-inset">
          {(['vi', 'en'] as const).map((code) => (
            <button
              key={code}
              type="button"
              aria-pressed={lang === code}
              className={cn(
                'h-7 rounded-md px-2.5 text-[11.5px] font-semibold transition-colors',
                lang === code ? 'bg-card text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground',
              )}
              onClick={() => onLang(code)}
            >
              {code.toUpperCase()}
            </button>
          ))}
        </div>
        <Button
          variant="outline"
          size="icon-sm"
          className="rounded-lg bg-background/70"
          aria-label={dark ? t.toLight : t.toDark}
          title={dark ? t.toLight : t.toDark}
          data-testid="settings-theme-toggle"
          onClick={() => onTheme(dark ? 'light' : 'dark')}
        >
          {dark ? <Sun className="text-steel" /> : <Moon className="text-accent-strong" />}
        </Button>
      </div>
    </header>
  );
}

/** A viewer's own choice kept by the page; storage can be missing or refused, then the choice is simply not kept. */
export function readChoice(key: string): string | null {
  try {
    return window.localStorage.getItem(key);
  } catch {
    return null;
  }
}

export function writeChoice(key: string, value: string): void {
  try {
    window.localStorage.setItem(key, value);
  } catch {
    // storage refused (private mode, policy): the choice lasts for this window only
  }
}
