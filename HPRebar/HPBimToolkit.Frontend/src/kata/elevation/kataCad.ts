// What Kata's drawing is made of, on screen (ports of KataCadPens.cs, KataCadText.cs and the constants of
// HPRebar.Core KataDrawingStyle / KataTagStyle): lineweights in whole device pixels at any zoom as AutoCAD shows them
// with LWDISPLAY on, linetypes scaled as drawn, kata_text (Arial, width factor 0.8) at a height in model millimetres.

import type { Pen } from './palette';
import type { ShapeList } from './shapes';
import { measure } from './text';

/** KataDrawingStyle (mm). */
export const STYLE = {
  dimTextHeight: 62.5,
  dimTextGap: 15.625,
  dimTickSize: 37.5,
  dimTickWidth: 0.15 * 37.5,
  extensionLength: 93.75,
  extensionBeyond: 37.5,
  dimLineBeyond: 30,
  runArrowLength: 67.5,
  runTickHalf: 18.75,
  bubbleRadius: 125,
  bubbleTextHeight: 125,
  bubbleTickEnd: 175,
  titleTextHeight: 125,
  titleNameLift: 36.8,
  titleScaleDrop: 100,
  charWidthRatio: 0.68,
  hiddenDashes: [6.35 * 18.75, 3.175 * 18.75],
  centerDashes: [31.75 * 12.5, 6.35 * 12.5, 6.35 * 12.5, 6.35 * 12.5],
} as const;

/** KataTagStyle (mm). */
export const TAG = {
  textHeight: 62.5,
  circleRadius: 62.5,
  textGapRight: 21.25,
  textGapLeft: 12.5,
  textLift: 21.14,
  spacingDrop: 74.31,
  stirrupShift: 125,
  leaderPerChar: 48,
  arrowSize: 37.5,
} as const;

/** 0.35 (bars, stirrups) → 3 px, 0.20 (concrete seen) → 2 px, thinner → 1 px. */
export const BAR_PX = 3;
export const OUTLINE_PX = 2;
export const THIN_PX = 1;

const devicePixels = () => (typeof window === 'undefined' ? 1 : window.devicePixelRatio || 1);

/** A pen `px` device pixels wide, dashed by `dashesMm` at `scale` px/mm (solid once its dashes shrink under 2 px). */
export function lineweight(color: string, px: number, scale: number, dashesMm?: readonly number[]): Pen {
  const pen: Pen = { color, width: px / devicePixels(), cad: true };
  if (dashesMm && Math.min(...dashesMm) * scale >= 2) pen.dash = dashesMm.map((d) => +(d * scale).toFixed(2)).join(' ');
  return pen;
}

const WIDTH_FACTOR = 0.8;
const MIN_FONT_PX = 1.5;
/** Arial's capital height as a fraction of its font size: AutoCAD's text height is the height of the capitals. */
const ARIAL_CAPS = 0.716;
const ARIAL = 'Arial, sans-serif';

export type Align = 'left' | 'centre' | 'right';

/** kata_text at `heightMm` scaled by `scale`; returns the width drawn (px), 0 when too small to draw. */
export function cadText(out: ShapeList, value: string, color: string, heightMm: number, scale: number, x: number, baselineY: number, align: Align, vertical = false): number {
  if (!value) return 0;
  const size = (heightMm * scale) / ARIAL_CAPS;
  if (size < MIN_FONT_PX) return 0;
  out.shapes.push({
    t: 'ctext',
    x,
    y: baselineY,
    text: value,
    size,
    color,
    anchor: align === 'left' ? 'start' : align === 'centre' ? 'middle' : 'end',
    vertical,
    widthFactor: WIDTH_FACTOR,
  });
  return measure(value, size, false, ARIAL).width * WIDTH_FACTOR;
}

/** kata_text with the middle of its capitals on `middleY`. */
export const cadTextCentredOn = (out: ShapeList, value: string, color: string, heightMm: number, scale: number, x: number, middleY: number, align: Align) =>
  cadText(out, value, color, heightMm, scale, x, middleY + (heightMm * scale) / 2, align);

/** A closed filled arrowhead (AutoCAD's default, 3:1) with its tip at (x, y), pointing away from (fromX, fromY). */
export function arrow(out: ShapeList, fill: string, x: number, y: number, fromX: number, fromY: number, length: number): void {
  let dx = fromX - x;
  let dy = fromY - y;
  const l = Math.hypot(dx, dy);
  if (l < 1e-9 || length < 0.5) return;
  dx /= l;
  dy /= l;
  const bx = x + dx * length;
  const by = y + dy * length;
  const half = length / 6;
  out.poly(fill, undefined, [[x, y], [bx - dy * half, by + dx * half], [bx + dy * half, by - dx * half]], true);
}
