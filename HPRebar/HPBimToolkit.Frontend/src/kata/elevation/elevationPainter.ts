// The geometry of the plain elevation (port of KataElevationPainter.cs): selection band, grid lines and bubbles,
// supports by kind (column stubs, footing blocks, crossing-beam sections), the beams, joints and free ends. Texts that
// describe the geometry are elevationAnnotations'.

import type { KataWebColumn, KataWebSupport } from '../kataScene';
import { LabelLane } from './labelLane';
import type { Palette } from './palette';
import { BUBBLE_RADIUS, FOOTING_PX, LOWER_STUB_PX, UPPER_STUB_PX, beamFaces, beamPieces, facesAt, isVisible, isZeroWidth, screenSpan, sx, sy, type Scene } from './scene';
import { ShapeList, type Shape } from './shapes';
import { TEXT_SIZE, measure } from './text';
import { contains, mid } from './viewport';

export function paintElevation(scene: Scene, palette: Palette): Shape[] {
  const out = new ShapeList();
  out.box(palette.fill, undefined, 0, 0, scene.width, scene.height);
  paintSelection(scene, palette, out);
  paintGridLines(scene, palette, out);
  paintFrame(scene, palette, out);
  for (const column of scene.elevation.columns) paintZeroWidth(scene, palette, out, column);
  for (const support of scene.elevation.supports) paintCrossingBeam(scene, palette, out, support);
  paintBubbles(scene, palette, out);
  return out.shapes;
}

function paintSelection(scene: Scene, palette: Palette, out: ShapeList): void {
  const column = scene.elevation.columns[scene.selectedColumn];
  if (!column) return;
  const { left, right } = screenSpan(scene, column.extent, 10);
  out.box(palette.selectionFill, undefined, left, scene.selectionTopY, right, scene.gridLineBottomY);
}

function paintGridLines(scene: Scene, palette: Palette, out: ShapeList): void {
  for (const grid of scene.elevation.grids) {
    const x = sx(scene, grid.x);
    if (x < -20 || x > scene.width + 20) continue;
    out.line(palette.gridLine, x, scene.gridLineTopY, x, scene.gridLineBottomY);
  }
}

function paintFrame(scene: Scene, palette: Palette, out: ShapeList): void {
  const elevation = scene.elevation;

  // Spans: each beam's own top and bottom lines, with a step where the next beam of the span sits higher or lower.
  for (const column of elevation.columns) {
    if (column.kind !== 'span') continue;
    if (!isVisible(scene, sx(scene, column.extent.start), sx(scene, column.extent.end))) continue;
    const pieces = beamPieces(elevation, column.extent);
    if (pieces.length === 0) {
      const faces = beamFaces(elevation, column.extent);
      pieces.push({ start: column.extent.start, end: column.extent.end, top: faces.top, bottom: faces.bottom });
    }
    pieces.forEach((piece, i) => {
      const left = sx(scene, piece.start);
      const right = sx(scene, piece.end);
      const top = sy(scene, piece.top);
      const bottom = sy(scene, piece.bottom);
      out.box(palette.beamFill, undefined, left, top, right, bottom);
      out.line(palette.outline, left, top, right, top);
      out.line(palette.outline, left, bottom, right, bottom);
      const previous = pieces[i - 1];
      if (!previous) return;
      if (previous.top !== piece.top) out.line(palette.outline, left, sy(scene, previous.top), left, top);
      if (previous.bottom !== piece.bottom) out.line(palette.outline, left, sy(scene, previous.bottom), left, bottom);
    });
  }

  for (const support of elevation.supports) paintSupport(scene, palette, out, support);
}

function paintSupport(scene: Scene, palette: Palette, out: ShapeList, support: KataWebSupport): void {
  const elevation = scene.elevation;
  let left = sx(scene, support.extent.start);
  let right = sx(scene, support.extent.end);
  if (!isVisible(scene, left - 20, right + 20)) return;
  if (right - left < 2) {
    const m = (left + right) / 2;
    left = m - 1;
    right = m + 1;
  }

  // The spans either side give the beam faces at each edge of the support.
  const columns = elevation.columns;
  const spanLeft = columns
    .slice(0, support.columnIndex)
    .filter((c) => c.kind === 'span' && Math.abs(c.extent.end - support.extent.start) < 10)
    .at(-1);
  const spanRight = columns.slice(support.columnIndex + 1).find((c) => c.kind === 'span' && Math.abs(c.extent.start - support.extent.end) < 10);
  const own = beamFaces(elevation, support.extent);
  const facesLeft = spanLeft ? facesAt(elevation, spanLeft.extent, spanLeft.extent.end) : own;
  const facesRight = spanRight ? facesAt(elevation, spanRight.extent, spanRight.extent.start) : own;

  let topMm = Math.max(spanLeft ? facesLeft.top : -Infinity, spanRight ? facesRight.top : -Infinity);
  if (!Number.isFinite(topMm)) topMm = own.top;
  let bottomMm = Math.min(spanLeft ? facesLeft.bottom : Infinity, spanRight ? facesRight.bottom : Infinity);
  if (!Number.isFinite(bottomMm)) bottomMm = own.bottom;
  const top = sy(scene, topMm);
  const bottom = sy(scene, bottomMm);

  if (support.kind === 'foundation') {
    const footingBottom = bottom + FOOTING_PX;
    const grid = elevation.grids.find((g) => contains(support.extent, g.x, 10));
    const gridX = grid ? sx(scene, grid.x) : (left + right) / 2;

    out.box(palette.supportFill, undefined, left, bottom, right, footingBottom);
    out.box(palette.beamFill, undefined, left, top, right, bottom);
    out.breakLine(palette.breakLine, left, right, footingBottom, 0, gridX);

    // An outer cantilever face runs from the beam top down to the footing bottom.
    out.line(palette.outline, left, spanLeft ? sy(scene, facesLeft.bottom) : top, left, footingBottom);
    out.line(palette.outline, right, footingBottom, right, spanRight ? sy(scene, facesRight.bottom) : top);

    const upper = support.upper;
    if (upper) {
      const upperLeft = sx(scene, upper.extent.start);
      const upperRight = sx(scene, upper.extent.end);
      const upperTop = top - UPPER_STUB_PX;
      out.box(palette.supportFill, undefined, upperLeft, upperTop, upperRight, top);
      out.breakLine(palette.breakLine, upperLeft, upperRight, upperTop, 4.5, gridX);
      out.line(palette.outline, upperLeft, top, upperLeft, upperTop);
      out.line(palette.outline, upperRight, top, upperRight, upperTop);
      if (upperLeft > left) out.line(palette.outline, left, top, upperLeft, top);
      if (upperRight < right) out.line(palette.outline, upperRight, top, right, top);
    } else {
      out.line(palette.outline, left, top, right, top);
    }
    return;
  }

  if (support.kind === 'beam') {
    const sectionBottom = support.sectionHeightMm !== null ? sy(scene, topMm - support.sectionHeightMm) : bottom;
    out.box(palette.supportFill, palette.outline, left, top, right, sectionBottom);
    out.line(palette.outline, left, top, right, sectionBottom);
    out.line(palette.outline, left, sectionBottom, right, top);
    return;
  }

  // A column: stubs over and under the beam, cut by break lines.
  const upperTop = top - UPPER_STUB_PX;
  const lowerBottom = bottom + LOWER_STUB_PX;
  out.box(palette.supportFill, undefined, left, upperTop, right, lowerBottom);
  out.breakLine(palette.breakLine, left, right, upperTop);
  out.breakLine(palette.breakLine, left, right, lowerBottom);
  edge(out, palette, left, upperTop, lowerBottom, spanLeft ? [sy(scene, facesLeft.top), sy(scene, facesLeft.bottom)] : null);
  edge(out, palette, right, upperTop, lowerBottom, spanRight ? [sy(scene, facesRight.top), sy(scene, facesRight.bottom)] : null);
}

/** A column edge: whole when no span meets it, else only the stubs beyond the span's beam faces. */
function edge(out: ShapeList, palette: Palette, x: number, upperTop: number, lowerBottom: number, faces: [number, number] | null): void {
  if (!faces) {
    out.line(palette.outline, x, upperTop, x, lowerBottom);
    return;
  }
  out.line(palette.outline, x, faces[0], x, upperTop);
  out.line(palette.outline, x, faces[1], x, lowerBottom);
}

/** A joint is a tick through the beam; a free end is a vertical end face. */
function paintZeroWidth(scene: Scene, palette: Palette, out: ShapeList, column: KataWebColumn): void {
  if (!isZeroWidth(column.kind)) return;
  const m = mid(column.extent);
  const x = sx(scene, m);
  if (x < -10 || x > scene.width + 10) return;
  const faces = beamFaces(scene.elevation, { start: m - 1, end: m + 1 });
  const top = sy(scene, faces.top);
  const bottom = sy(scene, faces.bottom);
  if (column.kind === 'freeEnd') out.line(palette.outline, x, top, x, bottom);
  else out.line(palette.outline, x, top - 6, x, bottom + 6);
}

function paintCrossingBeam(scene: Scene, palette: Palette, out: ShapeList, support: KataWebSupport): void {
  if (support.crossingBeamX === null) return;
  const x = sx(scene, support.crossingBeamX);
  if (x < -10 || x > scene.width + 10) return;
  const faces = beamFaces(scene.elevation, support.extent);
  out.line(palette.marker, x, sy(scene, faces.top) - 4, x, sy(scene, faces.bottom) + 4);
}

/** Grids written to Kata (row 22) claim their bubble first; the others fill the gaps. */
function paintBubbles(scene: Scene, palette: Palette, out: ShapeList): void {
  const lane = new LabelLane();
  const grids = [...scene.elevation.grids].sort((a, b) => Number(a.offsetText === null) - Number(b.offsetText === null));
  for (const grid of grids) {
    const x = sx(scene, grid.x);
    const name = measure(grid.name, TEXT_SIZE, true);
    const radius = Math.max(BUBBLE_RADIUS, name.width / 2 + 4);
    if (x + radius < 0 || x - radius > scene.width) continue;
    if (!lane.tryPlace(x - radius, x + radius)) continue;
    out.circle(palette.fill, palette.bubble, x, scene.bubbleY, radius);
    out.centered(name, palette.bubbleText, x, scene.bubbleY - name.height / 2);
  }
}
