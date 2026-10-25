// What the page draws over Kata's elevation for picking (port of KataElevationCanvas.PaintBarSelection, plus the
// hover the WPF canvas did not have): every line of the selected group in the accent colour with how to delete it,
// and the group under the cursor in a lighter accent.

import type { KataWebLine } from '../kataScene';
import { BAR_PX, lineweight } from './kataCad';
import type { Point } from './kataElevationPainter';
import type { Palette } from './palette';
import { ShapeList, type Shape } from './shapes';
import { SMALL_TEXT_SIZE, measure } from './text';

function groupLines(out: ShapeList, lines: KataWebLine[], keys: readonly string[], point: (x: number, z: number) => Point, color: string, scale: number): void {
  const selected = new Set(keys);
  const pen = lineweight(color, BAR_PX + 2, scale);
  for (const line of lines) {
    if (line.keys.some((k) => selected.has(k))) out.poly(undefined, pen, line.points.map((q) => point(q.x, q.z)));
  }
}

export function paintPicking(
  lines: KataWebLine[],
  point: (x: number, z: number) => Point,
  palette: Palette,
  scale: number,
  height: number,
  selected: readonly string[] | null,
  hovered: readonly string[] | null,
): Shape[] {
  const out = new ShapeList();
  if (hovered && hovered !== selected) groupLines(out, lines, hovered, point, `${palette.accent}80`, scale);
  if (!selected) return out.shapes;
  groupLines(out, lines, selected, point, palette.accent, scale);
  const hint = measure(`Đã chọn ${selected.length} thanh/vùng đai — Delete: xóa · Ctrl+Z: hoàn tác · Esc: bỏ chọn`, SMALL_TEXT_SIZE);
  out.box(palette.fill, undefined, 6, height - hint.height - 10, 14 + hint.width, height - 4);
  out.at(hint, palette.accent, 10, height - hint.height - 7);
  return out.shapes;
}
