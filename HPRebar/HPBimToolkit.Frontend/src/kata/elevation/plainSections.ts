// The section cuts on the plain elevation (port of the cut markers and SelectedCut of KataElevationSectionPainter.cs):
// every cut marked with its number, the cut of the picked column — a span's middle cut, a support's nearest one —
// shown in the card in the canvas corner. The card shows Kata's own section of that cut (the WPF card drew a schematic
// one; the page draws Kata's drawing, the one the Kata panel shows).

import type { KataWebCut, KataWebStationMap } from '../kataScene';
import type { Palette } from './palette';
import { beamFaces, isVisible, sx, sy, type Scene } from './scene';
import { ShapeList, type Shape } from './shapes';
import { SMALL_TEXT_SIZE, measure } from './text';

export const CARD = { width: 220, height: 270, margin: 12 } as const;

/** Whether a drawing area this size has room for the card. */
export const cardFits = (width: number, height: number) => width >= 420 && height >= 290;

/** Width the card takes off the right of the drawing area (0 when it does not fit). */
export const cardReserve = (width: number, height: number) => (cardFits(width, height) ? CARD.width + 2 * CARD.margin : 0);

/** The cut of the selected column: a span's middle one, else the one nearest the column; the first span when none is picked. */
export function selectedCut(scene: Scene, cuts: KataWebCut[], map: KataWebStationMap): KataWebCut | null {
  if (cuts.length === 0) return null;
  const local = (station: number) => (station - map.origin) * map.direction;
  const columns = scene.elevation.columns;
  let index = scene.selectedColumn;
  if (index < 0 || index >= columns.length) index = columns.findIndex((c) => c.kind === 'span');
  const column = columns[index];
  if (!column) return null;

  const a = local(column.extent.start);
  const b = local(column.extent.end);
  if (column.kind === 'span') {
    const inSpan = cuts.filter((c) => c.x >= Math.min(a, b) - 1 && c.x <= Math.max(a, b) + 1);
    if (inSpan.length > 0) return inSpan[Math.floor(inSpan.length / 2)]!;
  }
  const centre = (a + b) / 2;
  return [...cuts].sort((p, q) => Math.abs(p.x - centre) - Math.abs(q.x - centre))[0]!;
}

/** Each cut on the elevation: a short stroke above and below the beam, its number under the beam. */
export function paintCutMarkers(scene: Scene, palette: Palette, cuts: KataWebCut[], map: KataWebStationMap, selected: KataWebCut | null): Shape[] {
  const out = new ShapeList();
  for (const cut of cuts) {
    const station = map.origin + map.direction * cut.x;
    const x = sx(scene, station);
    if (!isVisible(scene, x - 6, x + 6)) continue;
    const faces = beamFaces(scene.elevation, { start: station, end: station });
    const top = sy(scene, faces.top);
    const bottom = sy(scene, faces.bottom);
    const isSelected = cut === selected;
    const pen = isSelected ? palette.marker : palette.dimension;
    if (isSelected) out.line(palette.marker, x, top, x, bottom);
    out.line(pen, x, top - 14, x, top - 4);
    out.line(pen, x, bottom + 4, x, bottom + 14);
    const text = measure(String(cut.number), SMALL_TEXT_SIZE - 1, isSelected);
    out.at(text, isSelected ? palette.accent : palette.dimText, x + 2, Math.max(bottom + 6, scene.bandBottomY + 2));
  }
  return out.shapes;
}
