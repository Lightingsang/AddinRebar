// The numbered circle every label of Kata's column drawing carries, placed in sheet mm.

import { THIN_PX, cadTextCentredOn, lineweight } from '../../kata/elevation/kataCad';
import type { Palette } from '../../kata/elevation/palette';
import type { ShapeList } from '../../kata/elevation/shapes';
import { toScreen, type SheetView } from './columnSheet';
import { LABEL_CIRCLE_MM, TEXT_MM } from './columnStyle';

export function tagCircle(out: ShapeList, view: SheetView, palette: Palette, x: number, y: number, number: number, on = false) {
  const [cx, cy] = toScreen(view, x, y);
  out.circle(undefined, lineweight(on ? palette.selected : palette.kata.circle, THIN_PX, view.scale), cx, cy, LABEL_CIRCLE_MM * view.scale);
  cadTextCentredOn(out, String(number), on ? palette.selected : palette.kata.number, TEXT_MM * 0.9, view.scale, cx, cy, 'centre');
}
