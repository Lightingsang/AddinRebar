// Kata's dimensions in model millimetres (port of KataCadDimPainter.cs). kata_dim_25: extension lines of fixed length
// from the dimension line and a little past it, the line running past them, a heavy 45° arch tick at each end, the
// value above the line (left of it, reading upwards, when vertical). kata_rai_thep: a magenta line with a run arrow and
// a cross stroke at each end, no text.

import type { KataWebDim } from '../kataScene';
import { STYLE, cadText } from './kataCad';
import type { Palette, Pen } from './palette';
import type { ShapeList } from './shapes';

type Point = [number, number];
const RUN_ARROW_HALF_WIDTH = 12.5;

export function paintDims(out: ShapeList, dims: KataWebDim[], palette: Palette, point: (x: number, z: number) => Point, scale: number, line: Pen, run: Pen): void {
  const tick: Pen = { color: palette.kata.grey, width: Math.max(line.width, STYLE.dimTickWidth * scale), cad: true };
  const tickAt = ([x, y]: Point, vertical: boolean) => {
    const h = (STYLE.dimTickSize * scale) / 2;
    const lean = vertical ? -h : h;
    out.line(tick, x - h, y + lean, x + h, y - lean);
  };
  /** The extension line at `onLine` toward its origin: a fixed length on the origin's side, a little past the line. */
  const extension = (onLine: Point, origin: Point, vertical: boolean) => {
    const toward = vertical ? origin[1] - onLine[1] : origin[0] - onLine[0];
    if (Math.abs(toward) < 0.5) return;
    const sign = Math.sign(toward);
    const near = Math.min(Math.abs(toward), STYLE.extensionLength * scale) * sign;
    const far = -STYLE.extensionBeyond * scale * sign;
    if (vertical) out.line(line, onLine[0], onLine[1] + far, onLine[0], onLine[1] + near);
    else out.line(line, onLine[0] + far, onLine[1], onLine[0] + near, onLine[1]);
  };
  const beyond = STYLE.dimLineBeyond * scale;

  for (const dim of dims) {
    if (dim.style === 'run') {
      const a = point(dim.x1, dim.lineAt);
      const b = point(dim.x2, dim.lineAt);
      out.line(run, a[0], a[1], b[0], b[1]);
      runEnd(out, palette, run, a, b, scale);
      runEnd(out, palette, run, b, a, scale);
    } else if (dim.vertical) {
      const a = point(dim.lineAt, dim.z1);
      const b = point(dim.lineAt, dim.z2);
      extension(a, point(dim.x1, dim.z1), false);
      extension(b, point(dim.x2, dim.z2), false);
      out.line(line, a[0], Math.min(a[1], b[1]) - beyond, a[0], Math.max(a[1], b[1]) + beyond);
      tickAt(a, true);
      tickAt(b, true);
      cadText(out, dim.text, palette.kata.tagText, STYLE.dimTextHeight, scale, a[0] - STYLE.dimTextGap * scale, (a[1] + b[1]) / 2, 'centre', true);
    } else {
      const a = point(dim.x1, dim.lineAt);
      const b = point(dim.x2, dim.lineAt);
      extension(a, point(dim.x1, dim.z1), true);
      extension(b, point(dim.x2, dim.z2), true);
      out.line(line, Math.min(a[0], b[0]) - beyond, a[1], Math.max(a[0], b[0]) + beyond, a[1]);
      tickAt(a, false);
      tickAt(b, false);
      cadText(out, dim.text, palette.kata.tagText, STYLE.dimTextHeight, scale, (a[0] + b[0]) / 2, a[1] - STYLE.dimTextGap * scale, 'centre');
    }
  }
}

/** One end of a stirrup run: a cross stroke and an arrow pointing out along the run. */
function runEnd(out: ShapeList, palette: Palette, run: Pen, end: Point, other: Point, scale: number): void {
  const dir = Math.sign(other[0] - end[0]);
  if (dir === 0) return;
  const length = STYLE.runArrowLength * scale;
  const cross = STYLE.runTickHalf * scale;
  const half = RUN_ARROW_HALF_WIDTH * scale;
  out.line(run, end[0], end[1] - cross, end[0], end[1] + cross);
  out.poly(palette.kata.run, undefined, [end, [end[0] + dir * length, end[1] - half], [end[0] + dir * length, end[1] + half]], true);
}
