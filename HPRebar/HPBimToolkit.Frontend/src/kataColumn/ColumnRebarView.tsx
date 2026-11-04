import { useState } from 'react';
import { FileSpreadsheet, Info, RefreshCw, TriangleAlert } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { useToast } from '../ui/Toast';
import { ColumnDrawing } from './drawing/ColumnDrawing';
import type { ColumnBridge, KataColumnPageState, KataColumnRebarSummary, KataColumnWorkbook, KataTheme } from './columnContracts';
import type { COLUMN_STRINGS } from './i18n';

type Strings = (typeof COLUMN_STRINGS)['vi'];

interface ColumnRebarViewProps {
  bridge: ColumnBridge;
  summary: KataColumnRebarSummary | null;
  workbook: KataColumnWorkbook;
  t: Strings;
  theme: KataTheme;
  /** A request of the page is pending: every action waits for it (the host answers one at a time). */
  busy: boolean;
  /** Runs one request with the page's busy state and error toast. */
  run(action: () => Promise<void>): Promise<void>;
  onState(state: KataColumnPageState): void;
}

/**
 * Step 2 of Kata Column Rebar: the plan the host built from sheet Cot on the measured columns (bars, hoop zones, ties and
 * numbers per storey), what it could not decide, and one action: create the rebar in Revit (one Undo). After every
 * creation the sheet is read again, so the page always shows what the next click would create, and the run's warnings
 * stay on the page.
 */
export function ColumnRebarView({ bridge, summary, workbook, t, theme, busy, run, onState }: ColumnRebarViewProps) {
  const notify = useToast();
  const [warnings, setWarnings] = useState<string[]>([]);
  const errors = summary?.errors ?? [];
  const canCreate = !!summary && errors.length === 0 && summary.rows.length > 0 && !busy;

  const reload = () => run(async () => onState(await bridge.request('column.reload')));
  const generate = () =>
    run(async () => {
      const result = await bridge.request('column.generate', undefined, { timeoutMs: 330_000 });
      setWarnings(result.warnings);
      notify(result.success ? t.generated(result.created, result.deleted) : t.generateFailed(result.error), result.success ? 'success' : 'error');
      onState(await bridge.request('column.reload'));
    });

  return (
    <>
      <main className="grid min-h-0 flex-1 content-start gap-4 overflow-auto p-5">
        {errors.length > 0 && (
          <Messages testId="rebar-errors" tone="error" title={t.rebarErrors} items={errors} />
        )}
        {warnings.length > 0 && <Messages testId="rebar-warnings" tone="note" title={t.rebarWarnings} items={warnings} />}
        {summary && summary.rows.length > 0 && (
          <section className="overflow-hidden rounded-xl border bg-card" aria-label={t.rebarStoreys}>
            <h2 className="flex items-center justify-between border-b px-4 py-2 text-[12px] font-semibold text-muted-foreground">
              <span>{t.rebarStoreys} — {summary.name}</span>
              <span className="font-normal">{t.rebarTotals(summary.starterCount, summary.barCount)}</span>
            </h2>
            <table className="w-full text-[12.5px] tabular-nums">
              <thead className="text-left text-[11.5px] text-muted-foreground">
                <tr>
                  <th className="px-4 py-1.5 font-medium">{t.level}</th>
                  <th className="px-2 py-1.5 font-medium">{t.rebarBars}</th>
                  <th className="px-2 py-1.5 font-medium">{t.rebarHoops}</th>
                  <th className="px-2 py-1.5 text-right font-medium">{t.rebarTies}</th>
                  <th className="px-4 py-1.5 font-medium">{t.rebarNumbers}</th>
                </tr>
              </thead>
              <tbody>
                {[...summary.rows].reverse().map((row, i) => (
                  <tr key={`${i}-${row.level}`} data-row={row.level} className="border-t">
                    <td className="px-4 py-1.5 font-medium">{row.level}</td>
                    <td className="px-2 py-1.5">{row.mainBars}</td>
                    <td className="px-2 py-1.5">{row.hoops}</td>
                    <td className="px-2 py-1.5 text-right">{row.tieCount}</td>
                    <td className="px-4 py-1.5 text-muted-foreground">{row.numbers}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </section>
        )}
        {summary?.elevation && errors.length === 0 && <ColumnDrawing elevation={summary.elevation} sections={summary.sections} theme={theme} t={t} />}
        {summary && summary.notes.length > 0 && <Messages testId="rebar-notes" tone="note" title={t.rebarNotes} items={summary.notes} />}
      </main>
      <footer className="flex shrink-0 flex-wrap items-center gap-3 border-t bg-glass px-5 py-2.5">
        <span data-testid="column-workbook" className={cn('flex min-w-0 flex-1 items-center gap-1.5 truncate text-[12px]', workbook.ready ? 'text-muted-foreground' : 'text-destructive')}>
          <FileSpreadsheet className="size-4 shrink-0" aria-hidden="true" />
          {workbook.ready ? workbook.name : workbook.error}
        </span>
        <Button variant="ghost" size="sm" onClick={() => void reload()} disabled={busy}>
          <RefreshCw />
          {t.reload}
        </Button>
        <Button variant="outline" size="sm" onClick={() => void bridge.request('column.close')} disabled={busy}>
          {t.close}
        </Button>
        <Button size="sm" onClick={() => void generate()} disabled={!canCreate} data-testid="rebar-generate">
          {t.generate}
        </Button>
      </footer>
    </>
  );
}

function Messages({ testId, tone, title, items }: { testId: string; tone: 'error' | 'note'; title: string; items: string[] }) {
  const Icon = tone === 'error' ? TriangleAlert : Info;
  return (
    <section
      aria-label={title}
      data-testid={testId}
      className={cn(
        'grid gap-1.5 rounded-xl border px-4 py-3 text-[12px]',
        tone === 'error' ? 'border-destructive/30 bg-destructive/10 text-destructive' : 'border-warning/30 bg-warning/10 text-warning',
      )}
    >
      <h2 className="flex items-center gap-2 font-semibold">
        <Icon className="size-4" aria-hidden="true" />
        {title}
      </h2>
      <ul className="grid list-disc gap-1 pl-6">
        {items.map((item, i) => (
          <li key={`${i}-${item}`}>{item}</li>
        ))}
      </ul>
    </section>
  );
}
