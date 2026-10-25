// What the painters produce: plain shapes in screen pixels, rendered by ShapesSvg. Keeping the painters pure (data in,
// shapes out) lets the tests check what is drawn without a browser. Helpers port KataDrawPrimitives.cs.

import type { Pen } from './palette';
import type { TextBox } from './text';

export type Shape =
  | { t: 'line'; x1: number; y1: number; x2: number; y2: number; pen: Pen }
  | { t: 'rect'; x1: number; y1: number; x2: number; y2: number; fill?: string; pen?: Pen }
  | { t: 'circle'; x: number; y: number; r: number; fill?: string; pen: Pen }
  | { t: 'text'; x: number; top: number; box: TextBox; color: string; anchor: 'start' | 'middle' }
  /** `id` names what a polyline is (a bar group's line, a flag) for tests and harnesses: rendered as data-id. */
  | { t: 'poly'; points: [number, number][]; pen?: Pen; fill?: string; closed: boolean; id?: string }
  /** An SVG path (Kata's section polylines, whose bulges are exact arcs). */
  | { t: 'path'; d: string; pen?: Pen; fill?: string }
  /** CAD text (kata_text): baseline through (x, y), font size in px, glyphs squeezed to `widthFactor`. */
  | { t: 'ctext'; x: number; y: number; text: string; size: number; color: string; anchor: 'start' | 'middle' | 'end'; vertical: boolean; widthFactor: number };

export class ShapeList {
  readonly shapes: Shape[] = [];

  line(pen: Pen, x1: number, y1: number, x2: number, y2: number): void {
    this.shapes.push({ t: 'line', x1, y1, x2, y2, pen });
  }

  box(fill: string | undefined, pen: Pen | undefined, x1: number, y1: number, x2: number, y2: number): void {
    this.shapes.push({ t: 'rect', x1, y1, x2, y2, fill, pen });
  }

  poly(fill: string | undefined, pen: Pen | undefined, points: [number, number][], closed = false, id?: string): void {
    if (points.length >= 2) this.shapes.push({ t: 'poly', points, pen, fill, closed: closed || fill !== undefined, id });
  }

  circle(fill: string | undefined, pen: Pen, x: number, y: number, r: number): void {
    this.shapes.push({ t: 'circle', x, y, r, fill, pen });
  }

  /** `box` centred on `x` with its top at `top`. */
  centered(box: TextBox, color: string, x: number, top: number): void {
    this.shapes.push({ t: 'text', x, top, box, color, anchor: 'middle' });
  }

  at(box: TextBox, color: string, left: number, top: number): void {
    this.shapes.push({ t: 'text', x: left, top, box, color, anchor: 'start' });
  }

  /** A dimension tick (CAD architectural 45-degree slash) at `x` on the line at `y`. */
  tick(pen: Pen, x: number, y: number): void {
    this.line(pen, x - 3, y + 3, x + 3, y - 3);
  }

  /** A horizontal break line (zigzag cut mark) from x1 to x2 at y, protruding past the edges by `overhang`. */
  breakLine(pen: Pen, x1: number, x2: number, y: number, overhang = 4.5, centre?: number): void {
    if (x2 < x1) [x1, x2] = [x2, x1];
    const left = x1 - overhang;
    const right = x2 + overhang;
    const width = right - left;
    if (width < 8) {
      this.line(pen, left, y, right, y);
      return;
    }
    const d = Math.min(4, width / 4);
    const m = centre !== undefined && centre - d >= left && centre + d <= right ? centre : (left + right) / 2;
    const h = 4.5;
    this.line(pen, left, y, m - d, y);
    this.line(pen, m - d, y, m - d / 3, y - h);
    this.line(pen, m - d / 3, y - h, m + d / 3, y + h);
    this.line(pen, m + d / 3, y + h, m + d, y);
    this.line(pen, m + d, y, right, y);
  }
}
