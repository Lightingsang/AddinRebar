// Kata's rebar elevation drawn from the B03 fixture: lineweights and linetypes, layer order, dimensions, flags, tags and
// the framing of the band, against the rules of the WPF painters (KataElevationCadPainter, KataCadDimPainter,
// KataElevationBarTagPainter).
import { describe, expect, it } from 'vitest';
import fixture from '../__fixtures__/kata-scene-b03.json';
import type { KataWebScene } from '../kataScene';
import { KATA_MARGIN_PX, frameAll, kataFrameOptions } from './framing';
import { BAR_PX, OUTLINE_PX, STYLE, THIN_PX, barPx, lineweight } from './kataCad';
import { kataPoint, paintKataElevation } from './kataElevationPainter';
import { paintTags, tagBand } from './kataTagPainter';
import { paletteFor } from './palette';
import { buildScene } from './scene';
import type { Shape } from './shapes';
import { toScreen } from './viewport';

const scene = fixture as unknown as KataWebScene;
const { elevation, stationMap } = scene;
const rebar = scene.rebar!;
const palette = paletteFor('dark');
const W = 1600;
const H = 700;
const depth = elevation.topMm - elevation.bottomMm;
const tags = tagBand(rebar, depth);
const options = kataFrameOptions(elevation, rebar, stationMap, tags);
const viewport = frameAll(elevation, W, H, options);
const kataScene = buildScene(elevation, viewport, W, H, -1, options.band!.above * viewport.scale, options.band!.below * viewport.scale);

const polys = (shapes: Shape[]) => shapes.filter((s): s is Extract<Shape, { t: 'poly' }> => s.t === 'poly');
const cadTexts = (shapes: Shape[]) => shapes.flatMap((s) => (s.t === 'ctext' ? [s.text] : []));

describe('Kata framing', () => {
  it('frames the run widened to the level mark, with the drawing between thin margins', () => {
    expect(options.range.start).toBeLessThan(elevation.bounds.start + rebar.minX);
    expect(toScreen(viewport, options.range.start)).toBeCloseTo(36, 6);
    const p = kataPoint(kataScene, stationMap);
    const top = p(0, Math.max(rebar.top, tags.above))[1];
    expect(top).toBeGreaterThanOrEqual(KATA_MARGIN_PX - 0.5);
  });

  it('tags above the beam reach past the drawing top when they carry more rows', () => {
    expect(tags.above).toBeGreaterThan(0);
    expect(options.band!.above).toBe(Math.max(rebar.top, tags.above));
  });
});

describe('Kata elevation', () => {
  const shapes = paintKataElevation(kataScene, palette, rebar, stationMap);

  it('draws every line of the drawing, the bars last', () => {
    const lines = polys(shapes).filter((s) => !s.closed);
    expect(lines.length).toBeGreaterThanOrEqual(rebar.lines.length);
    const firstBar = lines.findIndex((s) => s.pen?.color === palette.kata.bar);
    expect(lines.slice(firstBar).every((s) => s.pen?.color !== palette.kata.stirrup)).toBe(true);
  });

  it("draws bars no wider than a bar on screen when zoomed out, Kata's 3 px when zoomed in", () => {
    expect(viewport.scale).toBeLessThan(0.05); // the whole B03 run
    expect(barPx(viewport.scale)).toBe(THIN_PX);
    expect(barPx(0.06)).toBe(2);
    expect(barPx(0.12)).toBe(BAR_PX);
    expect(barPx(2)).toBe(BAR_PX);
  });

  it('keeps lineweights in whole pixels and dashes hidden lines when they are long enough', () => {
    const bar = polys(shapes).find((s) => s.pen?.color === palette.kata.bar)!;
    expect(bar.pen!.width).toBe(barPx(viewport.scale));
    expect(polys(shapes).some((s) => s.pen?.color === palette.kata.outline && s.pen.width === OUTLINE_PX)).toBe(true);
    expect(lineweight('#000', 1, 0.01, STYLE.hiddenDashes).dash).toBeUndefined(); // dashes under 2 px: solid
    expect(lineweight('#000', 1, 0.1, STYLE.hiddenDashes).dash).toBe('11.91 5.95');
  });

  it('writes the dimension values, flag numbers, grid names and title', () => {
    const t = cadTexts(shapes);
    expect(t).toEqual(expect.arrayContaining(rebar.dims.filter((d) => d.text).map((d) => d.text)));
    expect(t).toEqual(expect.arrayContaining(rebar.flags.map((f) => String(f.number))));
    expect(t).toEqual(expect.arrayContaining(rebar.bubbles.map((b) => b.name)));
    expect(t).toContain(rebar.title!.name);
  });

  it('draws the shown section flags in the accent colour', () => {
    const selected = paintKataElevation(kataScene, palette, rebar, stationMap, 1);
    expect(polys(selected).some((s) => s.fill === palette.accent)).toBe(true);
    expect(polys(shapes).some((s) => s.fill === palette.accent)).toBe(false);
  });

  it('skips texts too small to read when zoomed far out', () => {
    const far = frameAll(elevation, 200, H, options);
    const tiny = paintKataElevation(buildScene(elevation, far, 200, H, -1), palette, rebar, stationMap);
    expect(cadTexts(tiny).length).toBeLessThan(cadTexts(shapes).length);
  });
});

describe('Kata tags', () => {
  const shapes = paintTags(kataScene, palette, rebar, stationMap);

  it('writes every tag text and one circle per bar number', () => {
    const t = cadTexts(shapes);
    expect(t).toEqual(expect.arrayContaining(rebar.tags.map((x) => x.text)));
    const circles = shapes.filter((s) => s.t === 'circle').length;
    expect(circles).toBe(rebar.tags.reduce((n, x) => n + x.numbers.length, 0));
  });

  it('puts an arrow on each bar a leader starts from', () => {
    const arrows = polys(shapes).filter((s) => s.closed && s.fill === palette.kata.leader).length;
    expect(arrows).toBe(rebar.tags.filter((x) => x.kind !== 'stirrups').reduce((n, x) => n + x.footZ.length, 0));
  });
});
