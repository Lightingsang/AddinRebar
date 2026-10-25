import { FileSpreadsheet, Languages, Moon, MousePointerClick, RefreshCw, Sun, Table2 } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { useT, type KataLang } from './i18n';
import type { KataTheme, KataViewState } from './kataContracts';
import type { KataController } from './useKata';

interface KataHeaderProps {
  state: KataViewState;
  kata: KataController;
  mock: boolean;
  lang: KataLang;
  onLang(lang: KataLang): void;
  theme: KataTheme;
  onTheme(theme: KataTheme): void;
}

/** The window's title with the language and light/dark switches, then the run commands and the workbook's state. */
export function KataHeader({ state, kata, mock, lang, onLang, theme, onTheme }: KataHeaderProps) {
  const t = useT();
  const dark = theme === 'dark';
  return (
    <>
      <header className="flex shrink-0 items-center gap-2.5 px-4 pt-3 pb-2">
        <Table2 className="size-5 text-primary" aria-hidden="true" />
        <h1 className="text-[15px] font-bold tracking-wide">{t.title}</h1>
        {mock && <Badge variant="secondary">{t.sampleData}</Badge>}
        <div className="ml-auto flex items-center gap-1.5">
          <div role="group" aria-label={t.language} className="flex items-center rounded-md border bg-card p-0.5 shadow-sm">
            <Languages className="mx-1 size-3.5 text-muted-foreground" aria-hidden="true" />
            {(['vi', 'en'] as const).map((code) => (
              <button
                key={code}
                type="button"
                aria-pressed={lang === code}
                className={cn(
                  'rounded px-2 py-0.5 text-[11.5px] font-bold transition-colors',
                  lang === code ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-secondary hover:text-foreground',
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
            className="bg-card shadow-sm"
            aria-label={dark ? t.toLight : t.toDark}
            title={dark ? t.toLight : t.toDark}
            data-testid="kata-theme-toggle"
            onClick={() => onTheme(dark ? 'light' : 'dark')}
          >
            {dark ? <Sun className="text-steel" /> : <Moon className="text-accent-strong" />}
          </Button>
        </div>
      </header>
      <div className="mx-3 flex shrink-0 flex-wrap items-center gap-1 rounded-lg border bg-card px-2 py-1.5 shadow-sm">
        <Button variant="ghost" size="sm" disabled={!state.can.repick} onClick={() => kata.run('repick')}>
          <MousePointerClick />
          {t.repick}
        </Button>
        <Button variant="ghost" size="sm" onClick={() => kata.run('refreshExcel')} title={t.checkExcelHint}>
          <RefreshCw />
          {t.checkExcel}
        </Button>
        <div className="ml-auto flex min-w-0 items-center gap-2.5 pr-1">
          <FileSpreadsheet className="size-6 shrink-0 text-success" aria-hidden="true" />
          <div className="min-w-0 text-right leading-tight">
            <div className="truncate text-[12.5px] font-semibold" data-testid="kata-workbook" title={state.workbook}>
              Excel: {state.workbook}
            </div>
            <div role="status" className={cn('truncate text-[12px]', state.hasError ? 'text-danger' : 'text-success')} title={state.status}>
              {state.status}
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
