import { describe, expect, it } from 'vitest';
import sampleFixture from '../__fixtures__/kata-column-rebar-sample.json';
import type { KataColumnRebarSummary } from '../columnContracts';
import { paletteFor } from '../../kata/elevation/palette';
import { barAt, barLabel, levelText, paintColumnSheet } from './columnPainter';
import { describeBar } from './ColumnDrawing';
import { COLUMN_STRINGS } from '../i18n';
import { clampZoom, fit, layoutSheet, MAX_SCALE, SECTION_FACTOR, toScreen, zoomAt } from './columnSheet';

const summary = sampleFixture as KataColumnRebarSummary;
const elevation = summary.elevation!;
const sheet = layoutSheet(elevation, summary.sections);
const view = fit(sheet.extents, 1600, 900);
const texts = (shapes: ReturnType<typeof paintColumnSheet>['shapes']) => shapes.flatMap((s) => (s.t === 'ctext' ? [s.text] : []));

describe('column sheet layout', () => {
  it('puts the sections right of the elevation, top storey first, 1.5 × larger', () => {
    expect(sheet.sections.map((s) => s.index)).toEqual([...summary.sections.keys()].reverse());
    expect(sheet.sections.every((s) => s.x > elevation.rightMm)).toBe(true);
    const ys = sheet.sections.map((s) => s.y);
    expect([...ys].sort((a, b) => b - a)).toEqual(ys);
    expect(SECTION_FACTOR).toBe(1.5);
  });

  it('fits the whole sheet and zooms about the cursor', () => {
    const [l, t] = toScreen(view, sheet.extents.left, sheet.extents.top);
    const [r, b] = toScreen(view, sheet.extents.right, sheet.extents.bottom);
    expect(Math.min(l, t)).toBeGreaterThanOrEqual(23.9);
    expect(r).toBeLessThanOrEqual(1576.1);
    expect(b).toBeLessThanOrEqual(876.1);
    const zoomed = zoomAt(view, 2, 400, 300);
    expect(zoomed.scale).toBeCloseTo(view.scale * 2);
    expect(zoomed.offsetX + ((400 - view.offsetX) / view.scale) * zoomed.scale).toBeCloseTo(400);
  });

  it('stops zooming in past 20 px/mm and out past a quarter of the fit', () => {
    const close = { ...view, scale: MAX_SCALE };
    expect(clampZoom(close, zoomAt(close, 1.25, 0, 0), view.scale)).toBe(close);
    const far = { ...view, scale: view.scale / 4 };
    expect(clampZoom(far, zoomAt(far, 0.8, 0, 0), view.scale)).toBe(far);
    expect(clampZoom(view, zoomAt(view, 1.25, 0, 0), view.scale).scale).toBeCloseTo(view.scale * 1.25);
  });
});

describe('column sheet painter', () => {
  const painted = paintColumnSheet(sheet, zoomAt(view, 4, 800, 450), paletteFor('dark'), null);

  it('draws every representative bar and every section bar as a pick target', () => {
    const polylines = painted.shapes.filter((s) => s.t === 'poly' && s.id?.startsWith('bar-'));
    expect(polylines).toHaveLength(elevation.bars.length);
    const circles = painted.hits.filter((h) => h.t === 'circle');
    expect(circles).toHaveLength(summary.sections.reduce((n, s) => n + s.bars.length, 0));
  });

  it('writes the levels, the CT marks, the bar labels, the hoop notes and the section titles', () => {
    const words = texts(painted.shapes);
    for (const level of elevation.levels) {
      expect(words).toContain(level.name);
      expect(words).toContain(levelText(level.zMm));
    }
    for (const bar of elevation.bars.filter((b) => b.labelled)) expect(words).toContain(barLabel(bar));
    // One label per storey, group and Ø: the labels of the first storey add up to its 40 bars.
    const firstStorey = elevation.bars.filter((b) => b.storey === 0 && b.kind !== 'starter' && b.labelled);
    expect(firstStorey.reduce((n, b) => n + b.count, 0)).toBe(summary.rows.at(-1)!.barCount);
    expect(words).toContain('Ø10a100');
    expect(words).toContain('C1 (SL=1)');
    for (const cut of elevation.cuts) expect(words).toContain(cut.name);
  });

  it('marks each hoop zone with its first two and last two hoops, as VC', () => {
    // Elevation hoops start at a zone's left hoop line (the sections' crossties share the layer).
    const zoomed = zoomAt(view, 4, 800, 450);
    const lefts = new Set(elevation.hoopZones.map((z) => toScreen(zoomed, z.leftMm, 0)[0].toFixed(3)));
    const hoopLines = painted.shapes.filter((s) => s.t === 'line' && s.layer === 'stirrups' && Math.abs(s.y1 - s.y2) < 1e-6 && lefts.has(s.x1.toFixed(3)));
    const expected = elevation.hoopZones.reduce((n, z) => n + Math.min(4, z.count), 0);
    expect(hoopLines).toHaveLength(expected);
  });

  it('highlights a picked number in the elevation and the sections', () => {
    const number = elevation.bars.find((b) => b.kind !== 'starter')!.number;
    const lit = paintColumnSheet(sheet, view, paletteFor('light'), number);
    const selected = paletteFor('light').selected;
    expect(lit.shapes.some((s) => s.t === 'poly' && s.id === `bar-${number}` && s.pen?.color === selected)).toBe(true);
    expect(lit.shapes.some((s) => s.t === 'circle' && s.fill === selected)).toBe(true);
  });

  it('picks the bar under the cursor and nothing on empty paper', () => {
    const segment = painted.hits.find((h) => h.t === 'segment')!;
    if (segment.t !== 'segment') throw new Error('segment expected');
    expect(barAt(painted.hits, (segment.x1 + segment.x2) / 2 + 2, (segment.y1 + segment.y2) / 2)).toBe(segment.number);
    const circle = painted.hits.find((h) => h.t === 'circle')!;
    if (circle.t !== 'circle') throw new Error('circle expected');
    expect(barAt(painted.hits, circle.x, circle.y)).toBe(circle.number);
    expect(barAt(painted.hits, -500, -500)).toBeNull();
  });

  it('describes a picked bar with its Ø, length, group and storey', () => {
    const bar = elevation.bars.find((b) => b.kind === 'main')!;
    const chip = describeBar(bar.number, elevation, summary.sections, COLUMN_STRINGS.vi)!;
    expect(chip.title).toBe(`Thanh ${bar.number}`);
    expect(chip.facts).toContain(`Ø${bar.diameterMm}`);
    expect(chip.facts).toContain(`dài ${Math.round(bar.lengthMm)} mm`);
    expect(chip.facts).toContain(elevation.storeys[bar.storey]!.name);
  });

  it('writes levels in metres as Kata does', () => {
    expect([levelText(0), levelText(2500), levelText(-4000)]).toEqual(['±0.000', '+2.500', '-4.000']);
  });
});
