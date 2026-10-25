// Framing of the elevation (port of KataElevationCanvas.Framing.cs and the canvas constants): "zoom extents", a span
// framed with its supports, the zoom limits. The band (mm drawn over and under the beam band, e.g. Kata's tags) and
// the pixel rows kept around it are parameters, so the rebar drawing can frame with its own.

import type { KataWebElevation, KataWebRange, KataWebRebar, KataWebStationMap } from '../kataScene';
import { ABOVE_PX, BELOW_PX, focusRange } from './scene';
import { centreBand, fit, panBy, shrinkToHeight, zoomAt, type Viewport } from './viewport';

export const MARGIN_PX = 36;
export const FOCUS_MARGIN_PX = 60;

/** Wheel limit — 20 px per mm, deep enough to read a 2.5 mm Kata tag text at any print scale. */
export const MAX_SCALE = 20;

/** Zooming out stops at this fraction of "zoom extents". */
export const MIN_SCALE_OF_EXTENTS = 1 / 20;

/** Framing a span never zooms in further than this, so its neighbours and labels stay in view. */
export const FOCUS_MAX_SCALE = 1.2;

/** Zoom per wheel notch (AutoCAD's ZOOMFACTOR 60). */
export const WHEEL_STEP = 1.25;

/** A click this close to a joint or free end (px) picks it. */
export const ZERO_WIDTH_REACH_PX = 7;

export interface Band {
  /** Millimetres drawn over the highest beam top. */
  above: number;
  /** Millimetres drawn under the band bottom. */
  below: number;
}

export interface FrameOptions {
  band?: Band;
  /** Pixels kept free over and under what is drawn in millimetres (the plain elevation's label rows by default). */
  rows?: { above: number; below: number };
  /** Pixels taken off the right of an area this size (the section card beside the plain elevation): the run stays left of it. */
  reserve?: (width: number, height: number) => number;
}

/**
 * "Zoom extents" on `range`: across the width (capped at FOCUS_MAX_SCALE), zoomed out further if the beam band, its
 * tags and its labels would not fit the height, then centred. One scale both ways.
 */
export function frame(elevation: KataWebElevation, range: KataWebRange, width: number, height: number, marginPx: number, options: FrameOptions = {}): Viewport {
  const band = options.band ?? { above: 0, below: 0 };
  const rows = options.rows ?? { above: ABOVE_PX, below: BELOW_PX };
  const w = Math.max(1, width - (options.reserve?.(width, height) ?? 0));
  const h = Math.max(1, height);
  const depth = elevation.topMm - elevation.bottomMm + band.above + band.below;
  let viewport = fit(range, w, marginPx);
  if (viewport.scale > FOCUS_MAX_SCALE) viewport = zoomAt(viewport, FOCUS_MAX_SCALE / viewport.scale, w / 2, 0, 0, FOCUS_MAX_SCALE);
  viewport = shrinkToHeight(viewport, depth, h, rows.above, rows.below, w / 2);
  viewport = centreBand(viewport, depth, h, rows.above, rows.below);
  return panBy(viewport, 0, band.above * viewport.scale);
}

export const frameAll = (elevation: KataWebElevation, width: number, height: number, options?: FrameOptions & { range?: KataWebRange }) =>
  frame(elevation, options?.range ?? elevation.bounds, width, height, MARGIN_PX, options);

export const frameColumn = (elevation: KataWebElevation, index: number, width: number, height: number, options?: FrameOptions) =>
  frame(elevation, focusRange(elevation, index), width, height, FOCUS_MARGIN_PX, options);

/** Zooming out stops at a twentieth of the whole run's "zoom extents". */
export const minScale = (elevation: KataWebElevation, width: number, height: number, options?: FrameOptions & { range?: KataWebRange }) =>
  frameAll(elevation, width, height, options).scale * MIN_SCALE_OF_EXTENTS;

/** Pixels kept free over and under Kata's elevation when it is framed. */
export const KATA_MARGIN_PX = 12;

/** Kata's level mark runs this far right of its tip (lines 289, text after them). */
const LEVEL_MARK_REACH = 400;

/**
 * Framing of Kata's elevation (KataElevationCanvas.Band / MarginRows / FrameRange): it reaches from its flags to its
 * title, or further where a cut carries more rows of tags; the whole run is widened to its level mark and depth
 * dimensions.
 */
export function kataFrameOptions(elevation: KataWebElevation, rebar: KataWebRebar, map: KataWebStationMap, tags: { above: number; below: number } | null): FrameOptions & { range: KataWebRange } {
  const depth = elevation.topMm - elevation.bottomMm;
  const band = {
    above: Math.max(Math.max(0, rebar.top), tags?.above ?? 0),
    below: Math.max(Math.max(0, -rebar.bottom - depth), tags?.below ?? 0),
  };
  const a = map.origin + map.direction * rebar.minX;
  const b = map.origin + map.direction * rebar.maxX;
  const range = {
    start: Math.min(elevation.bounds.start, a, b) - LEVEL_MARK_REACH,
    end: Math.max(elevation.bounds.end, a, b) + LEVEL_MARK_REACH,
  };
  return { band, rows: { above: KATA_MARGIN_PX, below: KATA_MARGIN_PX }, range };
}

