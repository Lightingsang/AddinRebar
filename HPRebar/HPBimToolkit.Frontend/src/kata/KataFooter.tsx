import { CircleAlert, FileDown, FileSpreadsheet, Grid3x3 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { useT } from './i18n';
import type { KataViewState } from './kataContracts';
import type { KataController } from './useKata';

/** A filled command button in its own colour, white text, dimmed when disabled. */
const action = (colour: string) => cn('h-9 px-4 font-semibold text-white shadow-sm hover:brightness-90 disabled:opacity-45', colour);

/** The run's size, then write the sheet, read the bars back and build them in Revit — in that order, each its colour. */
export function KataFooter({ state, kata }: { state: KataViewState; kata: KataController }) {
  const t = useT();
  const busy = state.isBusy;
  return (
    <footer className="flex shrink-0 flex-wrap items-center gap-2 px-4 pt-1 pb-3">
      <span className="text-[12.5px] text-muted-foreground">
        {t.elementCount}: <b className="text-foreground">{state.header.elementCount}</b>
      </span>
      <span className="ml-4 text-[12.5px] text-muted-foreground">
        {t.totalLength}: <b className="text-foreground">{state.header.totalLength}</b>
      </span>
      <div className="ml-auto flex flex-wrap items-center gap-2" data-testid="kata-actions">
        <Button className={action('bg-[var(--action-export)]')} disabled={!state.can.export} onClick={() => kata.run('export')} title={t.exportHint}>
          <FileSpreadsheet />
          {t.exportExcel}
        </Button>
        <Button className={action('bg-[var(--action-read)]')} disabled={!state.can.loadRebar || busy} onClick={() => kata.run('loadRebar')} title={t.readRebarHint}>
          <FileDown />
          {t.readRebar}
        </Button>
        <Button className={action('bg-[var(--action-build)]')} disabled={!state.can.generateRebar} onClick={() => kata.run('generateRebar')} title={t.buildRebarHint}>
          <Grid3x3 />
          {t.buildRebar}
        </Button>
      </div>
    </footer>
  );
}

/** The run's warnings in a strip under the drawing; nothing when there are none. */
export function KataWarnings({ warnings }: { warnings: string[] }) {
  const t = useT();
  if (warnings.length === 0) return null;
  return (
    <section className="flex shrink-0 gap-3 rounded-lg border border-steel/70 bg-card px-3 py-2" aria-label={t.warnings}>
      <span className="flex shrink-0 items-start gap-1.5 pt-0.5 text-[12.5px] font-semibold text-steel">
        <CircleAlert className="mt-0.5 size-4" aria-hidden="true" />
        {t.warnings}:
      </span>
      <ul className="max-h-[4.6rem] min-w-0 flex-1 list-disc overflow-y-auto pl-4 text-[12px] leading-[1.55]" data-testid="kata-warnings">
        {warnings.map((warning) => (
          <li key={warning}>{warning}</li>
        ))}
      </ul>
    </section>
  );
}
