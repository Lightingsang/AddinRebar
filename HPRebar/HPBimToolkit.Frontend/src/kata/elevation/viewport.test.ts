// The same cases as HPRebar.Core.Tests/KataExport/KataElevationViewportTests.cs: the page must frame and zoom exactly
// as the WPF canvas did.
import { describe, expect, it } from 'vitest';
import { centreBand, ensureVisible, fit, panBy, shrinkToHeight, toScreen, toScreenY, toStation, zoomAt, type Viewport } from './viewport';

const v = (scale: number, offsetX: number, offsetY = 0): Viewport => ({ scale, offsetX, offsetY });

describe('viewport', () => {
  it('fit centres the range inside the margins', () => {
    const viewport = fit({ start: 0, end: 10000 }, 1040, 20);
    expect(viewport.scale).toBeCloseTo(0.1, 9);
    expect(toScreen(viewport, 0)).toBeCloseTo(20, 9);
    expect(toScreen(viewport, 10000)).toBeCloseTo(1020, 9);
    const away = fit({ start: 4000, end: 6000 }, 400, 0);
    expect(toScreen(away, 4000)).toBeCloseTo(0, 9);
    expect(toScreen(away, 6000)).toBeCloseTo(400, 9);
  });

  it.each([0.02, 0.5])('one millimetre is as long across as down at scale %s', (scale) => {
    const viewport = panBy(zoomAt(v(scale, 10, 20), 1.25, 300, 180, 0.001, 50), 7, -3);
    expect(toScreen(viewport, 1000) - toScreen(viewport, 0)).toBeCloseTo(toScreenY(viewport, 1000) - toScreenY(viewport, 0), 9);
  });

  it('zoom keeps the point under the cursor still in both directions', () => {
    const viewport = v(0.02, 20, 100);
    const station = toStation(viewport, 300);
    const depth = (180 - viewport.offsetY) / viewport.scale;
    for (const factor of [2, 20, 0.5]) {
      const zoomed = zoomAt(viewport, factor, 300, 180, 0.01, 3);
      expect(toScreen(zoomed, station)).toBeCloseTo(300, 9);
      expect(toScreenY(zoomed, depth)).toBeCloseTo(180, 9);
    }
  });

  it('zoom respects the limits and keeps the anchor', () => {
    const viewport = v(0.1, 20, 100);
    const station = toStation(viewport, 300);
    const capped = zoomAt(viewport, 100, 300, 150, 0.05, 3);
    expect(capped.scale).toBeCloseTo(3, 9);
    expect(toScreen(capped, station)).toBeCloseTo(300, 9);
    expect(toScreenY(capped, (150 - viewport.offsetY) / viewport.scale)).toBeCloseTo(150, 9);
    expect(zoomAt(viewport, 0.001, 300, 150, 0.05, 3).scale).toBeCloseTo(0.05, 9);
  });

  it('a limit already passed stops only further movement that way', () => {
    const past = v(0.01, 0);
    expect(zoomAt(past, 0.8, 100, 0, 0.05, 3).scale).toBeCloseTo(0.01, 9);
    expect(zoomAt(past, 1.2, 100, 0, 0.05, 3).scale).toBeCloseTo(0.012, 9);
  });

  it('shrink to height zooms out a short span until its labels fit', () => {
    const framed = v(0.48, -100, 0);
    const station = toStation(framed, 500);
    const shrunk = shrinkToHeight(framed, 350, 326, 88, 150, 500);
    expect(shrunk.scale).toBeCloseTo(88 / 350, 9);
    expect(toScreen(shrunk, station)).toBeCloseTo(500, 9);
    expect(shrinkToHeight(framed, 350, 1000, 88, 150, 500)).toEqual(framed);
    expect(shrinkToHeight({ ...framed, scale: 0.2 }, 350, 326, 88, 150, 500)).toEqual({ ...framed, scale: 0.2 });
  });

  it('pan moves both directions', () => {
    expect(panBy(v(0.1, 20, 100), -30, 45)).toEqual(v(0.1, -10, 145));
  });

  it('centre band puts labels and band in the middle or keeps the top labels visible', () => {
    const viewport = v(0.02, 0, 0);
    expect(centreBand(viewport, 400, 400, 80, 150).offsetY).toBeCloseTo((400 - 238) / 2 + 80, 9);
    expect(centreBand(zoomAt(viewport, 100, 0, 0, 0.01, 3), 400, 400, 80, 150).offsetY).toBeCloseTo(80, 9);
  });

  it('ensure visible pans the least amount and keeps the vertical position', () => {
    const viewport = v(0.1, 0, 77);
    expect(ensureVisible(viewport, { start: 1000, end: 2000 }, 500, 10)).toEqual(viewport);
    expect(toScreen(ensureVisible(viewport, { start: 5000, end: 6000 }, 500, 10), 6000)).toBeCloseTo(490, 9);
    expect(toScreen(ensureVisible(panBy(viewport, -300), { start: 1000, end: 2000 }, 500, 10), 1000)).toBeCloseTo(10, 9);
    expect(ensureVisible(viewport, { start: 5000, end: 6000 }, 500, 10).offsetY).toBe(77);
  });

  it('a range wider than the view stays put while on screen and is centred otherwise', () => {
    const viewport = v(0.1, 0);
    expect(ensureVisible(viewport, { start: 2000, end: 9000 }, 500, 10)).toEqual(viewport);
    expect(toScreen(ensureVisible(viewport, { start: 8000, end: 15000 }, 500, 10), 11500)).toBeCloseTo(250, 9);
  });
});
