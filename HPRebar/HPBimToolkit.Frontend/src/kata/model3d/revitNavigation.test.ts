// Revit-style navigation on the B03 fixture: ViewCube regions, fit to view for both cameras, orbit about a pivot.
import { OrthographicCamera, PerspectiveCamera, Vector3 } from 'three';
import { describe, expect, it } from 'vitest';
import fixture from '../__fixtures__/kata-model3d-b03.json';
import type { KataWeb3dModel } from '../kataScene';
import { modelBounds } from './model3dGeometry';
import { CUBE_REGIONS, HOME_REGION, fitCamera, orbitAbout, regionAt, regionDirection, regionName, regionOfDirection, type CubeRegion } from './revitNavigation';

const model = fixture as unknown as KataWeb3dModel;
const bounds = modelBounds(model);
const ASPECT = 879 / 369; // the drawing area in Revit's Kata window
const corners = () =>
  [bounds.min.x, bounds.max.x].flatMap((x) => [bounds.min.y, bounds.max.y].flatMap((y) => [bounds.min.z, bounds.max.z].map((z) => new Vector3(x, y, z))));

describe('ViewCube regions', () => {
  it('are the 6 faces, 12 edges and 8 corners, named as Revit names them', () => {
    expect(CUBE_REGIONS).toHaveLength(26);
    expect(regionName([0, 0, 1])).toBe('TOP');
    expect(regionName([0, -1, 0])).toBe('FRONT');
    expect(regionName([1, -1, 0])).toBe('FRONT RIGHT');
    expect(regionName(HOME_REGION)).toBe('TOP FRONT RIGHT');
  });

  it('tell the face, edge or corner a point of the cube surface lies on', () => {
    expect(regionAt(new Vector3(0.05, -0.1, 0.5))).toEqual([0, 0, 1]);
    expect(regionAt(new Vector3(0.45, -0.1, 0.5))).toEqual([1, 0, 1]);
    expect(regionAt(new Vector3(0.45, -0.42, 0.5))).toEqual([1, -1, 1]);
    expect(regionAt(new Vector3(-0.5, 0, -0.35))).toEqual([-1, 0, -1]);
  });

  it('look from their side, the poles with north up, and are found again from the direction', () => {
    for (const region of CUBE_REGIONS) expect(regionOfDirection(regionDirection(region))).toEqual(region);
    const top = regionDirection([0, 0, 1]);
    expect(top.z).toBeGreaterThan(0.999);
    expect(top.y).toBeLessThan(0); // standing a hair south, the camera has north (+Y) up the screen
    expect(regionOfDirection(new Vector3(1, -0.5, 0.2))).toBeNull();
  });
});

describe('fit to view', () => {
  it.each(([HOME_REGION, [0, -1, 0], [0, 0, 1], [-1, 0, 0], [1, 1, -1]] as CubeRegion[]).map((r) => [regionName(r), r] as const))('from %s holds the whole model in both cameras', (_name, region) => {
    const fit = fitCamera(regionDirection(region), bounds, 35, ASPECT);
    const perspective = new PerspectiveCamera(35, ASPECT, 10, 1e7);
    const ortho = new OrthographicCamera(-fit.orthoHalfHeight * ASPECT, fit.orthoHalfHeight * ASPECT, fit.orthoHalfHeight, -fit.orthoHalfHeight, 0.1, 1e7);
    let edge = 0;
    for (const camera of [perspective, ortho]) {
      camera.up.set(0, 0, 1);
      camera.position.set(fit.position.x, fit.position.y, fit.position.z);
      camera.lookAt(fit.target.x, fit.target.y, fit.target.z);
      camera.updateMatrixWorld();
      for (const c of corners()) {
        const p = c.clone().project(camera);
        expect(Math.abs(p.x)).toBeLessThanOrEqual(1);
        expect(Math.abs(p.y)).toBeLessThanOrEqual(1);
        edge = Math.max(edge, Math.abs(p.x), Math.abs(p.y));
      }
    }
    expect(edge).toBeGreaterThan(0.8);
  });
});

describe('orbit about a pivot', () => {
  const pivot = new Vector3(1000, 0, -500);
  const position = new Vector3(1000, -20000, 5000);
  const target = new Vector3(1000, 0, 0);

  it('keeps the distances to the pivot and turns the camera and its target together', () => {
    const r = orbitAbout(position, target, pivot, Math.PI / 2, 0.3);
    expect(r.position.distanceTo(pivot)).toBeCloseTo(position.distanceTo(pivot), 3);
    expect(r.target.distanceTo(pivot)).toBeCloseTo(target.distanceTo(pivot), 3);
    expect(r.position.distanceTo(r.target)).toBeCloseTo(position.distanceTo(target), 3);
  });

  it('a quarter turn about the vertical brings the camera from the south side to the east side', () => {
    const r = orbitAbout(position, target, pivot, Math.PI / 2, 0);
    expect(r.position.x).toBeGreaterThan(pivot.x + 19000);
  });

  it('raising the camera lifts it, and it never passes over the top', () => {
    const view = (p: Vector3, t: Vector3) => p.clone().sub(t).normalize();
    const up = orbitAbout(position, target, pivot, 0, 0.2);
    expect(view(up.position, up.target).z).toBeGreaterThan(view(position, target).z);
    const over = orbitAbout(position, target, pivot, 0, 10);
    const v = view(over.position, over.target);
    expect(v.z).toBeLessThan(1);
    expect(v.y).toBeLessThan(0); // still on the south side
  });
});
