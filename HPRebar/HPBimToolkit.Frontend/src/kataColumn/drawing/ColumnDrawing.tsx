import { useEffect, useLayoutEffect, useMemo, useRef, useState, type PointerEvent } from 'react';
import { Maximize } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { wheelNotches, DRAG_THRESHOLD_PX } from '../../kata/elevationGestures';
import { WHEEL_STEP } from '../../kata/elevation/framing';
import { paletteFor } from '../../kata/elevation/palette';
import { ShapesSvg } from '../../kata/elevation/ShapesSvg';
import type { KataColumnElevationDrawing, KataColumnSectionDrawing, KataTheme } from '../columnContracts';
import type { COLUMN_STRINGS } from '../i18n';
import { barAt, paintColumnSheet } from './columnPainter';
import { clampZoom, fit, layoutSheet, sectionExtents, toSheet, zoomAt, type SheetView } from './columnSheet';

type Strings = (typeof COLUMN_STRINGS)['vi'];

interface ColumnDrawingProps {
  elevation: KataColumnElevationDrawing;
  sections: KataColumnSectionDrawing[];
  theme: KataTheme;
  t: Strings;
}

/**
 * Kata's column drawing on the page, as VC draws it in AutoCAD: the elevation along b and the storeys' sections in one
 * sheet. The wheel zooms around the cursor, a drag pans, a double click fits the sheet; a click on a bar selects it
 * (highlighted in the elevation and every section, its facts in a chip), a click elsewhere in a section frames it.
 */
export function ColumnDrawing({ elevation, sections, theme, t }: ColumnDrawingProps) {
  const box = useRef<HTMLDivElement>(null);
  const [size, setSize] = useState({ width: 0, height: 0 });
  const [view, setView] = useState<SheetView | null>(null);
  const [picked, setPicked] = useState<number | null>(null);
  const [hover, setHover] = useState<number | null>(null);
  const press = useRef<{ x: number; y: number; view: SheetView; moved: boolean } | null>(null);
  // Pan updates wait for the next frame: one repaint per frame however fast the pointer moves.
  const frame = useRef<{ id: number; view: SheetView } | null>(null);

  const sheet = useMemo(() => layoutSheet(elevation, sections), [elevation, sections]);
  const palette = paletteFor(theme);

  useLayoutEffect(() => {
    const element = box.current;
    if (!element) return;
    const measure = () => setSize({ width: element.clientWidth, height: element.clientHeight });
    measure();
    if (typeof ResizeObserver === 'undefined') {
      window.addEventListener('resize', measure);
      return () => window.removeEventListener('resize', measure);
    }
    const observer = new ResizeObserver(measure);
    observer.observe(element);
    return () => observer.disconnect();
  }, []);

  // Fit when the drawing itself changes (another column, other bars) or the box first gets a size; reading the same
  // sheet again after a creation keeps the user's zoom, and so does a resize.
  const content = useMemo(() => JSON.stringify(elevation) + JSON.stringify(sections), [elevation, sections]);
  const sized = size.width > 0 && size.height > 0;
  useLayoutEffect(() => {
    if (sized) setView(fit(sheet.extents, box.current!.clientWidth, box.current!.clientHeight));
    // eslint-disable-next-line react-hooks/exhaustive-deps -- refit on content, not on every new sheet object
  }, [content, sized]);

  // The wheel zooms around the cursor and never scrolls the page (React's onWheel cannot cancel the scroll).
  const fitScale = sized ? fit(sheet.extents, size.width, size.height).scale : 0;
  useEffect(() => {
    const element = box.current;
    if (!element) return;
    const onWheel = (e: WheelEvent) => {
      e.preventDefault();
      const rect = element.getBoundingClientRect();
      const factor = WHEEL_STEP ** wheelNotches(e.deltaY, e.deltaMode);
      setView((v) => (v ? clampZoom(v, zoomAt(v, factor, e.clientX - rect.left, e.clientY - rect.top), fitScale) : v));
    };
    element.addEventListener('wheel', onWheel, { passive: false });
    return () => element.removeEventListener('wheel', onWheel);
  }, [fitScale]);

  useEffect(() => () => {
    if (frame.current) cancelAnimationFrame(frame.current.id);
  }, []);

  // A pinned bar that the reloaded drawing no longer has is dropped.
  useEffect(() => {
    if (picked !== null && !sheet.elevation.bars.some((b) => b.number === picked) && !sections.some((s) => s.bars.some((b) => b.number === picked))) setPicked(null);
  }, [sheet, sections, picked]);

  const highlight = hover ?? picked;
  const painted = useMemo(() => (view ? paintColumnSheet(sheet, view, palette, highlight) : null), [sheet, view, palette, highlight]);

  const local = (e: { clientX: number; clientY: number }) => {
    const rect = box.current!.getBoundingClientRect();
    return [e.clientX - rect.left, e.clientY - rect.top] as const;
  };

  const onPointerDown = (e: PointerEvent<HTMLDivElement>) => {
    if (!view || (e.button !== 0 && e.button !== 1)) return;
    // No text selection, and no browser autoscroll on the middle button.
    e.preventDefault();
    const [x, y] = local(e);
    press.current = { x, y, view, moved: false };
    try {
      e.currentTarget.setPointerCapture(e.pointerId);
    } catch {
      // A synthetic pointer (tests, harness) has no active pointer to capture; the press still works without it.
    }
  };

  const onPointerMove = (e: PointerEvent<HTMLDivElement>) => {
    const [x, y] = local(e);
    const p = press.current;
    if (p && e.buttons === 0) {
      // The button went up outside the box without a pointerup (no capture): the press is over.
      press.current = null;
    } else if (p) {
      if (!p.moved && Math.hypot(x - p.x, y - p.y) < DRAG_THRESHOLD_PX) return;
      p.moved = true;
      const next = { ...p.view, offsetX: p.view.offsetX + x - p.x, offsetY: p.view.offsetY + y - p.y };
      if (frame.current) frame.current.view = next;
      else
        frame.current = {
          view: next,
          id: requestAnimationFrame(() => {
            const pending = frame.current;
            frame.current = null;
            if (pending) setView(pending.view);
          }),
        };
      return;
    }
    if (painted) setHover(barAt(painted.hits, x, y));
  };

  const onPointerUp = (e: PointerEvent<HTMLDivElement>) => {
    const p = press.current;
    press.current = null;
    if (frame.current) {
      // The last pan step is still waiting for its frame: show where the drag ended.
      cancelAnimationFrame(frame.current.id);
      setView(frame.current.view);
      frame.current = null;
    }
    if (!p || p.moved || !painted || !view) return;
    const [x, y] = local(e);
    const number = barAt(painted.hits, x, y);
    if (number !== null) {
      setPicked(number === picked ? null : number);
      return;
    }

    setPicked(null);
    const [sx, sy] = toSheet(view, x, y);
    const section = sheet.sections.find((s) => {
      const r = sectionExtents(s);
      return sx >= r.left && sx <= r.right && sy >= r.bottom && sy <= r.top;
    });
    if (section) setView(fit(sectionExtents(section), size.width, size.height, 32));
  };

  const fitAll = () => size.width > 0 && setView(fit(sheet.extents, size.width, size.height));
  const chip = picked !== null ? describeBar(picked, sheet.elevation, sections, t) : null;

  return (
    <section className="flex min-h-[480px] flex-col overflow-hidden rounded-xl border bg-card" aria-label={t.drawing} data-testid="column-drawing">
      <h2 className="flex items-center justify-between gap-3 border-b px-4 py-2 text-[12px] font-semibold text-muted-foreground">
        <span>{t.drawing}</span>
        <span className="flex items-center gap-2 font-normal">
          <span className="hidden sm:inline">{t.drawingHint}</span>
          <Button variant="ghost" size="icon-sm" aria-label={t.fit} title={t.fit} onClick={fitAll} data-testid="drawing-fit">
            <Maximize />
          </Button>
        </span>
      </h2>
      <div
        ref={box}
        className="relative h-[68vh] min-h-[420px] touch-none select-none"
        style={{ background: palette.fill, cursor: hover !== null ? 'pointer' : press.current?.moved ? 'grabbing' : 'default' }}
        onPointerDown={onPointerDown}
        onPointerMove={onPointerMove}
        onPointerUp={onPointerUp}
        onPointerLeave={() => setHover(null)}
        onPointerCancel={() => (press.current = null)}
        onLostPointerCapture={() => (press.current = null)}
        onDoubleClick={fitAll}
        data-testid="drawing-canvas"
      >
        {painted && (
          <svg width={size.width} height={size.height} className="absolute inset-0" role="img" aria-label={t.drawing}>
            <ShapesSvg shapes={painted.shapes} />
          </svg>
        )}
        {chip && (
          <div className="absolute bottom-3 left-3 rounded-lg border bg-card/95 px-3 py-2 text-[12px] shadow-sm" data-testid="drawing-chip">
            <div className="font-semibold">{chip.title}</div>
            <div className="text-muted-foreground">{chip.facts}</div>
          </div>
        )}
      </div>
    </section>
  );
}

/** What the chip says about a bar number: from its elevation bar when drawn there, else from a section. */
export function describeBar(number: number, elevation: KataColumnElevationDrawing, sections: KataColumnSectionDrawing[], t: Strings) {
  const bar = elevation.bars.find((b) => b.number === number);
  if (bar) {
    // Starters stand in the pile cap (its level, when the sheet has one) below the first storey.
    const cap = elevation.levels[0] && elevation.storeys[0] && elevation.levels[0].zMm < elevation.storeys[0].baseMm ? elevation.levels[0].name : null;
    const level = bar.kind === 'starter' ? (cap ?? elevation.storeys[0]?.name ?? '') : (elevation.storeys[bar.storey]?.name ?? '');
    return { title: t.barNumber(number), facts: t.barFacts(bar.diameterMm, bar.lengthMm, bar.group.toUpperCase(), bar.layer, level) };
  }

  const index = sections.findIndex((s) => s.bars.some((b) => b.number === number));
  if (index < 0) return null;
  const sectionBar = sections[index]!.bars.find((b) => b.number === number)!;
  return { title: t.barNumber(number), facts: t.barFacts(sectionBar.diameterMm, null, '–', sectionBar.layer, sections[index]!.level) };
}
