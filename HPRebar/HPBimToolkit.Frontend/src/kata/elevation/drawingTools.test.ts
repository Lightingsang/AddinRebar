// The web-only tools on the B03 fixture: layers, measuring with snapping, finding a bar, file names.
import { describe, expect, it } from 'vitest';
import fixture from '../__fixtures__/kata-scene-b03.json';
import type { KataWebScene } from '../kataScene';
import { describeMeasure, findBar, snap, toModel, visibleShapes } from './drawingTools';
import { frameAll, kataFrameOptions } from './framing';
import { kataPoint, paintKataElevation } from './kataElevationPainter';
import { paintTags, tagBand } from './kataTagPainter';
import { paletteFor } from './palette';
import { buildScene } from './scene';

const scene = fixture as unknown as KataWebScene;
const { elevation, stationMap } = scene;
const rebar = scene.rebar!;
const options = kataFrameOptions(elevation, rebar, stationMap, tagBand(rebar, elevation.topMm - elevation.bottomMm));
const kataScene = buildScene(elevation, frameAll(elevation, 1600, 700, options), 1600, 700, -1);
const point = kataPoint(kataScene, stationMap);
const palette = paletteFor('dark');

describe('layers', () => {
  it('hides the shapes of a layer and keeps everything else', () => {
    const shapes = [...paintKataElevation(kataScene, palette, rebar, stationMap), ...paintTags(kataScene, palette, rebar, stationMap)];
    const withoutBars = visibleShapes(shapes, new Set(['bars']));
    expect(withoutBars.some((s) => s.layer === 'bars')).toBe(false);
    expect(withoutBars.filter((s) => s.layer === 'stirrups')).toHaveLength(shapes.filter((s) => s.layer === 'stirrups').length);
    expect(visibleShapes(shapes, new Set(['tags'])).some((s) => s.t === 'circle' && s.layer === 'tags')).toBe(false);
    expect(visibleShapes(shapes, new Set())).toBe(shapes);
  });
});

describe('measuring', () => {
  it('reads a screen point back as the drawing point it shows', () => {
    const [x, y] = point(5000, -600);
    const model = toModel(kataScene, stationMap, [x, y]);
    expect(model.x).toBeCloseTo(5000, 6);
    expect(model.z).toBeCloseTo(-600, 6);
  });

  it('snaps to a bar vertex within reach, else measures where the cursor is', () => {
    const vertex = rebar.lines.find((l) => l.pen === 'bar')!.points[0]!;
    const [x, y] = point(vertex.x, vertex.z);
    expect(snap(rebar, kataScene, stationMap, point, [x + 3, y - 2])).toEqual({ model: { x: vertex.x, z: vertex.z }, snapped: true });
    expect(snap(rebar, kataScene, stationMap, point, [3, 3]).snapped).toBe(false);
  });

  it('writes the horizontal, vertical and true distance', () => {
    expect(describeMeasure({ x: 0, z: 0 }, { x: 3000, z: -4000 })).toBe('Δx 3.000 · Δz 4.000 · L 5.000 mm');
  });
});

describe('finding a bar', () => {
  it('lists every tag carrying the number, left to right', () => {
    const number = rebar.tags[0]!.numbers[0]!;
    const found = findBar(rebar, number);
    expect(found.length).toBe(rebar.tags.filter((t) => t.numbers.includes(number)).length);
    expect(found.map((f) => f.x)).toEqual([...found.map((f) => f.x)].sort((a, b) => a - b));
    expect(findBar(rebar, 9999)).toEqual([]);
  });
});

