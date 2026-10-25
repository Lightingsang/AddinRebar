import { useEffect, useMemo, useRef, useState, type PointerEvent } from 'react';
import { wheelNotches } from '../elevationGestures';
import type { KataTheme } from '../kataContracts';
import type { KataWebSection } from '../kataScene';
import { WHEEL_STEP } from './framing';
import { paletteFor } from './palette';
import { FITTED, paintSection, zoomSection, type SectionView } from './sectionPainter';
import { ShapesSvg } from './ShapesSvg';

export type SectionState = { status: 'loading' } | { status: 'ready'; section: KataWebSection } | { status: 'failed'; message: string };

interface SectionPanelProps {
  number: number;
  state: SectionState;
  theme: KataTheme;
  rect: { left: number; top: number; width: number; height: number };
  /** Shown over the card beside the plain elevation ("MẶT CẮT n-n"); the Kata panel carries Kata's own title. */
  caption?: string;
  onClose?(): void;
}

/**
 * One of Kata's sections in a panel over the elevation, zoomed and panned on its own: wheel zooms around the cursor,
 * middle button or Shift + left drag pans, middle double click fits it again. Its events never reach the elevation.
 */
export function SectionPanel({ number, state, theme, rect, caption, onClose }: SectionPanelProps) {
  const box = useRef<HTMLDivElement>(null);
  const [view, setView] = useState<{ number: number; view: SectionView }>({ number, view: FITTED });
  const current = view.number === number ? view.view : FITTED;
  const drag = useRef<{ x: number; y: number } | null>(null);
  const palette = paletteFor(theme);
  const captionHeight = caption ? 22 : 0;
  const width = rect.width;
  const height = rect.height - captionHeight;

  const shapes = useMemo(
    () => (state.status === 'ready' ? paintSection(state.section, palette, width, height, current) : []),
    [state, palette, width, height, current],
  );

  useEffect(() => {
    const element = box.current;
    if (!element) return;
    const onWheel = (e: WheelEvent) => {
      e.preventDefault();
      e.stopPropagation();
      const r = element.getBoundingClientRect();
      setView((v) => ({
        number,
        view: zoomSection(v.number === number ? v.view : FITTED, WHEEL_STEP ** wheelNotches(e.deltaY, e.deltaMode), e.clientX - r.left, e.clientY - r.top, width, height),
      }));
    };
    element.addEventListener('wheel', onWheel, { passive: false });
    return () => element.removeEventListener('wheel', onWheel);
  }, [number, width, height]);

  const stop = (e: { stopPropagation(): void }) => e.stopPropagation();
  const onPointerDown = (e: PointerEvent<HTMLDivElement>) => {
    e.stopPropagation();
    if (e.button === 1 || (e.button === 0 && e.shiftKey)) {
      drag.current = { x: e.clientX, y: e.clientY };
      e.currentTarget.setPointerCapture?.(e.pointerId);
      e.preventDefault();
    }
  };
  const onPointerMove = (e: PointerEvent<HTMLDivElement>) => {
    const start = drag.current;
    if (!start) return;
    drag.current = { x: e.clientX, y: e.clientY };
    setView((v) => {
      const base = v.number === number ? v.view : FITTED;
      return { number, view: { ...base, panX: base.panX + e.clientX - start.x, panY: base.panY + e.clientY - start.y } };
    });
  };

  return (
    <div
      className="absolute overflow-hidden border"
      style={{ left: rect.left, top: rect.top, width: rect.width, height: rect.height, background: palette.fill, borderColor: palette.kata.grey }}
      data-testid="kata-section-panel"
      data-section={number}
      onPointerDown={onPointerDown}
      onPointerMove={onPointerMove}
      onPointerUp={(e) => {
        stop(e);
        drag.current = null;
      }}
      onClick={stop}
      onAuxClick={(e) => {
        stop(e);
        if (e.button === 1 && e.detail === 2) setView({ number, view: FITTED });
      }}
    >
      {caption && (
        <div className="truncate px-2 text-center text-[9.5px] font-semibold leading-[22px]" style={{ color: palette.accent }}>
          {caption}
        </div>
      )}
      <div ref={box} className="relative" style={{ width, height }}>
        {state.status === 'ready' && (
          <svg className="pointer-events-none absolute left-0 top-0" width={width} height={height}>
            <ShapesSvg shapes={shapes} />
          </svg>
        )}
        {state.status !== 'ready' && (
          <div className="p-3 text-[11px]" style={{ color: palette.mutedText }}>
            {state.status === 'loading' ? `Đang vẽ mặt cắt ${number}-${number}…` : `Không vẽ được mặt cắt ${number}-${number}: ${state.message}`}
          </div>
        )}
      </div>
      {onClose && (
        <button
          type="button"
          aria-label="Đóng mặt cắt"
          className="absolute right-1 top-1 h-[18px] w-[18px] text-[13px] leading-none"
          style={{ background: palette.fill, color: palette.text }}
          onPointerDown={stop}
          onClick={(e) => {
            e.stopPropagation();
            onClose();
          }}
        >
          ✕
        </button>
      )}
    </div>
  );
}
