// The annotations of the plain elevation (port of KataElevationAnnotations.cs): column letters (row 11), span captions
// (section, z offset, soffit step), the CAD dimension chains (top chain, detail chain with column face-to-grid
// offsets and clear spans, grid-to-grid chain), upper column texts, grid and crossing-beam offsets.

import type { KataWebRange } from '../kataScene';
import { LabelLane } from './labelLane';
import type { Palette } from './palette';
import { BUBBLE_RADIUS, FOOTING_PX, LOWER_STUB_PX, UPPER_STUB_PX, beamFaces, isVisible, isZeroWidth, sx, sy, type Scene } from './scene';
import { ShapeList, type Shape } from './shapes';
import { SMALL_TEXT_SIZE, TEXT_SIZE, measure, type TextBox } from './text';
import { contains, length, mid } from './viewport';

const isNonZero = (cell: string | null | undefined) => !!cell && cell !== '0';

export function paintAnnotations(scene: Scene, palette: Palette): Shape[] {
  const out = new ShapeList();
  const placeCentered = (lane: LabelLane, box: TextBox, color: string, x: number, top: number) => {
    const left = x - box.width / 2;
    const right = x + box.width / 2;
    if (!isVisible(scene, left, right) || !lane.tryPlace(left, right)) return;
    out.centered(box, color, x, top);
  };
  /** A dimension text centred over a piece of a chain, only when it fits between the ticks. */
  const chainText = (value: string, size: number, left: number, right: number, y: number, room = 4) => {
    if (value.length === 0) return;
    const box = measure(value, size);
    if (box.width + room <= right - left) out.centered(box, palette.dimText, (left + right) / 2, y - box.height - 1.5);
  };

  paintUpperColumns(scene, palette, placeCentered);
  paintGridOffsets(scene, palette, out);
  paintCrossingOffsets(scene, palette, placeCentered);
  paintLetters(scene, palette, out, placeCentered);
  paintTopChain(scene, palette, out, chainText);
  paintDetailChain(scene, palette, out, chainText);
  paintGridChain(scene, palette, out, chainText);
  paintCaptions(scene, palette, placeCentered);
  return out.shapes;
}

type PlaceCentered = (lane: LabelLane, box: TextBox, color: string, x: number, top: number) => void;
type ChainText = (value: string, size: number, left: number, right: number, y: number, room?: number) => void;

const stubTopOver = (scene: Scene, extent: KataWebRange) => sy(scene, beamFaces(scene.elevation, extent).top) - UPPER_STUB_PX;

/**
 * Top chain: column widths and clear spans with witness lines from the upper column stubs; with footings carrying
 * upper columns, it measures the outer cantilever, the upper column widths and the clear spans between them.
 */
function paintTopChain(scene: Scene, palette: Palette, out: ShapeList, chainText: ChainText): void {
  const elevation = scene.elevation;
  const columns = elevation.columns;
  if (columns.length === 0) return;
  const y = scene.topChainY;
  const startStation = columns[0]!.extent.start;
  const endStation = columns[columns.length - 1]!.extent.end;
  out.line(palette.dimension, sx(scene, startStation), y, sx(scene, endStation), y);

  if (!elevation.supports.some((s) => s.kind === 'foundation' && s.upper)) {
    for (const column of columns) {
      if (isZeroWidth(column.kind)) continue;
      const left = sx(scene, column.extent.start);
      const right = sx(scene, column.extent.end);
      if (!isVisible(scene, left - 5, right + 5)) continue;
      out.tick(palette.dimension, left, y);
      out.tick(palette.dimension, right, y);
      if (column.kind === 'support') {
        const stubTop = stubTopOver(scene, column.extent);
        out.line(palette.dimension, left, stubTop - 2, left, y - 3);
        out.line(palette.dimension, right, stubTop - 2, right, y - 3);
      }
      chainText(column.row11, SMALL_TEXT_SIZE, left, right, y);
    }
    return;
  }

  const intervals: { extent: KataWebRange; stubTop: number | null }[] = [];
  let current = startStation;
  const gapTo = (start: number) => {
    if (start > current + 0.5) intervals.push({ extent: { start: current, end: start }, stubTop: null });
  };
  for (const s of [...elevation.supports].sort((a, b) => a.extent.start - b.extent.start)) {
    if (s.kind === 'foundation' && s.upper) {
      gapTo(s.upper.extent.start);
      intervals.push({ extent: s.upper.extent, stubTop: stubTopOver(scene, s.extent) });
      current = s.upper.extent.end;
    } else if (s.kind !== 'foundation') {
      gapTo(s.extent.start);
      intervals.push({ extent: s.extent, stubTop: stubTopOver(scene, s.extent) });
      current = s.extent.end;
    } else {
      gapTo(s.extent.start);
      intervals.push({ extent: s.extent, stubTop: null });
      current = s.extent.end;
    }
  }
  gapTo(endStation);

  intervals.forEach((interval, i) => {
    const left = sx(scene, interval.extent.start);
    const right = sx(scene, interval.extent.end);
    if (!isVisible(scene, left - 5, right + 5)) return;
    out.tick(palette.dimension, left, y);
    out.tick(palette.dimension, right, y);
    const beamTop = sy(scene, beamFaces(elevation, interval.extent).top);
    if (i === 0) out.line(palette.dimension, left, beamTop - 2, left, y - 3);
    if (interval.stubTop !== null) {
      out.line(palette.dimension, left, interval.stubTop - 2, left, y - 3);
      out.line(palette.dimension, right, interval.stubTop - 2, right, y - 3);
    }
    if (i === intervals.length - 1) out.line(palette.dimension, right, beamTop - 2, right, y - 3);
    const lengthMm = Math.round(length(interval.extent));
    if (lengthMm > 0) chainText(String(lengthMm), SMALL_TEXT_SIZE, left, right, y);
  });
}

function paintUpperColumns(scene: Scene, palette: Palette, placeCentered: PlaceCentered): void {
  const lane = new LabelLane();
  for (const support of scene.elevation.supports) {
    if (support.kind === 'foundation' || !support.upper || support.upper.text.length === 0) continue;
    const box = measure(support.upper.text, SMALL_TEXT_SIZE);
    placeCentered(lane, box, palette.text, sx(scene, mid(support.upper.extent)), stubTopOver(scene, support.extent) - box.height - 2);
  }
}

function paintGridOffsets(scene: Scene, palette: Palette, out: ShapeList): void {
  const lane = new LabelLane();
  for (const grid of scene.elevation.grids) {
    if (!isNonZero(grid.offsetText)) continue;
    const box = measure(`lệch ${grid.offsetText}`, SMALL_TEXT_SIZE);
    const left = sx(scene, grid.x) + BUBBLE_RADIUS + 4;
    if (!isVisible(scene, left, left + box.width) || !lane.tryPlace(left, left + box.width)) continue;
    out.at(box, palette.accent, left, scene.bubbleY - box.height / 2);
  }
}

function paintCrossingOffsets(scene: Scene, palette: Palette, placeCentered: PlaceCentered): void {
  const lane = new LabelLane();
  for (const support of scene.elevation.supports) {
    if (support.crossingBeamX === null) continue;
    const row21 = scene.elevation.columns[support.columnIndex]?.row21;
    if (!isNonZero(row21)) continue;
    placeCentered(lane, measure(`giao ${row21}`, SMALL_TEXT_SIZE), palette.mutedText, sx(scene, support.crossingBeamX), scene.markerTextY);
  }
}

/** The selected column's letter is always shown; its neighbours give way to it. */
function paintLetters(scene: Scene, palette: Palette, out: ShapeList, placeCentered: PlaceCentered): void {
  const columns = scene.elevation.columns;
  let reservedLeft = NaN;
  let reservedRight = NaN;
  const selected = columns[scene.selectedColumn];
  if (selected) {
    const box = measure(selected.letter, TEXT_SIZE, true);
    const x = sx(scene, mid(selected.extent));
    reservedLeft = x - box.width / 2 - 4;
    reservedRight = x + box.width / 2 + 4;
    out.centered(box, palette.selected, x, scene.letterY - 1);
  }

  const lane = new LabelLane();
  for (const column of columns) {
    if (column.index === scene.selectedColumn) continue;
    const box = measure(column.letter, SMALL_TEXT_SIZE);
    const x = sx(scene, mid(column.extent));
    if (x + box.width / 2 > reservedLeft && x - box.width / 2 < reservedRight) continue;
    placeCentered(lane, box, palette.mutedText, x, scene.letterY);
  }
}

/** Detail chain: column face-to-grid distances either side of the grid, and clear spans between columns. */
function paintDetailChain(scene: Scene, palette: Palette, out: ShapeList, chainText: ChainText): void {
  const elevation = scene.elevation;
  const columns = elevation.columns;
  if (columns.length === 0) return;
  const y = scene.chainY;
  out.line(palette.dimension, sx(scene, columns[0]!.extent.start), y, sx(scene, columns[columns.length - 1]!.extent.end), y);

  for (const column of columns) {
    if (column.kind !== 'span' && column.kind !== 'support') continue;
    const left = sx(scene, column.extent.start);
    const right = sx(scene, column.extent.end);
    if (!isVisible(scene, left - 5, right + 5)) continue;

    if (column.kind === 'span') {
      out.tick(palette.dimension, left, y);
      out.tick(palette.dimension, right, y);
      chainText(column.row11, SMALL_TEXT_SIZE, left, right, y);
      continue;
    }

    const bottom = sy(scene, beamFaces(elevation, column.extent).bottom);
    const support = elevation.supports.find((s) => s.columnIndex === column.index);
    const stubBottom = bottom + (support?.kind === 'foundation' ? FOOTING_PX : LOWER_STUB_PX);
    out.line(palette.dimension, left, stubBottom + 2, left, y + 3);
    out.line(palette.dimension, right, stubBottom + 2, right, y + 3);
    out.tick(palette.dimension, left, y);
    out.tick(palette.dimension, right, y);

    const grid = elevation.grids.find((g) => contains(column.extent, g.x, 0.5));
    if (grid) {
      const gx = sx(scene, grid.x);
      out.tick(palette.dimension, gx, y);
      const dLeft = Math.round(grid.x - column.extent.start);
      if (dLeft > 0.5) chainText(String(dLeft), SMALL_TEXT_SIZE, left, gx, y, 2);
      const dRight = Math.round(column.extent.end - grid.x);
      if (dRight > 0.5) chainText(String(dRight), SMALL_TEXT_SIZE, gx, right, y, 2);
    } else if (!isZeroWidth(column.kind)) {
      chainText(column.row11, SMALL_TEXT_SIZE, left, right, y, 2);
    }
  }
}

/** Grid-to-grid chain: centre-to-centre distances between consecutive grid lines. */
function paintGridChain(scene: Scene, palette: Palette, out: ShapeList, chainText: ChainText): void {
  const dimensions = scene.elevation.gridDimensions;
  if (dimensions.length === 0) return;
  const y = scene.gridChainY;
  out.line(palette.dimension, sx(scene, dimensions[0]!.extent.start), y, sx(scene, dimensions[dimensions.length - 1]!.extent.end), y);
  for (const dimension of dimensions) {
    const left = sx(scene, dimension.extent.start);
    const right = sx(scene, dimension.extent.end);
    if (!isVisible(scene, left - 5, right + 5)) continue;
    out.tick(palette.dimension, left, y);
    out.tick(palette.dimension, right, y);
    chainText(dimension.text, TEXT_SIZE, left, right, y);
  }
}

/** "Nhịp n – b x h" above each span, then its z offset and soffit step when not zero. */
function paintCaptions(scene: Scene, palette: Palette, placeCentered: PlaceCentered): void {
  const titles = new LabelLane();
  const details = new LabelLane();
  for (const column of scene.elevation.columns) {
    if (column.kind !== 'span') continue;
    const left = sx(scene, column.extent.start);
    const right = sx(scene, column.extent.end);
    if (!isVisible(scene, left, right)) continue;
    const m = (left + right) / 2;
    const captionY = sy(scene, beamFaces(scene.elevation, column.extent).top) - 15;

    let title = measure(`Nhịp ${column.spanNumber ?? ''} – ${column.spanSection ?? ''}`, SMALL_TEXT_SIZE, true);
    if (title.width + 4 > right - left) title = measure(String(column.spanNumber ?? ''), SMALL_TEXT_SIZE, true);
    placeCentered(titles, title, palette.text, m, captionY);

    const parts: string[] = [];
    if (isNonZero(column.row19)) parts.push(`z ${column.row19}`);
    if (isNonZero(column.row21)) parts.push(`đáy ${column.row21}`);
    if (parts.length === 0) continue;
    const detail = measure(parts.join(' · '), SMALL_TEXT_SIZE);
    if (detail.width + 4 <= right - left) placeCentered(details, detail, palette.mutedText, m, captionY - title.height - 1);
  }
}
