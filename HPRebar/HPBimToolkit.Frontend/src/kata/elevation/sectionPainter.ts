// Kata's section n-n (port of KataSectionCadPainter.cs) in a panel, fitted to it then zoomed and panned on its own
// (port of KataSectionView): concrete and slab breaks, hoop and ties (bulges drawn as exact SVG arcs, not flattened),
// bars as kata_block_THEP, leaders with their arrowheads, marking circles, dimensions, tags and the title — with the
// elevation's lineweights. Coordinates are the panel's own pixels.

import type { KataDrawingPen, KataWebSection, KataWebVertex } from '../kataScene';
import { BAR_PX, OUTLINE_PX, THIN_PX, arrow, lineweight } from './kataCad';
import { paintDims } from './kataDimPainter';
import { paintTitle, type Point } from './kataElevationPainter';
import { tagOnLeader } from './kataTagPainter';
import type { Palette, Pen } from './palette';
import { ShapeList, type Shape } from './shapes';

export const PADDING_PX = 14;
/** kata_block_THEP's cross: arms half the bar's diameter, diagonals 0.35 of it. */
const CROSS_ARM = 0.5;
const CROSS_DIAGONAL = 0.35;
/** _DotSmall: a dot of radius 0.3125 of the arrow size. */
const DOT_SMALL_RADIUS = 0.3125;

/** Zoom 1 and no pan = the section fitted to the panel ("zoom extents"). */
export interface SectionView {
  zoom: number;
  panX: number;
  panY: number;
}

export const FITTED: SectionView = { zoom: 1, panX: 0, panY: 0 };
const MIN_ZOOM = 0.25;
const MAX_ZOOM = 40;

/** Zoomed by `factor` keeping the drawing point under (x, y) where it is (panel pixels). */
export function zoomSection(v: SectionView, factor: number, x: number, y: number, width: number, height: number): SectionView {
  const zoom = Math.max(MIN_ZOOM, Math.min(MAX_ZOOM, v.zoom * factor));
  const f = zoom / v.zoom;
  const fx = x - width / 2;
  const fy = y - height / 2;
  return { zoom, panX: fx - (fx - v.panX) * f, panY: fy - (fy - v.panY) * f };
}

/** The section's placement in a panel of `width` × `height`: scale and the mapping of drawing mm to panel pixels. */
export function sectionFrame(section: KataWebSection, width: number, height: number, view: SectionView) {
  const w = section.maxX - section.minX;
  const h = section.top - section.bottom;
  const fit = Math.min((width - 2 * PADDING_PX) / w, (height - 2 * PADDING_PX) / h);
  if (!(w > 0 && h > 0 && fit > 0)) return null;
  const scale = fit * view.zoom;
  const ox = width / 2 + view.panX - ((section.minX + section.maxX) / 2) * scale;
  const oy = height / 2 + view.panY + ((section.top + section.bottom) / 2) * scale;
  return { scale, point: (x: number, z: number): Point => [ox + x * scale, oy - z * scale] };
}

/**
 * An AutoCAD polyline with bulges as an SVG path: a bulge b turns the segment into an arc of 4·atan(b), counter-clockwise
 * for b > 0 in the drawing (z up), i.e. clockwise on screen (y down) — SVG's sweep flag 1.
 */
export function bulgePath(vertices: KataWebVertex[], point: (x: number, z: number) => Point): string {
  if (vertices.length === 0) return '';
  const f = (n: number) => +n.toFixed(2);
  const [x0, y0] = point(vertices[0]!.x, vertices[0]!.z);
  let d = `M${f(x0)} ${f(y0)}`;
  for (let i = 1; i < vertices.length; i++) {
    const from = vertices[i - 1]!;
    const [x, y] = point(vertices[i]!.x, vertices[i]!.z);
    if (Math.abs(from.bulge) < 1e-9) {
      d += ` L${f(x)} ${f(y)}`;
      continue;
    }
    const [ax, ay] = point(from.x, from.z);
    const theta = 4 * Math.atan(Math.abs(from.bulge));
    const radius = Math.hypot(x - ax, y - ay) / (2 * Math.sin(theta / 2));
    d += ` A${f(radius)} ${f(radius)} 0 ${theta > Math.PI ? 1 : 0} ${from.bulge > 0 ? 1 : 0} ${f(x)} ${f(y)}`;
  }
  return d;
}

export function paintSection(section: KataWebSection, palette: Palette, width: number, height: number, view: SectionView): Shape[] {
  const out = new ShapeList();
  const frame = sectionFrame(section, width, height, view);
  if (!frame) return out.shapes;
  const { scale, point: p } = frame;
  const k = palette.kata;
  const weight = (color: string, px: number) => lineweight(color, px, scale);
  const pens: Record<KataDrawingPen, Pen> = {
    outline: weight(k.outline, OUTLINE_PX),
    hidden: weight(k.hidden, THIN_PX),
    grid: weight(k.grey, THIN_PX),
    thin: weight(k.grey, THIN_PX),
    stirrup: weight(k.stirrup, BAR_PX),
    bar: weight(k.bar, BAR_PX),
  };

  for (const line of section.lines) out.shapes.push({ t: 'path', d: bulgePath(line.vertices, p), pen: pens[line.pen] });

  const leader = weight(k.leader, THIN_PX);
  for (const mark of section.marks) out.circle(undefined, leader, ...p(mark.x, mark.z), mark.radius * scale);

  for (const l of section.leaders) {
    const points = l.points.map((q) => p(q.x, q.z));
    if (points.length < 2) continue;
    const size = l.arrowSize * scale;
    const [first, next] = [points[0]!, points[1]!];
    if (l.arrow === 'dotBlank') {
      // The circle, and the leader from its edge, as the block's own stroke continues it.
      out.circle(undefined, leader, ...first, size / 2);
      points[0] = toward(first, next, size / 2);
    } else if (l.arrow === 'dotSmall') {
      out.circle(k.leader, leader, ...first, DOT_SMALL_RADIUS * size);
    } else if (l.arrow === 'closed') {
      arrow(out, k.leader, ...first, ...next, size);
    }
    out.poly(undefined, leader, points);
  }

  for (const bar of section.bars) {
    const [cx, cy] = p(bar.x, bar.z);
    const ringWidth = Math.max(1 / (typeof window === 'undefined' ? 1 : window.devicePixelRatio || 1), (bar.diameter / 12) * scale);
    out.circle(undefined, { color: k.bar, width: ringWidth }, cx, cy, (bar.diameter / 2) * scale);
    const arm = CROSS_ARM * bar.diameter * scale;
    const diagonal = CROSS_DIAGONAL * bar.diameter * scale;
    out.line(pens.bar, cx - arm, cy, cx + arm, cy);
    out.line(pens.bar, cx, cy - arm, cx, cy + arm);
    out.line(pens.bar, cx - diagonal, cy - diagonal, cx + diagonal, cy + diagonal);
    out.line(pens.bar, cx - diagonal, cy + diagonal, cx + diagonal, cy - diagonal);
  }

  paintDims(out, section.dims, palette, p, scale, pens.thin, weight(k.run, THIN_PX));
  for (const t of section.tags) tagOnLeader(out, palette, scale, t.text, t.numbers, ...p(t.x, t.z), t.pointsRight, t.spacing);
  paintTitle(out, palette, p(section.title.x, section.title.z), section.title, scale);
  return out.shapes;
}

function toward(from: Point, to: Point, distance: number): Point {
  const dx = to[0] - from[0];
  const dy = to[1] - from[1];
  const length = Math.hypot(dx, dy);
  return length <= distance ? to : [from[0] + (dx * distance) / length, from[1] + (dy * distance) / length];
}
