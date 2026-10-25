// Where everything of the plain elevation goes for one paint (port of KataElevationScene.cs and the station helpers
// of KataElevation): the beam band is placed by the viewport, one scale both ways; the rows of labels above (letters,
// dimension chain) and below (dimension chains, grid bubbles) keep fixed pixel sizes.

import type { KataWebElevation, KataWebRange } from '../kataScene';
import { contains, length, mid, overlaps, toScreen, toScreenY, union, type Viewport } from './viewport';

export const BUBBLE_RADIUS = 12;
export const UPPER_STUB_PX = 26;
export const LOWER_STUB_PX = 28;
export const FOOTING_PX = 26;

/** Margin above the beam band (bar tags excluded): upper column stubs, top dim chain, column letters, grid line top. */
export const ABOVE_PX = 142;

/** Margin below the band bottom (bar tags excluded): lower column stubs, 2 dim chains, grid bubbles. */
export const BELOW_PX = 150;

export interface Scene {
  elevation: KataWebElevation;
  viewport: Viewport;
  width: number;
  height: number;
  selectedColumn: number;
  bandTopY: number;
  bandBottomY: number;
  topChainY: number;
  letterY: number;
  captionY: number;
  gridLineTopY: number;
  selectionTopY: number;
  markerTextY: number;
  chainY: number;
  gridChainY: number;
  bubbleY: number;
  gridLineBottomY: number;
}

/**
 * @param tagsAbovePx Room the bar tags take over the beam at this zoom (0 when they are hidden).
 * @param tagsBelowPx Room they take under it.
 */
export function buildScene(
  elevation: KataWebElevation,
  viewport: Viewport,
  width: number,
  height: number,
  selectedColumn: number,
  tagsAbovePx = 0,
  tagsBelowPx = 0,
): Scene {
  const bandTopY = viewport.offsetY;
  const bandBottomY = toScreenY(viewport, elevation.topMm - elevation.bottomMm);
  const topChainY = bandTopY - Math.max(UPPER_STUB_PX, tagsAbovePx) - 20;
  const letterY = topChainY - 35;
  const below = Math.max(LOWER_STUB_PX, tagsBelowPx);
  const chainY = bandBottomY + below + 22;
  const gridChainY = chainY + 22;
  const bubbleY = gridChainY + 28;
  return {
    elevation,
    viewport,
    width,
    height,
    selectedColumn,
    bandTopY,
    bandBottomY,
    topChainY,
    letterY,
    captionY: bandTopY - 10,
    gridLineTopY: topChainY,
    selectionTopY: letterY - 2,
    markerTextY: bandBottomY + LOWER_STUB_PX + 4,
    chainY,
    gridChainY,
    bubbleY,
    gridLineBottomY: bubbleY + BUBBLE_RADIUS + 8,
  };
}

export const sx = (scene: Scene, station: number) => toScreen(scene.viewport, station);

/** Screen y of a height relative to the reference level. */
export const sy = (scene: Scene, heightMm: number) => toScreenY(scene.viewport, scene.elevation.topMm - heightMm);

export const isVisible = (scene: Scene, left: number, right: number) => right >= 0 && left <= scene.width;

/** Top and bottom of the beams over `extent` (the whole band when none reaches it). */
export function beamFaces(elevation: KataWebElevation, extent: KataWebRange): { top: number; bottom: number } {
  let top = -Infinity;
  let bottom = Infinity;
  for (const beam of elevation.beams) {
    if (!overlaps(beam.extent, extent)) continue;
    top = Math.max(top, beam.topMm);
    bottom = Math.min(bottom, beam.bottomMm);
  }
  return Number.isFinite(top) ? { top, bottom } : { top: elevation.topMm, bottom: elevation.bottomMm };
}

export interface BeamPiece {
  start: number;
  end: number;
  top: number;
  bottom: number;
}

/**
 * The beams under `extent`, each cut to it, in station order. A span made of several beams (a deeper or dropped piece
 * between a support and a transfer column) keeps one box per beam, so its steps show.
 */
export function beamPieces(elevation: KataWebElevation, extent: KataWebRange): BeamPiece[] {
  return elevation.beams
    .map((b) => ({ start: Math.max(b.extent.start, extent.start), end: Math.min(b.extent.end, extent.end), top: b.topMm, bottom: b.bottomMm }))
    .filter((p) => p.end - p.start > 1)
    .sort((a, b) => a.start - b.start);
}

/** Top and bottom of the beam of `span` that meets a support edge at `station` (the span's first or last piece). */
export function facesAt(elevation: KataWebElevation, span: KataWebRange, station: number): { top: number; bottom: number } {
  const inside = station <= mid(span) ? { start: station, end: station + 1 } : { start: station - 1, end: station };
  const piece = beamPieces(elevation, span).find((p) => overlaps(p, inside, 0));
  return piece ? { top: piece.top, bottom: piece.bottom } : beamFaces(elevation, span);
}

/** A column's screen span, at least `minPx` wide so zero-width columns stay visible. */
export function screenSpan(scene: Scene, extent: KataWebRange, minPx: number): { left: number; right: number } {
  const left = sx(scene, extent.start);
  const right = sx(scene, extent.end);
  if (right - left >= minPx) return { left, right };
  const m = (left + right) / 2;
  return { left: m - minPx / 2, right: m + minPx / 2 };
}

export const isZeroWidth = (kind: string) => kind === 'joint' || kind === 'freeEnd';

/**
 * The column under a station: a zero-width column within `zeroWidthReachMm` wins (it has no width to click), then the
 * column whose extent holds the station; null outside every column.
 */
export function columnAt(elevation: KataWebElevation, x: number, zeroWidthReachMm: number): number | null {
  const near = elevation.columns
    .filter((c) => length(c.extent) <= 1 && Math.abs(mid(c.extent) - x) <= zeroWidthReachMm)
    .sort((a, b) => Math.abs(mid(a.extent) - x) - Math.abs(mid(b.extent) - x))[0];
  if (near) return near.index;
  return elevation.columns.find((c) => length(c.extent) > 1 && contains(c.extent, x, 0))?.index ?? null;
}

/** A span with the supports on either side, for zooming onto it. */
export function focusRange(elevation: KataWebElevation, columnIndex: number): KataWebRange {
  const columns = elevation.columns;
  const first = Math.max(0, columnIndex - 1);
  const last = Math.min(columns.length - 1, columnIndex + 1);
  let range = columns[first]!.extent;
  for (let i = first + 1; i <= last; i++) range = union(range, columns[i]!.extent);
  return range;
}
