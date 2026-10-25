// The plain elevation drawn from the B03 fixture (HPRebar.Core.Tests writes it from KataWebSceneMapper): framing,
// picking and what the painters draw, against the rules of the WPF canvas.
import { describe, expect, it } from 'vitest';
import fixture from '../__fixtures__/kata-scene-b03.json';
import type { KataWebScene } from '../kataScene';
import { paintAnnotations } from './elevationAnnotations';
import { paintElevation } from './elevationPainter';
import { FOCUS_MAX_SCALE, MARGIN_PX, ZERO_WIDTH_REACH_PX, frameAll, frameColumn } from './framing';
import { paletteFor } from './palette';
import { ABOVE_PX, LOWER_STUB_PX, UPPER_STUB_PX, beamFaces, buildScene, columnAt, focusRange, sx, sy } from './scene';
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

  it('draws each beam of a span at its own top and bottom, with the step between them', () => {
    // Span F made of a dropped 600 mm beam up to a transfer column at 15000, then the 900 mm beam.
    const stepped = {
      ...elevation,
      beams: [
        elevation.beams[0]!,
        { extent: { start: 11000, end: 15000 }, topMm: -50, bottomMm: -650, heightMm: 600 },
        { extent: { start: 15000, end: 24800 }, topMm: 0, bottomMm: -900, heightMm: 900 },
        ...elevation.beams.slice(2),
      ],
    };
    const s = buildScene(stepped, v, W, H, -1);
    const shapes = paintElevation(s, palette);
    const lines = shapes.filter((x) => x.t === 'line' && x.pen === palette.outline);
    const horizontal = (y: number, from: number, to: number) =>
      lines.some((x) => x.t === 'line' && x.y1 === y && x.y2 === y && x.x1 === sx(s, from) && x.x2 === sx(s, to));
    const vertical = (at: number, y1: number, y2: number) =>
      lines.some((x) => x.t === 'line' && x.x1 === sx(s, at) && x.x2 === sx(s, at) && x.y1 === y1 && x.y2 === y2);

    expect(horizontal(sy(s, -50), 11200, 15000)).toBe(true);
    expect(horizontal(sy(s, -650), 11200, 15000)).toBe(true);
    expect(horizontal(sy(s, 0), 15000, 24600)).toBe(true);
    expect(vertical(15000, sy(s, -50), sy(s, 0))).toBe(true);
    expect(vertical(15000, sy(s, -650), sy(s, -900))).toBe(true);
    // Support E's right face meets the dropped beam, not the deepest beam of the span.
    expect(vertical(11200, sy(s, -50), sy(s, 0) - UPPER_STUB_PX)).toBe(true);
    expect(vertical(11200, sy(s, -650), sy(s, -1200) + LOWER_STUB_PX)).toBe(true);
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
