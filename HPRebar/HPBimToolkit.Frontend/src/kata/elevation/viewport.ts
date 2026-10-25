// Placement of the elevation on screen, one scale in both directions as in AutoCAD (port of
// HPRebar.Core/KataExport/Calculators/KataElevationViewport.cs): screen x = offsetX + station × scale, screen y =
// offsetY + (depth below the highest beam top) × scale. Immutable; every gesture returns a new viewport.

import type { KataWebRange } from '../kataScene';

export interface Viewport {
  scale: number;
  offsetX: number;
  offsetY: number;
}

export const toScreen = (v: Viewport, station: number) => v.offsetX + station * v.scale;

export const toStation = (v: Viewport, screenX: number) => (screenX - v.offsetX) / v.scale;

/** Screen y of a point `depthMm` below the highest beam top. */
export const toScreenY = (v: Viewport, depthMm: number) => v.offsetY + depthMm * v.scale;

export const length = (r: KataWebRange) => r.end - r.start;

export const mid = (r: KataWebRange) => (r.start + r.end) / 2;

/** Stations within 1 mm (KataTolerance.StationMm) of the range count as inside, as in Interval1D. */
export const contains = (r: KataWebRange, station: number, tolerance = 1) => station >= r.start - tolerance && station <= r.end + tolerance;

export const overlaps = (a: KataWebRange, b: KataWebRange, tolerance = 1) => b.start <= a.end + tolerance && b.end >= a.start - tolerance;

export const union = (a: KataWebRange, b: KataWebRange): KataWebRange => ({ start: Math.min(a.start, b.start), end: Math.max(a.end, b.end) });

/** The whole `range` centred in `widthPx` with `marginPx` each side. */
export function fit(range: KataWebRange, widthPx: number, marginPx: number): Viewport {
  const usable = Math.max(1, widthPx - 2 * marginPx);
  const scale = usable / Math.max(1, length(range));
  const offsetX = (widthPx - length(range) * scale) / 2 - range.start * scale;
  return { scale, offsetX, offsetY: 0 };
}

/**
 * Places the beam band (`bandDepthMm` deep, with `abovePx` of labels over it and `belowPx` under it) in the middle of
 * `heightPx`; when it does not fit, the top labels stay on screen.
 */
export function centreBand(v: Viewport, bandDepthMm: number, heightPx: number, abovePx: number, belowPx: number): Viewport {
  const total = abovePx + bandDepthMm * v.scale + belowPx;
  const top = total <= heightPx ? (heightPx - total) / 2 : 0;
  return { ...v, offsetY: top + abovePx };
}

/**
 * Zoom by `factor` keeping the point under (anchorX, anchorY) still. The limits only stop movement in their own
 * direction: a view already past one (the window was resized) is never pulled back by the next step.
 */
export function zoomAt(v: Viewport, factor: number, anchorX: number, anchorY: number, minScale: number, maxScale: number): Viewport {
  const target = v.scale * factor;
  const scale = factor >= 1 ? Math.min(Math.max(maxScale, v.scale), target) : Math.max(Math.min(minScale, v.scale), target);
  if (!(scale > 0) || !Number.isFinite(scale) || !(v.scale > 0)) return v;

  const station = toStation(v, anchorX);
  const offsetY = anchorY - ((anchorY - v.offsetY) * scale) / v.scale;
  return { scale, offsetX: anchorX - station * scale, offsetY };
}

/**
 * Zooms out, keeping `anchorX` still, until the beam band and its labels fit in `heightPx` — the vertical half of a
 * "zoom extents". Nothing changes when they already fit.
 */
export function shrinkToHeight(v: Viewport, bandDepthMm: number, heightPx: number, abovePx: number, belowPx: number, anchorX: number): Viewport {
  const room = heightPx - abovePx - belowPx;
  if (room <= 0 || bandDepthMm <= 0 || bandDepthMm * v.scale <= room) return v;
  const wanted = room / bandDepthMm;
  return wanted < v.scale ? zoomAt(v, wanted / v.scale, anchorX, 0, 0, v.scale) : v;
}

export const panBy = (v: Viewport, dx: number, dy = 0): Viewport => ({ ...v, offsetX: v.offsetX + dx, offsetY: v.offsetY + dy });

/**
 * Pans the least amount horizontally that brings `range` inside the margins; zoom and vertical position are kept. A
 * range wider than the view stays put while any of it is on screen, and is centred otherwise.
 */
export function ensureVisible(v: Viewport, range: KataWebRange, widthPx: number, marginPx: number): Viewport {
  const left = toScreen(v, range.start);
  const right = toScreen(v, range.end);
  if (right - left > widthPx - 2 * marginPx) return right < marginPx || left > widthPx - marginPx ? panBy(v, widthPx / 2 - (left + right) / 2) : v;
  if (left < marginPx) return panBy(v, marginPx - left);
  if (right > widthPx - marginPx) return panBy(v, widthPx - marginPx - right);
  return v;
}
