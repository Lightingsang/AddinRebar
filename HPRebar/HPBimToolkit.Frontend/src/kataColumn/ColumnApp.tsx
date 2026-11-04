import { useEffect, useState } from 'react';
import { Columns3, FileSpreadsheet, Moon, RefreshCw, Sun, TriangleAlert } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { errorText } from '../bridge/errorText';
import { readChoice, writeChoice } from '../kataSettings/SettingsHeader';
import { useToast } from '../ui/Toast';
import type { ColumnBridge, KataColumnMode, KataColumnPreview, KataColumnPreviewRow, KataColumnRebarSummary, KataColumnWorkbook, KataTheme } from './columnContracts';
import { ColumnRebarView } from './ColumnRebarView';
import { ColumnSketch } from './ColumnSketch';
import { COLUMN_STRINGS, type ColumnLang } from './i18n';

const LANG_KEY = 'hp.kata.lang';
const THEME_KEY = 'hp.kata.theme';

type Strings = (typeof COLUMN_STRINGS)['vi'];

/**
 * The Kata Column Rebar window, the column counterpart of Kata Export. Step 1: the column stack measured in Revit as
 * the cells of sheet Cot, a sketch along b, the host's warnings, and the write into the Kata workbook Excel has active.
 * Step 2: the bars typed in that sheet read back as a plan on the Revit faces, and their creation in Revit.
 */
export function ColumnApp({ bridge }: { bridge: ColumnBridge }) {
  const notify = useToast();
  const [lang, setLang] = useState<ColumnLang>(() => (readChoice(LANG_KEY) === 'en' ? 'en' : 'vi'));
  const [themeChoice, setThemeChoice] = useState<KataTheme | null>(() => {
    const saved = readChoice(THEME_KEY);
    return saved === 'light' || saved === 'dark' ? saved : null;
  });
  const [hostTheme, setHostTheme] = useState<KataTheme>('light');
  const [mode, setMode] = useState<KataColumnMode>('export');
  const [preview, setPreview] = useState<KataColumnPreview | null>(null);
  const [rebar, setRebar] = useState<KataColumnRebarSummary | null>(null);
  const [workbook, setWorkbook] = useState<KataColumnWorkbook | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [name, setName] = useState('');
  const [count, setCount] = useState('1');
  const [busy, setBusy] = useState(false);
  const [rowsWithoutBars, setRowsWithoutBars] = useState<string[]>([]);
  const t = COLUMN_STRINGS[lang];
  const theme = themeChoice ?? hostTheme;

  useEffect(() => {
    let alive = true;
    bridge
      .request('column.load')
      .then((state) => {
        if (!alive) return;
        setHostTheme(state.theme);
        setMode(state.mode);
        setPreview(state.preview);
        setRebar(state.rebar);
        setWorkbook(state.workbook);
        if (state.preview) {
          setName(state.preview.name);
          setCount(String(state.preview.count));
        }
      })
      .catch((error: unknown) => alive && setLoadError(errorText(error)));
    const off = bridge.on('column.theme', (event) => setHostTheme(event.theme));
    return () => {
      alive = false;
      off();
    };
  }, [bridge]);

  useEffect(() => {
    document.documentElement.classList.toggle('dark', theme === 'dark');
  }, [theme]);
  useEffect(() => {
    document.documentElement.lang = lang;
  }, [lang]);

  if (!workbook || (mode === 'export' && !preview)) {
    return <div className="flex h-full bg-background text-[13px] text-muted-foreground"><span className="m-auto">{loadError ? t.loadFailed(loadError) : t.loading}</span></div>;
  }

  const countValue = Number(count);
  const canWrite = workbook.ready && name.trim().length > 0 && Number.isInteger(countValue) && countValue >= 1 && !busy;

  // One busy state for the whole page: the host answers one request at a time, so no action (step switch, close,
  // create) may start while another is pending — a Create would otherwise run on a plan the page has not shown yet.
  const run = async (action: () => Promise<void>, fail: (reason: string) => string = (reason) => reason) => {
    setBusy(true);
    try {
      await action();
    } catch (error) {
      notify(fail(errorText(error)), 'error');
    } finally {
      setBusy(false);
    }
  };

  const probe = () => run(async () => setWorkbook(await bridge.request('column.probe')));

  const write = () =>
    run(async () => {
      const result = await bridge.request('column.write', { name: name.trim(), count: countValue });
      setWorkbook(result.workbook);
      setRowsWithoutBars(result.rowsWithoutBars);
      notify(result.success ? t.written(result.storeysWritten, result.workbook.name) : t.writeFailed(result.error), result.success ? 'success' : 'error');
    }, t.writeFailed);

  // Step 2 reads sheet Cot again: the user typed the bars in Excel since the page opened.
  const openStep = async (step: KataColumnMode) => {
    setMode(step);
    if (step !== 'rebar') return;
    await run(async () => {
      const state = await bridge.request('column.reload');
      setRebar(state.rebar);
      setWorkbook(state.workbook);
    });
  };

  const pickTheme = (next: KataTheme) => {
    setThemeChoice(next);
    writeChoice(THEME_KEY, next);
  };
  const pickLang = (next: ColumnLang) => {
    setLang(next);
    writeChoice(LANG_KEY, next);
  };

  const header = (
    <header className="flex shrink-0 items-center gap-3 border-b bg-card px-5 py-3">
      <span className="grid size-9 shrink-0 place-items-center rounded-xl bg-primary text-primary-foreground shadow-sm">
        <Columns3 className="size-[18px]" aria-hidden="true" />
      </span>
      <div className="min-w-0">
        <div className="flex items-center gap-2">
          <h1 className="truncate text-[15px] leading-tight font-semibold">{t.title}</h1>
          {bridge.isMock && <Badge variant="secondary">{t.sampleData}</Badge>}
        </div>
        <p className="truncate text-[12px] text-muted-foreground">{t.subtitle}</p>
      </div>
      <div className="ml-auto flex items-center gap-2">
        <div role="group" aria-label={t.language} className="flex items-center rounded-lg bg-secondary p-0.5 ring-1 ring-border ring-inset">
          {(['vi', 'en'] as const).map((code) => (
            <button
              key={code}
              type="button"
              aria-pressed={lang === code}
              className={cn('h-7 rounded-md px-2.5 text-[11.5px] font-semibold', lang === code ? 'bg-card text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground')}
              onClick={() => pickLang(code)}
            >
              {code.toUpperCase()}
            </button>
          ))}
        </div>
        <Button variant="outline" size="icon-sm" className="rounded-lg" aria-label={theme === 'dark' ? t.toLight : t.toDark} onClick={() => pickTheme(theme === 'dark' ? 'light' : 'dark')}>
          {theme === 'dark' ? <Sun className="text-steel" /> : <Moon className="text-accent-strong" />}
        </Button>
      </div>
    </header>
  );

  const steps = (
    <nav role="tablist" aria-label={t.steps} className="flex shrink-0 gap-1 border-b bg-card px-5 pb-2">
      {(['export', 'rebar'] as const).map((step) => (
        <button
          key={step}
          type="button"
          role="tab"
          aria-selected={mode === step}
          data-testid={`step-${step}`}
          disabled={busy}
          className={cn('h-8 rounded-lg px-3 text-[12.5px] font-medium', mode === step ? 'bg-primary text-primary-foreground shadow-sm' : 'text-muted-foreground hover:bg-secondary hover:text-foreground')}
          onClick={() => void openStep(step)}
        >
          {step === 'export' ? t.stepExport : t.stepRebar}
        </button>
      ))}
    </nav>
  );

  if (mode === 'rebar') {
    return (
      <div className="flex h-full flex-col bg-background text-foreground">
        {header}
        {steps}
        <ColumnRebarView bridge={bridge} summary={rebar} workbook={workbook} t={t} theme={theme} busy={busy} run={run} onState={(state) => {
          setRebar(state.rebar);
          setWorkbook(state.workbook);
        }} />
      </div>
    );
  }

  return (
    <div className="flex h-full flex-col bg-background text-foreground">
      {header}
      {steps}

      <main className="grid min-h-0 flex-1 grid-cols-[minmax(0,1fr)_300px] gap-4 overflow-auto p-5">
        <div className="grid content-start gap-4">
          <StoreyTable rows={preview!.rows} t={t} />
          <HeaderCells preview={preview!} t={t} />
          {rowsWithoutBars.length > 0 && (
            <p role="status" data-testid="column-no-bars" className="flex gap-2 rounded-xl border border-destructive/30 bg-destructive/10 px-4 py-2.5 text-[12px] text-destructive">
              <TriangleAlert className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
              {t.noBars(rowsWithoutBars.join(', '))}
            </p>
          )}
          {preview!.warnings.length > 0 && (
            <section aria-label={t.warnings} data-testid="column-warnings" className="grid gap-1.5 rounded-xl border border-warning/30 bg-warning/10 px-4 py-3 text-[12px] text-warning">
              <h2 className="flex items-center gap-2 font-semibold">
                <TriangleAlert className="size-4" aria-hidden="true" />
                {t.warnings}
              </h2>
              <ul className="grid list-disc gap-1 pl-6">
                {preview!.warnings.map((w) => (
                  <li key={w}>{w}</li>
                ))}
              </ul>
            </section>
          )}
        </div>
        <section className="rounded-xl border bg-card p-3" aria-label={t.sketch}>
          <h2 className="mb-2 text-[12px] font-semibold text-muted-foreground">{t.sketch}</h2>
          <ColumnSketch rows={preview!.rows} title={t.sketch} />
        </section>
      </main>

      <footer className="flex shrink-0 flex-wrap items-center gap-3 border-t bg-glass px-5 py-2.5">
        <label className="flex items-center gap-2 text-[12px]">
          {t.name}
          <input className="h-8 w-28 rounded-lg border border-input bg-background px-2 text-[12.5px]" value={name} onChange={(e) => setName(e.target.value)} />
        </label>
        <label className="flex items-center gap-2 text-[12px]">
          {t.count}
          <input className="h-8 w-16 rounded-lg border border-input bg-background px-2 text-[12.5px]" inputMode="numeric" value={count} onChange={(e) => setCount(e.target.value)} />
        </label>
        <span data-testid="column-workbook" className={cn('flex min-w-0 flex-1 items-center gap-1.5 truncate text-[12px]', workbook.ready ? 'text-muted-foreground' : 'text-destructive')}>
          <FileSpreadsheet className="size-4 shrink-0" aria-hidden="true" />
          {workbook.ready ? t.workbookReady(workbook.name) : workbook.error}
        </span>
        <Button variant="ghost" size="sm" onClick={() => void probe()} disabled={busy}>
          <RefreshCw />
          {t.probe}
        </Button>
        <Button variant="outline" size="sm" onClick={() => void bridge.request('column.close')} disabled={busy}>
          {t.close}
        </Button>
        <Button size="sm" onClick={() => void write()} disabled={!canWrite} data-testid="column-write">
          {t.write}
        </Button>
      </footer>
    </div>
  );
}

function StoreyTable({ rows, t }: { rows: KataColumnPreviewRow[]; t: Strings }) {
  const placement = (row: KataColumnPreviewRow) =>
    row.placement === 'first'
      ? t.placementFirst
      : row.placement === 'leftFace'
        ? t.placementLeftFace
        : row.placement === 'stub'
          ? t.placementStub
          : t.placementCentred;
  return (
    <section className="overflow-hidden rounded-xl border bg-card" aria-label={t.storeys}>
      <h2 className="border-b px-4 py-2 text-[12px] font-semibold text-muted-foreground">{t.storeys}</h2>
      <table className="w-full text-[12.5px] tabular-nums">
        <thead className="text-left text-[11.5px] text-muted-foreground">
          <tr>
            <th className="px-4 py-1.5 font-medium">{t.level}</th>
            <th className="px-2 py-1.5 text-right font-medium">{t.height}</th>
            <th className="px-2 py-1.5 text-right font-medium">{t.sizeB}</th>
            <th className="px-2 py-1.5 text-right font-medium">{t.sizeH}</th>
            <th className="px-2 py-1.5 text-right font-medium">{t.beamDepth}</th>
            <th className="px-4 py-1.5 font-medium">{t.placement}</th>
          </tr>
        </thead>
        <tbody>
          {[...rows].reverse().map((row, i) => (
            <tr key={`${i}-${row.level}`} data-row={row.level} className="border-t">
              <td className="px-4 py-1.5 font-medium">{row.level}</td>
              <td className="px-2 py-1.5 text-right">{row.heightM.toFixed(3)}</td>
              <td className="px-2 py-1.5 text-right">{row.sizeB}</td>
              <td className="px-2 py-1.5 text-right">{row.shape === 'circle' ? `0 (${t.circle})` : row.sizeH}</td>
              <td className="px-2 py-1.5 text-right">{row.beamDepthMm}</td>
              <td className={cn('px-4 py-1.5', row.placement === 'leftFace' && 'text-steel', row.placement === 'stub' && 'text-muted-foreground')}>{placement(row)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}

function HeaderCells({ preview, t }: { preview: KataColumnPreview; t: Strings }) {
  const cells: [string, number | null][] = [
    [t.crossingBeam, preview.crossingBeamWidthMm],
    [t.slab, preview.slabThicknessMm],
    [t.capBottom, preview.pileCapBottomM],
    [t.capHeight, preview.pileCapHeightM],
  ];
  return (
    <section className="grid grid-cols-4 gap-2 rounded-xl border bg-card px-4 py-3 text-[12px]" aria-label={t.header}>
      {cells.map(([label, value]) => (
        <div key={label} className="grid gap-0.5">
          <span className="text-muted-foreground">{label}</span>
          <span className="font-medium tabular-nums">{value ?? t.keep}</span>
        </div>
      ))}
    </section>
  );
}
