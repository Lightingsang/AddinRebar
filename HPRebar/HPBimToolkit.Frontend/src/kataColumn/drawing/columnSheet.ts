// Where Kata's column drawing puts things, as VC lays them out in AutoCAD: the elevation along b at 1:30 in model mm,
// and right of it the storeys' sections at 1:20, i.e. 30 / 20 = 1.5 × larger in the same sheet, top storey first.
// Sheet mm: x to the right, y up (the elevation's z). One viewport scales the whole sheet, like paper.

import type { KataColumnElevationDrawing, KataColumnSectionDrawing } from '../columnContracts';

/** Scale of the sections against the elevation (1:20 beside 1:30). */
export const SECTION_FACTOR = 30 / 20;

/** Elevation dimension lines sit this far apart (KataColumnElevationDims.GapMm). */
export const DIM_GAP_MM = 300;

/** Room for the bar labels outside the dimension lines (mm). */
export const LABEL_ROOM_MM = 1600;

/** Level lines reach this far left of the stack, for the level names and CT marks. */
export const LEVEL_REACH_MM = 2400;

const SECTION_GAP_MM = 1100;

export interface PlacedSection {
  index: number;
  name: string;
  drawing: KataColumnSectionDrawing;
  /** Sheet position of the section's lower-left corner. */
  x: number;
  y: number;
}

export interface Extents {
  left: number;
  right: number;
  bottom: number;
  top: number;
}

export interface ColumnSheet {
  elevation: KataColumnElevationDrawing;
  sections: PlacedSection[];
  /** Left end of the level lines, where the level names go. */
  levelLeft: number;
  /** Where group A's labels and group B's labels stand. */
  labelLeft: number;
  labelRight: number;
  extents: Extents;
}

export function layoutSheet(elevation: KataColumnElevationDrawing, sections: KataColumnSectionDrawing[]): ColumnSheet {
  const labelLeft = elevation.leftMm - 2 * DIM_GAP_MM - 300;
  const labelRight = elevation.rightMm + 4 * DIM_GAP_MM + 300;
  const levelLeft = Math.min(elevation.pileCapLeftMm, labelLeft - LABEL_ROOM_MM) - LEVEL_REACH_MM / 2;
  const sectionLeft = labelRight + LABEL_ROOM_MM + 600;

  const placed: PlacedSection[] = [];
  let top = elevation.topMm;
  // Top storey first, as the drawing reads from the roof down.
  for (let k = sections.length - 1; k >= 0; k--) {
    const drawing = sections[k]!;
    const height = drawing.depthMm * SECTION_FACTOR;
    const y = top - height;
    placed.push({ index: k, name: elevation.cuts[k]?.name ?? `${k + 1}-${k + 1}`, drawing, x: sectionLeft, y });
    top = y - SECTION_GAP_MM;
  }

  const widest = Math.max(0, ...sections.map((s) => s.widthMm * SECTION_FACTOR));
  return {
    elevation,
    sections: placed,
    levelLeft,
    labelLeft,
    labelRight,
    extents: {
      left: levelLeft,
      right: sections.length > 0 ? sectionLeft + widest + 600 : labelRight + LABEL_ROOM_MM,
      bottom: Math.min(elevation.bottomMm - 800, top),
      top: elevation.topMm + 600,
    },
  };
}

/** A section's own mm (x along b, y along h from its lower-left corner) on the sheet. */
export const sectionPoint = (s: PlacedSection, x: number, y: number): [number, number] => [s.x + x * SECTION_FACTOR, s.y + y * SECTION_FACTOR];

/** Placement of the sheet on screen: one scale both ways (px per sheet mm), y up. */
export interface SheetView {
  scale: number;
  offsetX: number;
  offsetY: number;
}

export const toScreen = (v: SheetView, x: number, y: number): [number, number] => [v.offsetX + x * v.scale, v.offsetY - y * v.scale];

export const toSheet = (v: SheetView, sx: number, sy: number): [number, number] => [(sx - v.offsetX) / v.scale, (v.offsetY - sy) / v.scale];

/** The view that shows `extents` whole in a `width` × `height` box with `margin` px around. */
export function fit(extents: Extents, width: number, height: number, margin = 24): SheetView {
  const w = Math.max(1, extents.right - extents.left);
  const h = Math.max(1, extents.top - extents.bottom);
  const scale = Math.max(1e-4, Math.min((width - 2 * margin) / w, (height - 2 * margin) / h));
  return {
    scale,
    offsetX: (width - w * scale) / 2 - extents.left * scale,
    offsetY: (height + h * scale) / 2 + extents.bottom * scale,
  };
}

/** Zoom by `factor` keeping the sheet point under (sx, sy) in place. */
export function zoomAt(v: SheetView, factor: number, sx: number, sy: number): SheetView {
  const scale = v.scale * factor;
  return { scale, offsetX: sx - (sx - v.offsetX) * factor, offsetY: sy - (sy - v.offsetY) * factor };
}

/** Closest zoom: 20 px per sheet mm (a bar Ø fills the screen); farthest: a quarter of the fit. */
export const MAX_SCALE = 20;
const MIN_FIT_FRACTION = 1 / 4;

/** `next` unless it zooms past the limits, then `previous` (the wheel stops there). */
export function clampZoom(previous: SheetView, next: SheetView, fitScale: number): SheetView {
  if (next.scale > MAX_SCALE && next.scale > previous.scale) return previous;
  if (fitScale > 0 && next.scale < fitScale * MIN_FIT_FRACTION && next.scale < previous.scale) return previous;
  return next;
}

export const sectionExtents = (s: PlacedSection): Extents => ({
  left: s.x - 400,
  right: s.x + s.drawing.widthMm * SECTION_FACTOR + 400,
  bottom: s.y - 700,
  top: s.y + s.drawing.depthMm * SECTION_FACTOR + 400,
});
