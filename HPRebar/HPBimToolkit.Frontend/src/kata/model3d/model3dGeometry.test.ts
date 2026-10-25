// The 3D view's geometry on the B03 fixture: every bar of the plan, tubes along the bar, framing and labels.
import { PerspectiveCamera, Vector3 } from 'three';
import { describe, expect, it } from 'vitest';
import fixture from '../__fixtures__/kata-model3d-b03.json';
import type { KataWeb3dModel } from '../kataScene';
import { barGeometry, barSegments, describeGroup, modelBounds, presetCamera, roleColor } from './model3dGeometry';

const model = fixture as unknown as KataWeb3dModel;

describe('the 3D model', () => {
  it('holds every bar the plan counts, keyed like the elevation for what a click can remove', () => {
    expect(model.groups.reduce((sum, g) => sum + g.offsets.length, 0)).toBe(model.barCount);
    expect(model.groups.filter((g) => g.role === 'mainTop').every((g) => g.key.startsWith('bar|'))).toBe(true);
    expect(model.groups.filter((g) => g.role === 'stirrupClosed').every((g) => g.key.startsWith('zone|'))).toBe(true);
  });
});

describe('bar tubes', () => {
  it('close a loop back to its first point and drop zero-length segments', () => {
    const square = [
      { x: 0, y: 0, z: 0 },
      { x: 0, y: 100, z: 0 },
      { x: 0, y: 100, z: 0 },
      { x: 0, y: 100, z: -100 },
    ];
    expect(barSegments(square, false)).toHaveLength(2);
    expect(barSegments(square, true)).toHaveLength(3);
  });

  it('follow the bar within its radius', () => {
    const stirrup = model.groups.find((g) => g.role === 'stirrupClosed')!;
    const geometry = barGeometry(stirrup);
    geometry.computeBoundingBox();
    const box = geometry.boundingBox!;
    const ys = stirrup.points.map((p) => p.y);
    const zs = stirrup.points.map((p) => p.z);
    const r = stirrup.diameter / 2;
    expect(box.min.y).toBeCloseTo(Math.min(...ys) - r, 0);
    expect(box.max.y).toBeCloseTo(Math.max(...ys) + r, 0);
    expect(box.min.z).toBeCloseTo(Math.min(...zs) - r, 0);
    expect(box.max.z).toBeCloseTo(Math.max(...zs) + r, 0);
  });
});

describe('framing', () => {
  const bounds = modelBounds(model);

  it('takes in the concrete, the columns and every stirrup offset', () => {
    expect(bounds.min.x).toBeLessThanOrEqual(0);
    expect(bounds.max.x).toBeGreaterThanOrEqual(model.length - 1);
    expect(bounds.max.z).toBeGreaterThan(0); // the column stubs stand above the beam
  });

  it('looks at the middle of the run from the side the preset names', () => {
    const front = presetCamera('front', bounds, 35, 2);
    expect(front.target.x).toBeCloseTo((bounds.min.x + bounds.max.x) / 2, 6);
    expect(front.position.y).toBeLessThan(bounds.min.y);
    expect(presetCamera('top', bounds, 35, 2).position.z).toBeGreaterThan(bounds.max.z);
    expect(presetCamera('end', bounds, 35, 2).position.x).toBeLessThan(bounds.min.x);
    // The whole 33.6 m run fits across a wide view: the camera stands at least half its length away.
    expect(front.target.y - front.position.y).toBeGreaterThan(model.length / 2);
  });
});

describe('preset framing', () => {
  const bounds = modelBounds(model);

  it.each(['iso', 'front', 'top', 'end'] as const)('%s holds the whole model, filling the view', (preset) => {
    const aspect = 879 / 369; // the drawing area in Revit's Kata window
    const { position, target } = presetCamera(preset, bounds, 35, aspect);
    const camera = new PerspectiveCamera(35, aspect, 10, 1e7);
    camera.up.set(0, 0, 1);
    camera.position.set(position.x, position.y, position.z);
    camera.lookAt(target.x, target.y, target.z);
    camera.updateMatrixWorld();
    let edge = 0;
    for (const x of [bounds.min.x, bounds.max.x])
      for (const y of [bounds.min.y, bounds.max.y])
        for (const z of [bounds.min.z, bounds.max.z]) {
          const p = new Vector3(x, y, z).project(camera);
          expect(Math.abs(p.x)).toBeLessThanOrEqual(1);
          expect(Math.abs(p.y)).toBeLessThanOrEqual(1);
          edge = Math.max(edge, Math.abs(p.x), Math.abs(p.y));
        }
    expect(edge).toBeGreaterThan(0.8);
  });
});

describe('labels', () => {
  it('say what a bar group is, its count and number', () => {
    const stirrup = model.groups.find((g) => g.role === 'stirrupClosed')!;
    expect(describeGroup(stirrup)).toBe(`Đai kín Ø${stirrup.diameter} × ${stirrup.offsets.length} · số hiệu ${stirrup.number}`);
    expect(roleColor('mainTop', true)).not.toBe(roleColor('mainBottom', true));
  });
});
