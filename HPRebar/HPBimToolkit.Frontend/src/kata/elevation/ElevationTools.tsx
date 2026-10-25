import { useState, type KeyboardEvent } from 'react';
import { Icon } from '../../ui/Icon';
import { LAYERS } from './drawingTools';
import type { LayerId } from './shapes';
import { useT } from '../i18n';

interface ElevationToolsProps {
  /** Kata's drawing is shown: the layers, finding a bar and snapping apply to it. */
  kata: boolean;
  measuring: boolean;
  onMeasure(on: boolean): void;
  hidden: ReadonlySet<LayerId>;
  onToggleLayer(layer: LayerId): void;
  /** Finds bar `number` (the next occurrence when it is the same number again); returns how many tags carry it. */
  onFind(number: number): { index: number; count: number };
}

/** The tool strip over the drawing (web only): measure, layers, find a bar. */
export function ElevationTools({ kata, measuring, onMeasure, hidden, onToggleLayer, onFind }: ElevationToolsProps) {
  const t = useT();
  const [open, setOpen] = useState<'layers' | null>(null);
  const [query, setQuery] = useState('');
  // what the last search found, kept as data so it reads in the language shown now
  const [found, setFound] = useState<{ number: number; index: number; count: number } | 'empty' | null>(null);
  const button = (active: boolean) =>
    `inline-flex items-center gap-1 rounded-md px-2 py-1 text-[11.5px] font-medium transition-colors ${active ? 'bg-accent-soft text-accent-strong' : 'text-ink hover:bg-panel-muted'}`;
  const menu = 'absolute left-0 top-8 z-30 rounded-lg border border-line bg-panel p-1 text-ink shadow-lg';

  const find = () => {
    const number = Number(query);
    if (!Number.isInteger(number) || number <= 0) {
      setFound('empty');
      return;
    }
    const { index, count } = onFind(number);
    setFound({ number, index, count });
  };
  const onKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    e.stopPropagation(); // the drawing's own keys (Delete, Esc) are not the input's
    if (e.key === 'Enter') find();
    if (e.key === 'Escape') setQuery('');
  };

  return (
    <div
      className="absolute left-2 top-2 z-20 flex flex-wrap items-center gap-0.5 rounded-lg border border-line bg-panel/95 p-0.5 shadow-sm backdrop-blur"
      data-testid="kata-tools"
      onPointerDown={(e) => e.stopPropagation()}
      onPointerUp={(e) => e.stopPropagation()}
    >
      <button type="button" className={button(measuring)} aria-pressed={measuring} title={t.measureHint} onClick={() => onMeasure(!measuring)}>
        <Icon name="tool" className="h-3.5 w-3.5" />
        {t.measure}
      </button>
      {kata && (
        <div className="relative">
          <button type="button" className={button(open === 'layers' || hidden.size > 0)} onClick={() => setOpen(open === 'layers' ? null : 'layers')}>
            <Icon name="layers" className="h-3.5 w-3.5" />
            {t.layers}{hidden.size > 0 ? ` (${LAYERS.length - hidden.size}/${LAYERS.length})` : ''}
          </button>
          {open === 'layers' && (
            <div className={`${menu} w-52 p-1.5`} data-testid="kata-layers">
              {LAYERS.map((layer) => (
                <label key={layer.id} className="flex cursor-pointer items-center gap-2 rounded-md px-1.5 py-1 text-[12px] hover:bg-panel-muted">
                  <input type="checkbox" checked={!hidden.has(layer.id)} onChange={() => onToggleLayer(layer.id)} />
                  {t.layerNames[layer.id] ?? layer.label}
                </label>
              ))}
            </div>
          )}
        </div>
      )}
      {kata && (
        <span className="flex items-center gap-1 pl-1">
          <Icon name="search" className="h-3.5 w-3.5 text-muted" />
          <input
            aria-label={t.findBar}
            inputMode="numeric"
            placeholder={t.findPlaceholder}
            className="w-24 rounded-md border border-line bg-panel-muted px-2 py-0.5 text-[11.5px] text-ink"

            value={query}
            onChange={(e) => {
              setQuery(e.target.value.replace(/\D/g, ''));
              setFound(null);
            }}
            onKeyDown={onKeyDown}
          />
          {found && (
            <span className="pr-1 text-[11px] text-muted" data-testid="kata-find-result">
              {found === 'empty' ? t.enterBarNumber : found.count === 0 ? t.noBar(found.number) : `${found.index + 1}/${found.count}`}
            </span>
          )}
        </span>
      )}
    </div>
  );
}
