import { useState, type KeyboardEvent } from 'react';
import { LAYERS } from './drawingTools';
import type { Palette } from './palette';
import type { LayerId } from './shapes';

export type ExportKind = 'svg' | 'png2' | 'png4' | 'print';

interface ElevationToolsProps {
  palette: Palette;
  /** Kata's drawing is shown: the layers, finding a bar and snapping apply to it. */
  kata: boolean;
  measuring: boolean;
  onMeasure(on: boolean): void;
  hidden: ReadonlySet<LayerId>;
  onToggleLayer(layer: LayerId): void;
  /** Finds bar `number` (the next occurrence when it is the same number again); returns how many tags carry it. */
  onFind(number: number): { index: number; count: number };
  onExport(kind: ExportKind): void;
}

/** The tool strip over the drawing (web only): measure, layers, find a bar, save or print the drawing. */
export function ElevationTools({ palette, kata, measuring, onMeasure, hidden, onToggleLayer, onFind, onExport }: ElevationToolsProps) {
  const [open, setOpen] = useState<'layers' | 'export' | null>(null);
  const [query, setQuery] = useState('');
  const [found, setFound] = useState<string | null>(null);
  const button = (active: boolean) =>
    `rounded border px-1.5 py-0.5 text-[11px] leading-4 ${active ? 'font-semibold' : ''}`;
  const style = (active: boolean) => ({ background: active ? `${palette.accent}33` : palette.fill, color: palette.text, borderColor: active ? palette.accent : palette.kata.grey });

  const find = () => {
    const number = Number(query);
    if (!Number.isInteger(number) || number <= 0) {
      setFound('Nhập số hiệu thanh');
      return;
    }
    const { index, count } = onFind(number);
    setFound(count === 0 ? `Không có thanh ${number}` : `${index + 1}/${count}`);
  };
  const onKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    e.stopPropagation(); // the drawing's own keys (Delete, Esc) are not the input's
    if (e.key === 'Enter') find();
    if (e.key === 'Escape') setQuery('');
  };

  return (
    <div
      className="absolute left-2 top-2 z-20 flex flex-wrap items-center gap-1"
      data-testid="kata-tools"
      onPointerDown={(e) => e.stopPropagation()}
      onPointerUp={(e) => e.stopPropagation()}
    >
      <button type="button" className={button(measuring)} style={style(measuring)} aria-pressed={measuring} title="Đo khoảng cách: bấm hai điểm (bắt điểm vào nét thép, kích thước)" onClick={() => onMeasure(!measuring)}>
        Đo
      </button>
      {kata && (
        <div className="relative">
          <button type="button" className={button(open === 'layers')} style={style(open === 'layers' || hidden.size > 0)} onClick={() => setOpen(open === 'layers' ? null : 'layers')}>
            Lớp{hidden.size > 0 ? ` (${LAYERS.length - hidden.size}/${LAYERS.length})` : ''}
          </button>
          {open === 'layers' && (
            <div className="absolute left-0 top-6 w-48 rounded border p-1.5 shadow" style={{ background: palette.fill, borderColor: palette.kata.grey, color: palette.text }} data-testid="kata-layers">
              {LAYERS.map((layer) => (
                <label key={layer.id} className="flex cursor-pointer items-center gap-1.5 py-0.5 text-[11px]">
                  <input type="checkbox" checked={!hidden.has(layer.id)} onChange={() => onToggleLayer(layer.id)} />
                  {layer.label}
                </label>
              ))}
            </div>
          )}
        </div>
      )}
      {kata && (
        <span className="flex items-center gap-1">
          <input
            aria-label="Tìm số hiệu thanh"
            inputMode="numeric"
            placeholder="Tìm số hiệu"
            className="w-20 rounded border px-1 py-0.5 text-[11px]"
            style={{ background: palette.fill, color: palette.text, borderColor: palette.kata.grey }}
            value={query}
            onChange={(e) => {
              setQuery(e.target.value.replace(/\D/g, ''));
              setFound(null);
            }}
            onKeyDown={onKeyDown}
          />
          {found && (
            <span className="text-[11px]" style={{ color: palette.mutedText }} data-testid="kata-find-result">
              {found}
            </span>
          )}
        </span>
      )}
      <div className="relative">
        <button type="button" className={button(open === 'export')} style={style(open === 'export')} onClick={() => setOpen(open === 'export' ? null : 'export')}>
          Xuất hình
        </button>
        {open === 'export' && (
          <div className="absolute left-0 top-6 flex w-36 flex-col rounded border p-1 shadow" style={{ background: palette.fill, borderColor: palette.kata.grey }}>
            {(
              [
                ['svg', 'SVG (vector)'],
                ['png2', 'PNG ×2'],
                ['png4', 'PNG ×4'],
                ['print', 'In / PDF…'],
              ] as const
            ).map(([kind, label]) => (
              <button
                key={kind}
                type="button"
                className="rounded px-1.5 py-0.5 text-left text-[11px] hover:opacity-80"
                style={{ color: palette.text }}
                onClick={() => {
                  setOpen(null);
                  onExport(kind);
                }}
              >
                {label}
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
