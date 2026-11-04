// Kata's column drawing as painter shapes, in screen px (pure: sheet + view in, shapes out), laid out as VC draws it:
// the elevation along b — pile cap, storeys, beams, levels with CT marks, representative bars with their labels,
// hoops, section marks and the lap / level / zone dimensions — and the storeys' sections. The bars are also returned
// as hit targets so the page can pick them.

import type { KataColumnDrawingBar, KataDimChain } from '../columnContracts';
import { BAR_PX, OUTLINE_PX, THIN_PX, barPx, cadText, cadTextCentredOn, lineweight, STYLE } from '../../kata/elevation/kataCad';
import type { Palette, Pen } from '../../kata/elevation/palette';
import { ShapeList } from '../../kata/elevation/shapes';
import { paintSection } from './columnSectionPainter';
import { DIM_GAP_MM, toScreen, type ColumnSheet, type SheetView } from './columnSheet';
import { LABEL_CIRCLE_MM, LEVEL_TEXT_MM, TEXT_MM, TITLE_MM, mm, type BarHit, type PaintedSheet } from './columnStyle';

export type { BarHit, PaintedSheet } from './columnStyle';


/** Level in metres as Kata's CT block writes it: "+2.500", "-4.000", "±0.000". */
export const levelText = (zMm: number) => (Math.abs(zMm) < 0.5 ? '±0.000' : `${zMm > 0 ? '+' : '-'}${(Math.abs(zMm) / 1000).toFixed(3)}`);

/** The label of a representative bar: "20Ø25". */
export const barLabel = (bar: KataColumnDrawingBar) => `${bar.count}Ø${mm(bar.diameterMm)}`;

export function paintColumnSheet(sheet: ColumnSheet, view: SheetView, palette: Palette, highlight: number | null): PaintedSheet {
  const out = new ShapeList();
  const hits: BarHit[] = [];
  paintConcrete(out, sheet, view, palette);
  paintLevels(out, sheet, view, palette);
  paintHoops(out, sheet, view, palette);
  for (const chain of sheet.elevation.chains) paintChain(out, chain, view, palette);
  paintCuts(out, sheet, view, palette);
  paintBars(out, hits, sheet, view, palette, highlight);
  for (const section of sheet.sections) paintSection(out, hits, section, view, palette, highlight);
  return { shapes: out.shapes, hits };
}

const P = (view: SheetView, x: number, y: number) => toScreen(view, x, y);

function paintConcrete(out: ShapeList, sheet: ColumnSheet, view: SheetView, palette: Palette) {
  const e = sheet.elevation;
  const pen = lineweight(palette.kata.outline, OUTLINE_PX, view.scale);
  const rect = (x1: number, z1: number, x2: number, z2: number, p: Pen) => out.poly(undefined, p, [P(view, x1, z1), P(view, x2, z1), P(view, x2, z2), P(view, x1, z2)], true);
  out.layer = 'concrete';
  const capBottom = e.levels[0]?.zMm ?? e.bottomMm;
  // A pile cap only when the sheet has one (its level is below the first storey).
  if (e.storeys[0] && capBottom < e.storeys[0].baseMm - 1) rect(e.pileCapLeftMm, capBottom, e.pileCapRightMm, e.storeys[0].baseMm, pen);
  for (const s of e.storeys) {
    rect(s.leftMm, s.baseMm, s.rightMm, s.topMm, pen);
    if (s.beamDepthMm > 0) paintBeams(out, view, pen, s.leftMm, s.rightMm, s.topMm, s.beamDepthMm);
  }
  out.layer = 'notes';
  // The column's name under its elevation, as VC writes it: "C1 (SL=1)".
  const [tx, ty] = P(view, (e.leftMm + e.rightMm) / 2, e.bottomMm - 450);
  cadText(out, e.title, palette.text, TITLE_MM, view.scale, tx, ty, 'centre');
  out.layer = undefined;
}

/** Beam reach drawn beside the column at each head (mm), ended by a break mark. */
const BEAM_STUB_MM = 450;

/** The beams cut at a storey's head, as VC draws them: top and soffit lines out from both faces, a break mark at each end. */
function paintBeams(out: ShapeList, view: SheetView, pen: Pen, left: number, right: number, top: number, depth: number) {
  for (const [face, end] of [[left, left - BEAM_STUB_MM], [right, right + BEAM_STUB_MM]] as const) {
    out.line(pen, ...P(view, face, top), ...P(view, end, top));
    out.line(pen, ...P(view, face, top - depth), ...P(view, end, top - depth));
    const mid = top - depth / 2;
    const k = Math.sign(end - face) * 40;
    out.poly(undefined, pen, [P(view, end, top + 60), P(view, end, mid + 50), P(view, end + k, mid + 20), P(view, end - k, mid - 20), P(view, end, mid - 50), P(view, end, top - depth - 60)]);
  }
}

function paintLevels(out: ShapeList, sheet: ColumnSheet, view: SheetView, palette: Palette) {
  const e = sheet.elevation;
  const pen = lineweight(palette.kata.grey, THIN_PX, view.scale, STYLE.centerDashes);
  const right = e.rightMm + 4 * DIM_GAP_MM;
  out.layer = 'grid';
  for (const level of e.levels) {
    out.line(pen, ...P(view, sheet.levelLeft, level.zMm), ...P(view, right, level.zMm));
    const [x, y] = P(view, sheet.levelLeft, level.zMm + 40);
    cadText(out, level.name, palette.text, LEVEL_TEXT_MM, view.scale, x, y, 'left');
    // CT mark: a small triangle on the line and the level in metres beside it.
    const tx = sheet.levelLeft + 1100;
    out.poly(undefined, lineweight(palette.kata.number, THIN_PX, view.scale), [P(view, tx, level.zMm), P(view, tx - 60, level.zMm + 100), P(view, tx + 60, level.zMm + 100)], true);
    const [cx, cy] = P(view, tx + 90, level.zMm + 40);
    cadText(out, levelText(level.zMm), palette.kata.tagText, TEXT_MM, view.scale, cx, cy, 'left');
  }
  out.layer = undefined;
}

function paintHoops(out: ShapeList, sheet: ColumnSheet, view: SheetView, palette: Palette) {
  const pen = lineweight(palette.kata.stirrup, THIN_PX, view.scale);
  out.layer = 'stirrups';
  for (const zone of sheet.elevation.hoopZones) {
    // As VC: the first two and the last two hoops of each zone mark it; its spacing is in the label.
    for (const i of new Set([0, 1, zone.count - 2, zone.count - 1].filter((n) => n >= 0 && n < zone.count))) {
      const z = zone.fromMm + i * zone.spacingMm;
      out.line(pen, ...P(view, zone.leftMm, z), ...P(view, zone.rightMm, z));
    }

    const z = (zone.fromMm + zone.toMm) / 2;
    const x = sheet.labelRight + LABEL_CIRCLE_MM;
    const leader = lineweight(palette.kata.leader, THIN_PX, view.scale);
    out.line(leader, ...P(view, zone.rightMm, z), ...P(view, x - LABEL_CIRCLE_MM, z));
    tagCircle(out, view, palette, x, z, zone.number);
    const [tx, ty] = P(view, x + LABEL_CIRCLE_MM + 30, z);
    cadTextCentredOn(out, `Ø${mm(zone.diameterMm)}a${mm(zone.spacingMm)}`, palette.kata.tagText, TEXT_MM, view.scale, tx, ty, 'left');
  }
  out.layer = undefined;
}

/** A dimension chain along z: dimension line, extension lines from the measured points, ticks and values. */
function paintChain(out: ShapeList, chain: KataDimChain, view: SheetView, palette: Palette) {
  if (!chain.vertical || chain.stations.length < 2) return;
  const pen = lineweight(palette.kata.grey, THIN_PX, view.scale);
  const x = chain.lineAt;
  out.layer = 'dims';
  const first = chain.stations[0]!;
  const last = chain.stations[chain.stations.length - 1]!;
  out.line(pen, ...P(view, x, first.along - STYLE.dimLineBeyond), ...P(view, x, last.along + STYLE.dimLineBeyond));
  for (const s of chain.stations) {
    const toward = Math.sign(x - s.across) || 1;
    out.line(pen, ...P(view, s.across + toward * STYLE.extensionLength * 0.3, s.along), ...P(view, x + toward * STYLE.extensionBeyond, s.along));
    out.tick(lineweight(palette.kata.grey, OUTLINE_PX, view.scale), ...P(view, x, s.along));
  }

  for (let i = 1; i < chain.stations.length; i++) {
    const a = chain.stations[i - 1]!.along;
    const b = chain.stations[i]!.along;
    const [tx, ty] = P(view, x - STYLE.dimTextGap, (a + b) / 2);
    cadText(out, mm(b - a), palette.dimText, STYLE.dimTextHeight, view.scale, tx, ty, 'centre', true);
  }
  out.layer = undefined;
}

/** Section marks at each cut, both sides of the stack, with the section's number (VC's kata_block_SECBAL). */
function paintCuts(out: ShapeList, sheet: ColumnSheet, view: SheetView, palette: Palette) {
  const e = sheet.elevation;
  const pen = lineweight(palette.kata.number, BAR_PX, view.scale);
  out.layer = 'flags';
  for (const cut of e.cuts) {
    const storey = e.storeys[cut.storey];
    if (!storey) continue;
    const number = cut.name.split('-')[0] ?? cut.name;
    for (const [a, b, align] of [[storey.leftMm - 260, storey.leftMm - 60, 'right'], [storey.rightMm + 60, storey.rightMm + 260, 'left']] as const) {
      out.line(pen, ...P(view, a, cut.zMm), ...P(view, b, cut.zMm));
      const [tx, ty] = P(view, align === 'right' ? a : b, cut.zMm + 40);
      cadText(out, number, palette.kata.number, TEXT_MM, view.scale, tx, ty, align);
    }
  }
  out.layer = undefined;
}

function paintBars(out: ShapeList, hits: BarHit[], sheet: ColumnSheet, view: SheetView, palette: Palette, highlight: number | null) {
  const e = sheet.elevation;
  const width = barPx(view.scale);
  out.layer = 'bars';
  for (const bar of e.bars) {
    const on = bar.number === highlight;
    const pen = lineweight(on ? palette.selected : palette.kata.bar, on ? width + 2 : width, view.scale);
    const points = bar.points.map((p) => P(view, p.x, p.z));
    out.poly(undefined, pen, points, false, `bar-${bar.number}`);
    for (let i = 1; i < points.length; i++) {
      const [x1, y1] = points[i - 1]!;
      const [x2, y2] = points[i]!;
      hits.push({ number: bar.number, t: 'segment', x1, y1, x2, y2 });
    }
  }

  out.layer = 'tags';
  // One label per storey, group and Ø (VC's "8Ø20" / "12Ø25"); a second layer of the same Ø is drawn unlabelled.
  for (const bar of e.bars) if (bar.labelled) paintBarLabel(out, sheet, view, palette, bar, bar.number === highlight);
  out.layer = undefined;
}

/** Where a bar's label sits: starters in the cap, group A left at 40 % of the storey, group B right at 62 %, layer 2 a step higher. */
function labelHeight(sheet: ColumnSheet, bar: KataColumnDrawingBar): number {
  const e = sheet.elevation;
  const storey = e.storeys[bar.storey];
  if (!storey) return bar.points[0]?.z ?? 0;
  const lift = (bar.layer - 1) * 260;
  if (bar.kind === 'starter') return (Math.min(...bar.points.map((p) => p.z)) + storey.baseMm) / 2 + (bar.group === 'b' ? 200 : 0) + lift;
  return storey.baseMm + (storey.topMm - storey.baseMm) * (bar.group === 'a' ? 0.4 : 0.62) + lift;
}

/** The bar's plan offset at elevation z (on its segment spanning z, else its nearest end). */
function xAt(bar: KataColumnDrawingBar, z: number): number {
  for (let i = 1; i < bar.points.length; i++) {
    const a = bar.points[i - 1]!;
    const b = bar.points[i]!;
    if ((z - a.z) * (z - b.z) <= 0 && Math.abs(b.z - a.z) > 1e-6) return a.x + ((z - a.z) / (b.z - a.z)) * (b.x - a.x);
  }
  const ends = [bar.points[0]!, bar.points[bar.points.length - 1]!];
  return Math.abs(ends[0]!.z - z) < Math.abs(ends[1]!.z - z) ? ends[0]!.x : ends[1]!.x;
}

function paintBarLabel(out: ShapeList, sheet: ColumnSheet, view: SheetView, palette: Palette, bar: KataColumnDrawingBar, on: boolean) {
  const z = labelHeight(sheet, bar);
  const left = bar.group === 'a';
  const x = left ? sheet.labelLeft - LABEL_CIRCLE_MM : sheet.labelRight + LABEL_CIRCLE_MM;
  const leader = lineweight(on ? palette.selected : palette.kata.leader, THIN_PX, view.scale);
  out.line(leader, ...P(view, xAt(bar, z), z), ...P(view, left ? x + LABEL_CIRCLE_MM : x - LABEL_CIRCLE_MM, z));
  tagCircle(out, view, palette, x, z, bar.number, on);
  const [tx, ty] = P(view, left ? x - LABEL_CIRCLE_MM - 30 : x + LABEL_CIRCLE_MM + 30, z);
  cadTextCentredOn(out, barLabel(bar), on ? palette.selected : palette.kata.tagText, TEXT_MM, view.scale, tx, ty, left ? 'right' : 'left');
}

function tagCircle(out: ShapeList, view: SheetView, palette: Palette, x: number, z: number, number: number, on = false) {
  const [cx, cy] = P(view, x, z);
  out.circle(undefined, lineweight(on ? palette.selected : palette.kata.circle, THIN_PX, view.scale), cx, cy, LABEL_CIRCLE_MM * view.scale);
  cadTextCentredOn(out, String(number), on ? palette.selected : palette.kata.number, TEXT_MM * 0.9, view.scale, cx, cy, 'centre');
}

/** The bar number under (x, y) px, or null: segments within `reach` px, circles within their radius + `reach`. */
export function barAt(hits: BarHit[], x: number, y: number, reach = 6): number | null {
  let best: { number: number; distance: number } | null = null;
  for (const hit of hits) {
    const distance = hit.t === 'circle' ? Math.max(0, Math.hypot(x - hit.x, y - hit.y) - hit.r) : segmentDistance(x, y, hit);
    if (distance <= reach && (!best || distance < best.distance)) best = { number: hit.number, distance };
  }
  return best?.number ?? null;
}

function segmentDistance(x: number, y: number, s: { x1: number; y1: number; x2: number; y2: number }) {
  const dx = s.x2 - s.x1;
  const dy = s.y2 - s.y1;
  const l2 = dx * dx + dy * dy;
  const t = l2 < 1e-9 ? 0 : Math.max(0, Math.min(1, ((x - s.x1) * dx + (y - s.y1) * dy) / l2));
  return Math.hypot(x - (s.x1 + t * dx), y - (s.y1 + t * dy));
}
