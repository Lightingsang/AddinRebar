// Picking and sections on the B03 fixtures: the bar lines and flags a click finds (KataElevationCanvas.Edit / FlagHit),
// the reading of the bar keys, the cut a picked column shows, and Kata's section drawn with exact arcs.
import { describe, expect, it } from 'vitest';
import sceneFixture from '../__fixtures__/kata-scene-b03.json';
import sectionFixture from '../__fixtures__/kata-section-b03-1.json';
import type { KataWebScene, KataWebSection } from '../kataScene';
import { frameAll, kataFrameOptions } from './framing';
import { kataPoint } from './kataElevationPainter';
import { barHits, describeKeys, flagAt, segmentDistance } from './kataHit';
import { tagBand } from './kataTagPainter';
import { paletteFor } from './palette';
import { selectedCut } from './plainSections';
import { buildScene } from './scene';
import { FITTED, bulgePath, paintSection, sectionFrame, zoomSection } from './sectionPainter';

const scene = sceneFixture as unknown as KataWebScene;
const section = sectionFixture as unknown as KataWebSection;
const { elevation, stationMap } = scene;
const rebar = scene.rebar!;
const W = 1600;
const H = 700;
const options = kataFrameOptions(elevation, rebar, stationMap, tagBand(rebar, elevation.topMm - elevation.bottomMm));
const viewport = frameAll(elevation, W, H, options);
const kataScene = buildScene(elevation, viewport, W, H, -1);
const point = kataPoint(kataScene, stationMap);

describe('bar picking', () => {
  it('a click on a bar line finds it first; empty space finds nothing', () => {
    const bar = rebar.lines.find((l) => l.pen === 'bar' && l.keys.length > 0)!;
    const [a, b] = [point(bar.points[0]!.x, bar.points[0]!.z), point(bar.points[1]!.x, bar.points[1]!.z)];
    const middle: [number, number] = [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2 + 2];
    expect(barHits(rebar.lines, middle, point)[0]?.keys).toEqual(expect.arrayContaining(bar.keys.slice(0, 1)));
    expect(barHits(rebar.lines, [5, 5], point)).toHaveLength(0);
  });

  it('measures the distance to a segment, clamped to its ends', () => {
    expect(segmentDistance([5, 3], [0, 0], [10, 0])).toBe(3);
    expect(segmentDistance([14, 3], [0, 0], [10, 0])).toBe(5);
  });

  it('a click on a flag finds it, beside it does not', () => {
    const flag = rebar.flags[0]!;
    const [x, y] = point(flag.x, flag.z);
    const up = flag.below ? 1 : -1;
    expect(flagAt(rebar.flags, [x + 2, y + up * 90 * viewport.scale], point, viewport.scale)?.number).toBe(flag.number);
    expect(flagAt(rebar.flags, [x + 400, y + up * 2000 * viewport.scale], point, viewport.scale)).toBeNull();
  });

  it('reads the keys as bar kinds and stirrup zones', () => {
    expect(describeKeys(['bar|MainTop|3|20|12340|1|-150|0', 'bar|MainTop|4|20|12340|1|150|0'])).toEqual(['Thép chủ trên Ø20 · L 12.340 mm · lớp 1 · 2 thanh']);
    expect(describeKeys(['zone|1|0|18|450'])).toEqual(['Vùng đai nhịp 2 · 18 đai']);
  });
});

describe('plain section card', () => {
  it("shows the middle cut of the picked span, the nearest cut of a support, the first span's when nothing is picked", () => {
    const at = (column: number) => selectedCut(buildScene(elevation, viewport, W, H, column), rebar.cuts, stationMap)!;
    const spanCuts = rebar.cuts.filter((c) => c.spanIndex === 0);
    expect(at(-1).number).toBe(spanCuts[Math.floor(spanCuts.length / 2)]!.number);
    expect(at(1)).toEqual(at(-1));
    expect(at(2).x).toBeGreaterThan(9000);
  });
});

describe('section drawing', () => {
  it('turns a bulge of 1 into a half circle and −0.414 into a clockwise quarter (drawing z up)', () => {
    const p = (x: number, z: number): [number, number] => [x, -z];
    expect(bulgePath([{ x: 0, z: 0, bulge: 1 }, { x: 10, z: 0, bulge: 0 }], p)).toBe('M0 0 A5 5 0 0 0 10 0');
    const quarter = bulgePath([{ x: 0, z: 0, bulge: -Math.tan(Math.PI / 8) }, { x: 10, z: 10, bulge: 0 }], p);
    expect(quarter).toMatch(/^M0 0 A10 10 0 0 1 10 -10$/);
  });

  it("bends the stirrups as Kata's drawing does: hooks over the top bars, corners rounded inside the hoop", () => {
    // Drawing mm straight to screen px, z up: the arcs' midpoints are read back from the SVG path and turned to mm.
    const p = (x: number, z: number): [number, number] => [x, -z];
    const stirrups = section.lines.filter((l) => l.pen === 'stirrup');
    const hoop = stirrups.find((l) => l.vertices.length === 12)!;
    const u = stirrups.find((l) => l.vertices.length === 10)!;
    const topBar = Math.max(...section.bars.map((b) => b.z));

    // The U's 180° hook (half circle, bulge 1, from its tail leg to the long leg) passes over the top bar's centre.
    const hook = arcMidpoints(bulgePath(u.vertices, p))[0]!;
    expect(-hook[1]).toBeGreaterThan(topBar);
    // The hoop's top-left corner (bulge −0.414 from the left leg to the top) bends toward the outer corner.
    const left = Math.min(...hoop.vertices.map((v) => v.x));
    const top = Math.max(...hoop.vertices.map((v) => v.z));
    const corner = arcMidpoints(bulgePath(hoop.vertices, p)).find(([x, y]) => x < left + 10 && -y > top - 10)!;
    expect(corner).toBeDefined();
  });

  it('fits the section in the panel and zooms about the cursor', () => {
    const frame = sectionFrame(section, 350, 600, FITTED)!;
    const [left] = frame.point(section.minX, 0);
    const [right] = frame.point(section.maxX, 0);
    expect(right - left).toBeLessThanOrEqual(350 - 28 + 1e-6);
    const zoomed = zoomSection(FITTED, 2, 175, 300, 350, 600);
    expect(zoomed).toEqual({ zoom: 2, panX: 0, panY: 0 });
  });

  it('draws every cut bar with its ring, every tag and the title', () => {
    const shapes = paintSection(section, paletteFor('dark'), 350, 600, FITTED);
    const palette = paletteFor('dark');
    expect(shapes.filter((s) => s.t === 'circle' && s.pen.color === palette.kata.bar)).toHaveLength(section.bars.length);
    const texts = shapes.flatMap((s) => (s.t === 'ctext' ? [s.text] : []));
    expect(texts).toEqual(expect.arrayContaining(section.tags.map((t) => t.text)));
    expect(texts).toContain(section.title.name);
    expect(shapes.filter((s) => s.t === 'path')).toHaveLength(section.lines.length);
  });
});

/** The midpoint of every arc of an SVG path made of M / L / A (circular arcs, no rotation), per the SVG arc rules. */
function arcMidpoints(d: string): [number, number][] {
  const out: [number, number][] = [];
  let at: [number, number] = [0, 0];
  for (const [, cmd, args] of d.matchAll(/([MLA])([^MLA]*)/g)) {
    const n = args!.trim().split(/[\s,]+/).map(Number);
    if (cmd !== 'A') {
      at = [n[0]!, n[1]!];
      continue;
    }
    const [r, , , large, sweep, x2, y2] = n as [number, number, number, number, number, number, number];
    const [x1, y1] = at;
    const hx = (x1 - x2) / 2;
    const hy = (y1 - y2) / 2;
    const k = Math.sqrt(Math.max(0, (r * r - hx * hx - hy * hy) / (hx * hx + hy * hy))) * (large === sweep ? -1 : 1);
    const cx = k * hy + (x1 + x2) / 2;
    const cy = -k * hx + (y1 + y2) / 2;
    const a1 = Math.atan2(y1 - cy, x1 - cx);
    let delta = Math.atan2(y2 - cy, x2 - cx) - a1;
    if (sweep === 1 && delta < 0) delta += 2 * Math.PI;
    if (sweep === 0 && delta > 0) delta -= 2 * Math.PI;
    out.push([cx + r * Math.cos(a1 + delta / 2), cy + r * Math.sin(a1 + delta / 2)]);
    at = [x2, y2];
  }
  return out;
}
