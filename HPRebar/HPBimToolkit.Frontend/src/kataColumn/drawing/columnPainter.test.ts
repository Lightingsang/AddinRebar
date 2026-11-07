import { describe, expect, it } from 'vitest';
import sampleFixture from '../__fixtures__/kata-column-rebar-sample.json';
import type { KataColumnRebarSummary } from '../columnContracts';
import { paletteFor } from '../../kata/elevation/palette';
import { barAt, barLabel, levelText, paintColumnSheet } from './columnPainter';
import { describeBar } from './ColumnDrawing';
import { COLUMN_STRINGS } from '../i18n';
import { tieDetails } from './columnTieDetail';
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

describe('taken-apart hoop and ties', () => {
  const placed = sheet.sections.at(-1)!;
  const d = placed.drawing;

  it('keeps the shapes VC keeps: the hoop, then every tie moved its own way', () => {
    const [hoop, ...ties] = tieDetails(d);
    expect(hoop!.kind).toBe('hoop');
    expect(hoop!.note).toBe('Ø10a100');
    expect(ties).toHaveLength(d.ties.length);
    expect(ties.every((t) => t.note === 'Ø8a400')).toBe(true);
    expect(hoop!.x1).toBeGreaterThan(d.widthMm);

    const centre = (t: { x1: number; x2: number; y1: number; y2: number }) => [(t.x1 + t.x2) / 2, (t.y1 + t.y2) / 2] as const;
    const [hx, hy] = centre(hoop!);
    const cross = ties.filter((t) => t.kind === 'cross');
    // As j-all500: the horizontal C goes left of the hoop, the vertical C below it.
    const across = cross.find((t) => t.y1 === t.y2)!;
    const along = cross.find((t) => t.x1 === t.x2)!;
    expect(centre(across)[0]).toBeLessThan(hx);
    expect(centre(along)[1]).toBeLessThan(hy);
    const closed = ties.filter((t) => t.kind === 'closed');
    expect(closed.some((t) => centre(t)[0] > hx + 1)).toBe(true);
    expect(closed.some((t) => centre(t)[1] > hy + 1)).toBe(true);
  });

  it('leaves a round section with its hoop alone', () => {
    const round = { ...d, isRound: true, widthMm: 800, depthMm: 800, ties: [] };
    expect(tieDetails(round).map((t) => t.kind)).toEqual(['hoop']);
  });

  it('keeps every section with its labels and details clear of the next one', () => {
    for (let i = 1; i < sheet.sections.length; i++) expect(sheet.sections[i]!.frame.top).toBeLessThan(sheet.sections[i - 1]!.frame.bottom);
    expect(Math.max(...sheet.sections.map((s) => s.frame.right))).toBeLessThanOrEqual(sheet.extents.right);
    expect(sheet.extents.bottom).toBeLessThanOrEqual(sheet.sections.at(-1)!.frame.bottom);
  });

  it('labels each section per bar number, the counts adding up to its bars, and each shape with its spacing', () => {
    for (const s of sheet.sections) expect(s.groups.reduce((n, g) => n + Number.parseInt(g.text, 10), 0)).toBe(s.drawing.bars.length);
    const floor2 = sheet.sections.find((s) => s.drawing.level === 'FLOOR 2')!;
    expect(floor2.groups.map((g) => g.text).sort()).toEqual(['24Ø25', '8Ø20', '8Ø20']);
    expect(new Set(floor2.groups.map((g) => g.number)).size).toBe(3);
    const words = texts(paintColumnSheet(sheet, zoomAt(view, 4, 800, 450), paletteFor('dark'), null).shapes);
    expect(words.filter((w) => w === 'Ø8a400').length).toBe(sheet.sections.reduce((n, s) => n + s.drawing.ties.length, 0));
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
