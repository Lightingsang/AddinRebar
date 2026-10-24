import { formatNumber } from './format';

interface StatTileProps {
  label: string;
  value: number | null;
  hint?: string;
  tone?: 'default' | 'warning';
}

export function StatTile({ label, value, hint, tone = 'default' }: StatTileProps) {
  return (
    <div className="min-w-0 rounded-md border border-line bg-panel-muted px-2.5 py-2">
      <div className="truncate text-[11px] text-muted">{label}</div>
      <div className={`text-lg font-semibold tabular-nums ${tone === 'warning' && value ? 'text-steel' : 'text-ink'}`}>
        {value === null ? '—' : formatNumber(value)}
      </div>
      {hint && <div className="truncate text-[11px] text-muted">{hint}</div>}
    </div>
  );
}
