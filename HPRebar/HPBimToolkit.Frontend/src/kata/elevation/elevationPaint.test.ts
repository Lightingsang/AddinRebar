// The plain elevation drawn from the B03 fixture (HPRebar.Core.Tests writes it from KataWebSceneMapper): framing,
// picking and what the painters draw, against the rules of the WPF canvas.
import { describe, expect, it } from 'vitest';
import fixture from '../__fixtures__/kata-scene-b03.json';
import type { KataWebScene } from '../kataScene';
import { paintAnnotations } from './elevationAnnotations';
import { paintElevation } from './elevationPainter';
import { FOCUS_MAX_SCALE, MARGIN_PX, ZERO_WIDTH_REACH_PX, frameAll, frameColumn } from './framing';
import { paletteFor } from './palette';
import { ABOVE_PX, beamFaces, buildScene, columnAt, focusRange, sx } from './scene';
import type { Shape } from './shapes';
import { toScreen } from './viewport';

const scene = fixture as unknown as KataWebScene;
const elevation = scene.elevation;
const W = 1600;
const H = 600;

const texts = (shapes: Shape[]) => shapes.flatMap((s) => (s.t === 'text' ? [s.box.text] : []));

describe('framing', () => {
  it('frames the whole run inside the margins, the band centred with its label rows', () => {
    const v = frameAll(elevation, W, H);
    expect(toScreen(v, elevation.bounds.start)).toBeCloseTo(MARGIN_PX, 6);
    expect(toScreen(v, elevation.bounds.end)).toBeCloseTo(W - MARGIN_PX, 6);
    expect(v.offsetY).toBeGreaterThanOrEqual(ABOVE_PX);
  });

  it('frames a span with its supports, never zoomed in past the focus limit', () => {
    const v = frameColumn(elevation, 7, W, H); // J: the 2 m console
    const range = focusRange(elevation, 7);
    expect(range).toEqual({ start: 31200, end: 33600 });
    expect(v.scale).toBeLessThanOrEqual(FOCUS_MAX_SCALE);
  });
});

describe('picking', () => {
  it('a station picks the column holding it, a free end within reach wins', () => {
    expect(columnAt(elevation, 5000, 1)).toBe(1); // span D
    expect(columnAt(elevation, 200, 1)).toBe(0); // support C
    expect(columnAt(elevation, 33600 - 3, 5)).toBe(8); // free end K
    expect(columnAt(elevation, 40000, 5)).toBeNull();
  });

  it('the reach of a zero-width column is a few pixels at any zoom', () => {
    const v = frameAll(elevation, W, H);
    expect(columnAt(elevation, 33600 - 4 / v.scale, ZERO_WIDTH_REACH_PX / v.scale)).toBe(8);
  });
});

describe('painters', () => {
  const v = frameAll(elevation, W, H);
  const palette = paletteFor('dark');

  it('draws one bubble per grid, each named', () => {
    const shapes = paintElevation(buildScene(elevation, v, W, H, -1), palette);
    expect(shapes.filter((s) => s.t === 'circle')).toHaveLength(4);
    expect(texts(shapes)).toEqual(expect.arrayContaining(['1', '2', '3', '4']));
  });

  it('draws the span faces where the beams are, deeper span 1 than span 2', () => {
    const s = buildScene(elevation, v, W, H, -1);
    const span1 = beamFaces(elevation, elevation.columns[1]!.extent);
    const span2 = beamFaces(elevation, elevation.columns[3]!.extent);
    expect(span1.top - span1.bottom).toBe(1200);
    expect(span2.top - span2.bottom).toBe(900);
    const shapes = paintElevation(s, palette);
    const beamLines = shapes.filter((x) => x.t === 'line' && x.pen === palette.outline && x.y1 === x.y2 && x.x1 === sx(s, 400));
    expect(beamLines).toHaveLength(2);
  });

  it('marks the selected column with a band and its letter in the selection colour', () => {
    const s = buildScene(elevation, v, W, H, 3);
    const band = paintElevation(s, palette).find((x) => x.t === 'rect' && x.fill === palette.selectionFill);
    expect(band).toBeDefined();
    const letter = paintAnnotations(s, palette).find((x) => x.t === 'text' && x.color === palette.selected);
    expect(letter?.t === 'text' && letter.box.text).toBe('F');
  });

  it('writes letters, spans, grid distances and span captions', () => {
    const t = texts(paintAnnotations(buildScene(elevation, v, W, H, -1), palette));
    expect(t).toEqual(expect.arrayContaining(['C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K']));
    expect(t).toEqual(expect.arrayContaining(['10400', '13400', '6200']));
    expect(t).toEqual(expect.arrayContaining(['10800', '13800', '6600']));
    expect(t.some((x) => x.startsWith('Nhịp 1'))).toBe(true);
  });

  it('drops labels that would overlap when the run is zoomed far out', () => {
    const narrow = frameAll(elevation, 300, H);
    const t = texts(paintAnnotations(buildScene(elevation, narrow, 300, H, -1), palette));
    expect(t.filter((x) => /^[C-K]$/.test(x)).length).toBeLessThan(9);
  });
});
