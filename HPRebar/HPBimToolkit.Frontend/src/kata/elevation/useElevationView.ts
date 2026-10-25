import { useCallback, useEffect, useRef, useState } from 'react';
import type { KataFocusEvent, KataWebElevation, KataWebRange } from '../kataScene';
import { FOCUS_MARGIN_PX, MARGIN_PX, MAX_SCALE, WHEEL_STEP, frame, frameAll, frameColumn, minScale, type FrameOptions } from './framing';
import { ensureVisible, panBy, zoomAt, type Viewport } from './viewport';

export interface ElevationView {
  size: { width: number; height: number };
  viewport: Viewport | null;
  zoom(x: number, y: number, notches: number): void;
  pan(dx: number, dy: number): void;
  frameWhole(): void;
  /** Frames `range` (stations) as a span is framed, gliding there. */
  frameRange(range: KataWebRange): void;
  /** Pans so that station `x` sits in the middle of the area. */
  centreOn(x: number): void;
}

/** How long a framing change glides (ms); a wheel notch or a drag is never animated. */
const GLIDE_MS = 220;

const ease = (t: number) => 1 - (1 - t) ** 3;

const between = (a: Viewport, b: Viewport, t: number): Viewport => {
  // Scale glides geometrically so that a large zoom does not rush through its first half.
  const scale = a.scale * (b.scale / a.scale) ** t;
  return { scale, offsetX: a.offsetX + (b.offsetX - a.offsetX) * t, offsetY: a.offsetY + (b.offsetY - a.offsetY) * t };
};

const canAnimate = () =>
  typeof requestAnimationFrame === 'function' && !(typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches);

/** The same run rebuilt (a parameter changed) has the same bounds and columns; anything else is a new run. */
const runKey = (e: KataWebElevation | null) => (e ? `${e.bounds.start}|${e.bounds.end}|${e.columns.length}` : '');

/**
 * The view of the elevation, kept by the page (port of the canvas's framing rules): framed whole until the user zooms
 * or pans; refitted on resize while not user-framed; a new run starts framed whole, the same run rebuilt (a parameter
 * changed) keeps the user's framing; the view model's focus requests frame a span or the run; a column picked in the
 * table is brought into view sideways.
 */
export function useElevationView(
  element: React.RefObject<HTMLElement | null>,
  elevation: KataWebElevation | null,
  selectedColumn: number,
  focus: KataFocusEvent | null,
  options?: FrameOptions & { range?: KataWebRange },
): ElevationView {
  const [size, setSize] = useState({ width: 0, height: 0 });
  // The user's framing belongs to the run it was made on: a new run starts framed whole again.
  const [manual, setManual] = useState<{ run: string; viewport: Viewport } | null>(null);
  const run = runKey(elevation);

  useEffect(() => {
    const target = element.current;
    if (!target) return;
    const report = () => setSize({ width: target.clientWidth, height: target.clientHeight });
    const observer = new ResizeObserver(report);
    observer.observe(target);
    report();
    return () => observer.disconnect();
  }, [element]);

  const fitted = elevation && size.width > 0 && size.height > 0 ? frameAll(elevation, size.width, size.height, options) : null;
  const viewport = manual && manual.run === run ? manual.viewport : fitted;

  // Gestures read the view as it is now, not as it was when their callback was made.
  const latest = useRef({ viewport, elevation, size, options, run });
  useEffect(() => {
    latest.current = { viewport, elevation, size, options, run };
  });

  const glide = useRef(0);

  /** null = framed whole (refitted on resize). Any direct gesture stops a glide on its way. */
  const setUser = useCallback((next: Viewport | null) => {
    cancelAnimationFrame(glide.current);
    setManual(next ? { run: latest.current.run, viewport: next } : null);
  }, []);

  /** Glides from the view on screen to `target` (null: framed whole), then holds it. */
  const glideTo = useCallback(
    (target: Viewport | null) => {
      const { viewport: from, elevation: e, size: s, options: o, run: key } = latest.current;
      const to = target ?? (e ? frameAll(e, s.width, s.height, o) : null);
      if (!from || !to || !canAnimate()) {
        setUser(target);
        return;
      }
      cancelAnimationFrame(glide.current);
      const started = performance.now();
      // The frame time rAF passes may run on another clock than performance.now(): read one clock, keep t in 0…1.
      const step = () => {
        const t = Math.max(0, Math.min(1, (performance.now() - started) / GLIDE_MS));
        if (t >= 1) {
          setManual(target ? { run: key, viewport: target } : null);
          return;
        }
        setManual({ run: key, viewport: between(from, to, ease(t)) });
        glide.current = requestAnimationFrame(step);
      };
      glide.current = requestAnimationFrame(step);
    },
    [setUser],
  );
  useEffect(() => () => cancelAnimationFrame(glide.current), []);

  const zoom = useCallback(
    (x: number, y: number, notches: number) => {
      const { viewport: v, elevation: e, size: s, options: o } = latest.current;
      if (!v || !e) return;
      setUser(zoomAt(v, WHEEL_STEP ** notches, x, y, minScale(e, s.width, s.height, o), MAX_SCALE));
    },
    [setUser],
  );

  const pan = useCallback(
    (dx: number, dy: number) => {
      const v = latest.current.viewport;
      if (v && (dx !== 0 || dy !== 0)) setUser(panBy(v, dx, dy));
    },
    [setUser],
  );

  const frameWhole = useCallback(() => glideTo(null), [glideTo]);

  const frameRange = useCallback(
    (range: KataWebRange) => {
      const { elevation: e, size: s, options: o } = latest.current;
      if (e && s.width > 0) glideTo(frame(e, range, s.width, s.height, FOCUS_MARGIN_PX, o));
    },
    [glideTo],
  );

  const centreOn = useCallback(
    (x: number) => {
      const { viewport: v, size: s } = latest.current;
      if (v) setUser({ ...v, offsetX: s.width / 2 - x * v.scale });
    },
    [setUser],
  );

  // The view model asks to frame a span (with its supports) or the whole run.
  useEffect(() => {
    if (!focus) return;
    const { elevation: e, size: s, options: o } = latest.current;
    if (!e || s.width < 1) return;
    if (focus.columnIndex !== null && focus.columnIndex >= 0 && focus.columnIndex < e.columns.length) glideTo(frameColumn(e, focus.columnIndex, s.width, s.height, o));
    else glideTo(null);
  }, [focus, glideTo]);

  // A column picked in the table is brought into view sideways, without changing the zoom or the height.
  useEffect(() => {
    const { viewport: v, elevation: e, size: s } = latest.current;
    const column = e?.columns[selectedColumn];
    if (!v || !column) return;
    const next = ensureVisible(v, column.extent, Math.max(1, s.width - (latest.current.options?.reserve?.(s.width, s.height) ?? 0)), MARGIN_PX);
    if (next !== v) setUser(next);
  }, [selectedColumn, setUser]);

  return { size, viewport, zoom, pan, frameWhole, frameRange, centreOn };
}
