// Kata's elevation of the rebar plan while the bars are shown (port of KataElevationCadPainter.cs), in model
// millimetres scaled with the view like CAD: Kata's lineweights in whole pixels, its HIDDEN / CENTER linetypes,
// dimensions (kataDimPainter), section flags, grid bubbles, level mark and title. The bar tags are kataTagPainter's.

import type { KataDrawingPen, KataWebBubble, KataWebFlag, KataWebLevel, KataWebRebar, KataWebStationMap, KataWebTitle } from '../kataScene';
import { BAR_PX, OUTLINE_PX, STYLE, TAG, THIN_PX, cadText, cadTextCentredOn, lineweight } from './kataCad';
import { paintDims } from './kataDimPainter';
import type { Palette, Pen } from './palette';
import { isVisible, screenSpan, sx, sy, type Scene } from './scene';
import { ShapeList, type Shape } from './shapes';

// kata_block_SECBAL at scale 25: stem −25..108.9, flag (0, 69.45)–(147.3, 108.9), number right-aligned 12.5 left of
// the stem, its middle 43.75 up.
export const FLAG = { stemBottom: -25, top: 108.9, slopeStart: 69.45, width: 147.3, textGap: 12.5, textMiddle: 43.75 } as const;

export type Point = [number, number];

/** Screen point of a drawing point (mm along the run in the layout's local X, mm up from the beam top). */
export const kataPoint = (scene: Scene, map: KataWebStationMap) => (x: number, z: number): Point => [
  sx(scene, map.origin + map.direction * x),
  sy(scene, scene.elevation.topMm + z),
];

/**
 * @param selectedFlag Number of the section shown beside the elevation; its flags are drawn in the accent colour.
 */
export function paintKataElevation(scene: Scene, palette: Palette, rebar: KataWebRebar, map: KataWebStationMap, selectedFlag: number | null = null): Shape[] {
  const out = new ShapeList();
  const scale = scene.viewport.scale;
  const p = kataPoint(scene, map);
  const k = palette.kata;
  out.box(palette.fill, undefined, 0, 0, scene.width, scene.height);

  const column = scene.elevation.columns[scene.selectedColumn];
  if (column) {
    const { left, right } = screenSpan(scene, column.extent, 10);
    out.box(palette.selectionFill, undefined, left, p(0, rebar.top)[1], right, p(0, rebar.bottom)[1]);
  }

  const pens: Record<KataDrawingPen, Pen> = {
    outline: lineweight(k.outline, OUTLINE_PX, scale),
    hidden: lineweight(k.hidden, THIN_PX, scale, STYLE.hiddenDashes),
    grid: lineweight(k.grey, THIN_PX, scale, STYLE.centerDashes),
    thin: lineweight(k.grey, THIN_PX, scale),
    stirrup: lineweight(k.stirrup, BAR_PX, scale),
    bar: lineweight(k.bar, BAR_PX, scale),
  };

  // Bars last, over the stirrups and the concrete, as Kata's layer order shows them.
  const ordered = rebar.lines.map((line, index) => ({ line, index })).sort((a, b) => Number(a.line.pen === 'bar') - Number(b.line.pen === 'bar'));
  for (const { line, index } of ordered) {
    out.poly(undefined, pens[line.pen], line.points.map((q) => p(q.x, q.z)), false, line.keys.length > 0 ? `line-${index}` : undefined);
  }

  paintDims(out, rebar.dims, palette, p, scale, pens.thin, lineweight(k.run, THIN_PX, scale));
  for (const flag of rebar.flags) paintFlag(out, scene, palette, p(flag.x, flag.z), flag, scale, flag.number === selectedFlag);
  for (const bubble of rebar.bubbles) paintBubble(out, scene, palette, p(bubble.x, bubble.z), bubble, pens.thin, scale);
  if (rebar.level) paintLevel(out, palette, p(rebar.level.x, rebar.level.z), rebar.level, pens.thin, scale);
  if (rebar.title) paintTitle(out, palette, p(rebar.title.x, rebar.title.z), rebar.title, scale);
  return out.shapes;
}

/** kata_block_SECBAL: a stem with a filled flag at its top (mirrored upside down under the beam), the cut number before it. */
function paintFlag(out: ShapeList, scene: Scene, palette: Palette, [x, y]: Point, flag: KataWebFlag, s: number, selected: boolean): void {
  if (!isVisible(scene, x - FLAG.width * s, x + FLAG.width * s)) return;
  const up = flag.below ? 1 : -1;
  const color = selected ? palette.accent : palette.kata.flag;
  out.line(lineweight(color, selected ? OUTLINE_PX : THIN_PX, s), x, y + up * FLAG.stemBottom * s, x, y + up * FLAG.top * s);
  out.poly(color, undefined, [[x, y + up * FLAG.slopeStart * s], [x + FLAG.width * s, y + up * FLAG.top * s], [x, y + up * FLAG.top * s]], true, `flag-${flag.number}`);
  const middle = y + up * FLAG.textMiddle * s;
  cadText(out, String(flag.number), color, TAG.textHeight, s, x - FLAG.textGap * s, middle + (TAG.textHeight * s) / 2, 'right');
}

/** kata_block_GRID: a circle, four ticks out to 175, the grid's name in the middle. */
function paintBubble(out: ShapeList, scene: Scene, palette: Palette, [x, y]: Point, bubble: KataWebBubble, pen: Pen, s: number): void {
  const r = STYLE.bubbleRadius * s;
  const t = STYLE.bubbleTickEnd * s;
  if (!isVisible(scene, x - t, x + t)) return;
  out.circle(undefined, pen, x, y, r);
  out.line(pen, x + r, y, x + t, y);
  out.line(pen, x - r, y, x - t, y);
  out.line(pen, x, y + r, x, y + t);
  out.line(pen, x, y - r, x, y - t);
  cadTextCentredOn(out, bubble.name, palette.kata.number, STYLE.bubbleTextHeight, s, x, y, 'centre');
}

/**
 * kata_block_CT at scale 25: a triangle on the beam top (left half solid, right half hatched), a stem up from its tip,
 * two lines across, the level written over the upper one.
 */
function paintLevel(out: ShapeList, palette: Palette, [ox, oy]: Point, level: KataWebLevel, pen: Pen, s: number): void {
  const at = (x: number, y: number): Point => [ox + x * s, oy - y * s];
  out.line(pen, ox, oy, ox, oy - 167.4 * s);
  out.poly(palette.kata.grey, pen, [at(0, 0), at(-40.3, 39.9), at(0, 39.9)], true);
  out.poly(undefined, pen, [at(0, 0), at(40.3, 39.9), at(0, 39.9)], true);
  // ANSI31: 45° hatch lines, parallel to the right half's slope.
  for (let c = 13.3; c < 39.9; c += 13.3) out.line(pen, ...at(0, c), ...at(39.9 - c, 39.9));
  out.line(pen, ...at(-39.8, 78.5), ...at(286.7, 78.5));
  out.line(pen, at(-41, 0)[0], oy, at(289, 0)[0], oy);
  cadText(out, level.text, palette.kata.tagText, TAG.textHeight, s, ...at(35.25, 101.7), 'left');
}

/** kata_block_TD inserted at `o`: the name underlined, the scale under it. */
export function paintTitle(out: ShapeList, palette: Palette, [x, y]: Point, title: KataWebTitle, s: number): void {
  const baseline = y - STYLE.titleNameLift * s;
  const width = cadText(out, title.name, palette.kata.number, STYLE.titleTextHeight, s, x, baseline, 'centre');
  if (width > 0) {
    const underline = baseline + STYLE.titleTextHeight * 0.2 * s;
    out.line(lineweight(palette.kata.number, THIN_PX, s), x - width / 2, underline, x + width / 2, underline);
  }
  cadText(out, title.scale, palette.kata.tagText, TAG.textHeight, s, x, y + STYLE.titleScaleDrop * s, 'centre');
}
