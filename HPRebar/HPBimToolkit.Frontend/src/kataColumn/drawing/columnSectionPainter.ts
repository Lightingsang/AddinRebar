// The storeys' sections of Kata's column drawing, as VC draws them at 1:20 beside the 1:30 elevation: concrete, hoop,
// inner ties, bars (pick targets) with their numbers once legible, overall sizes and the "1-1" title.

import { BAR_PX, OUTLINE_PX, THIN_PX, cadText, cadTextCentredOn, lineweight, STYLE } from '../../kata/elevation/kataCad';
import type { Palette } from '../../kata/elevation/palette';
import type { ShapeList } from '../../kata/elevation/shapes';
import { SECTION_FACTOR, sectionPoint, toScreen, type PlacedSection, type SheetView } from './columnSheet';
import { NUMBER_MIN_PX, TEXT_MM, TITLE_MM, mm, type BarHit } from './columnStyle';

const P = (view: SheetView, x: number, y: number) => toScreen(view, x, y);

export function paintSection(out: ShapeList, hits: BarHit[], s: PlacedSection, view: SheetView, palette: Palette, highlight: number | null) {
  const d = s.drawing;
  const at = (x: number, y: number) => P(view, ...sectionPoint(s, x, y));
  const k = SECTION_FACTOR * view.scale;
  out.layer = 'concrete';
  const concrete = lineweight(palette.kata.outline, OUTLINE_PX, view.scale);
  const stirrup = lineweight(palette.kata.stirrup, BAR_PX - 1, view.scale);
  if (d.isRound) {
    const [cx, cy] = at(d.widthMm / 2, d.widthMm / 2);
    out.circle(undefined, concrete, cx, cy, (d.widthMm / 2) * k);
    out.layer = 'stirrups';
    out.circle(undefined, stirrup, cx, cy, (d.widthMm / 2 - d.hoopOffsetMm) * k);
  } else {
    out.poly(undefined, concrete, [at(0, 0), at(d.widthMm, 0), at(d.widthMm, d.depthMm), at(0, d.depthMm)], true);
    out.layer = 'stirrups';
    const o = d.hoopOffsetMm;
    out.poly(undefined, stirrup, [at(o, o), at(d.widthMm - o, o), at(d.widthMm - o, d.depthMm - o), at(o, d.depthMm - o)], true);
  }

  for (const tie of d.ties) {
    const pen = lineweight(highlight === tie.number ? palette.selected : palette.kata.stirrup, THIN_PX + 1, view.scale);
    if (tie.kind === 'closed') out.poly(undefined, pen, [at(tie.x1, tie.y1), at(tie.x2, tie.y1), at(tie.x2, tie.y2), at(tie.x1, tie.y2)], true);
    else out.line(pen, ...at(tie.x1, tie.y1), ...at(tie.x2, tie.y2));
  }

  out.layer = 'bars';
  const showNumbers = TEXT_MM * view.scale >= NUMBER_MIN_PX;
  for (const bar of d.bars) {
    const on = bar.number === highlight;
    const [x, y] = at(bar.x, bar.y);
    const r = Math.max(1.5, (bar.diameterMm / 2) * k);
    out.circle(on ? palette.selected : palette.kata.bar, lineweight(on ? palette.selected : palette.kata.bar, THIN_PX, view.scale), x, y, r);
    hits.push({ number: bar.number, t: 'circle', x, y, r });
    if (showNumbers) {
      const dx = bar.x - d.widthMm / 2;
      const dy = bar.y - d.depthMm / 2;
      const len = Math.max(1, Math.hypot(dx, dy));
      const [nx, ny] = at(bar.x + (dx / len) * 70, bar.y + (dy / len) * 70);
      cadTextCentredOn(out, String(bar.number), on ? palette.selected : palette.kata.number, TEXT_MM * 0.55, view.scale, nx, ny, 'centre');
    }
  }

  out.layer = 'dims';
  paintSectionDims(out, s, view, palette, at);
  out.layer = 'notes';
  const [tx, ty] = at(d.widthMm / 2, -340 / SECTION_FACTOR);
  const titleWidth = cadText(out, s.name, palette.text, TITLE_MM, view.scale, tx, ty, 'centre');
  if (titleWidth > 0) out.line(lineweight(palette.text, THIN_PX, view.scale), tx - titleWidth / 2, ty + 3, tx + titleWidth / 2, ty + 3);
  const [lx, ly] = at(d.widthMm / 2, -560 / SECTION_FACTOR);
  cadText(out, `${d.level} — TL: 1/20`, palette.mutedText, TEXT_MM, view.scale, lx, ly, 'centre');
  out.layer = undefined;
}

/** The section's overall sizes: b under it, h on its right (Ø for a round one). */
function paintSectionDims(out: ShapeList, s: PlacedSection, view: SheetView, palette: Palette, at: (x: number, y: number) => [number, number]) {
  const d = s.drawing;
  const pen = lineweight(palette.kata.grey, THIN_PX, view.scale);
  const tick = lineweight(palette.kata.grey, OUTLINE_PX, view.scale);
  const below = -120 / SECTION_FACTOR;
  out.line(pen, ...at(0, below), ...at(d.widthMm, below));
  out.tick(tick, ...at(0, below));
  out.tick(tick, ...at(d.widthMm, below));
  const [bx, by] = at(d.widthMm / 2, below + 20 / SECTION_FACTOR);
  cadText(out, d.isRound ? `Ø${mm(d.widthMm)}` : mm(d.widthMm), palette.dimText, STYLE.dimTextHeight, view.scale, bx, by, 'centre');
  if (d.isRound) return;
  const beside = d.widthMm + 120 / SECTION_FACTOR;
  out.line(pen, ...at(beside, 0), ...at(beside, d.depthMm));
  out.tick(tick, ...at(beside, 0));
  out.tick(tick, ...at(beside, d.depthMm));
  const [hx, hy] = at(beside - 20 / SECTION_FACTOR, d.depthMm / 2);
  cadText(out, mm(d.depthMm), palette.dimText, STYLE.dimTextHeight, view.scale, hx, hy, 'centre', true);
}

