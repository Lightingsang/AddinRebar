import type { ReactNode } from 'react';
import { Settings2 } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { KataUpdateRequest, KataViewState } from './kataContracts';
import { useT } from './i18n';

const NONE = '';

/** A cog over an invisible native select: the beam parameter cell B3 or B4 is read from. */
function ParameterPicker({ label, parameter, parameters, onChange }: { label: string; parameter: string; parameters: string[]; onChange(parameter: string): void }) {
  const t = useT();
  return (
    <span className="relative inline-grid size-6 shrink-0 place-items-center rounded text-muted-foreground hover:bg-secondary hover:text-foreground has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-ring" title={parameter ? t.fromParameter(parameter) : t.noParameterHint}>
      <Settings2 className="size-3.5" aria-hidden="true" />
      <select aria-label={t.parameterOf(label)} className="absolute inset-0 cursor-pointer opacity-0" value={parameter} onChange={(e) => onChange(e.target.value)}>
        <option value={NONE}>{t.noParameter}</option>
        {parameters.map((name) => (
          <option key={name} value={name}>
            {name}
          </option>
        ))}
      </select>
    </span>
  );
}

function Row({ label, value, tone, picker }: { label: string; value: string; tone?: 'name' | 'warn'; picker?: ReactNode }) {
  return (
    <div className="grid min-h-11 grid-cols-[minmax(0,1fr)_minmax(0,1fr)] border-b last:border-b-0">
      <div className="flex items-center bg-secondary/60 px-2.5 text-[12.5px] font-semibold">{label}</div>
      <div className="flex min-w-0 items-center justify-center gap-1 px-1.5">
        <span
          className={cn('truncate font-mono text-[12.5px] font-bold', tone === 'name' && 'text-accent-strong', tone === 'warn' && 'text-steel')}
          title={value}
        >
          {value || '—'}
        </span>
        {picker}
      </div>
    </div>
  );
}

/** Cells B3:B10 as they will be written; B3 and B4 come from the beam parameters picked with the cogs. */
export function KataParameters({ state, update }: { state: KataViewState; update(request: KataUpdateRequest): void }) {
  const t = useT();
  const h = state.header;
  const offsetIsZero = /^0(\s|$)/.test(h.axisOffset.trim());
  return (
    <section className="flex min-h-0 flex-col overflow-hidden rounded-lg border bg-card shadow-sm" aria-label={t.parameters}>
      <header className="flex shrink-0 items-center justify-between gap-2 border-b px-3 py-2.5">
        <h2 className="text-[13.5px] font-bold">{t.parametersTitle}</h2>
        <span className="text-[11.5px] text-muted-foreground">{t.cells}</span>
      </header>
      <div className="m-2 min-h-0 overflow-auto rounded-md border">
        <Row
          label={t.beamName}
          value={h.beamName}
          tone="name"
          picker={<ParameterPicker label={t.beamName} parameter={state.nameParameter} parameters={state.parameters} onChange={(nameParameter) => update({ nameParameter })} />}
        />
        <Row
          label={t.beamCount}
          value={h.beamCount}
          picker={<ParameterPicker label={t.beamCount} parameter={state.countParameter} parameters={state.parameters} onChange={(countParameter) => update({ countParameter })} />}
        />
        <Row label={t.beamHeight} value={h.height} />
        <Row label={t.beamWidth} value={h.width} />
        <Row label={t.slabThickness} value={h.slabThickness} />
        <Row label={t.axisGrid} value={h.axisGrid} tone="name" />
        <Row label={t.axisOffset} value={h.axisOffset} tone={offsetIsZero ? undefined : 'warn'} />
        <Row label={t.beamElevation} value={h.elevation} />
      </div>
    </section>
  );
}
