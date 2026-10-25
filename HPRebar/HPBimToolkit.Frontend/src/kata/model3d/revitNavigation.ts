// How the 3D view moves, as Revit's 3D view does: the 26 ViewCube directions (6 faces, 12 edges, 8 corners), fit to
// view for the perspective and the orthographic camera, and orbit around a pivot (Shift + middle drag). Pure maths on
// three's Vector3, so it is tested without WebGL. Model space: X along the beam, Y across it, Z up; north = +Y.
import { Vector3 } from 'three';
import type { KataWebPoint3 } from '../kataScene';
import type { Bounds3 } from './model3dGeometry';

/** One ViewCube direction: each component −1, 0 or +1 (not all 0). The camera stands on that side of the model. */
export type CubeRegion = readonly [number, number, number];

/** Revit's default {3D} view: from the top, front and right. */
export const HOME_REGION: CubeRegion = [1, -1, 1];

/** Room left around the model when it is fitted, as a share of the view. */
const MARGIN = 1.08;
/** Straight down or up the camera leans this much toward −Y, so north stays up the screen (Z is the camera's up). */
const POLE_TILT = 1e-3;
/** The camera never orbits closer to the vertical than this (radians): past the pole the view would flip. */
const MIN_POLAR = 0.01;

const FACE_NAMES: Record<string, string> = { '0,0,1': 'TOP', '0,0,-1': 'BOTTOM', '0,-1,0': 'FRONT', '0,1,0': 'BACK', '1,0,0': 'RIGHT', '-1,0,0': 'LEFT' };

/** "TOP", "FRONT RIGHT", "TOP FRONT RIGHT": the faces a region touches, vertical first, as Revit names them. */
export function regionName(region: CubeRegion): string {
  const [x, y, z] = region;
  return [z ? FACE_NAMES[`0,0,${z}`] : '', y ? FACE_NAMES[`0,${y},0`] : '', x ? FACE_NAMES[`${x},0,0`] : ''].filter(Boolean).join(' ');
}

/** Every one of the 26 regions. */
export const CUBE_REGIONS: CubeRegion[] = [-1, 0, 1].flatMap((x) => [-1, 0, 1].flatMap((y) => [-1, 0, 1].map((z) => [x, y, z] as const))).filter((r) => r.some((c) => c !== 0));

/** The unit direction from the model toward the camera for a region (the poles lean so north stays up). */
export function regionDirection(region: CubeRegion): Vector3 {
  const [x, y, z] = region;
  const pole = x === 0 && y === 0;
  return new Vector3(x, pole ? -POLE_TILT : y, z).normalize();
}

/** The region a direction looks from, when it is one (within 1°); null for any other direction. */
export function regionOfDirection(direction: Vector3): CubeRegion | null {
  const d = direction.clone().normalize();
  return CUBE_REGIONS.find((r) => regionDirection(r).angleTo(d) < (1 * Math.PI) / 180) ?? null;
}

/**
 * The region of a point on the ViewCube's surface (a cube of side 1 at the origin): the face it lies on, plus each
 * other axis whose coordinate is in the outer band (|c| > 0.3) — Revit's edges and corners.
 */
export function regionAt(point: Vector3): CubeRegion {
  const c = [point.x, point.y, point.z];
  const face = c.map((v) => Math.abs(v)).indexOf(Math.max(...c.map((v) => Math.abs(v))));
  return c.map((v, i) => (i === face ? Math.sign(v) : Math.abs(v) > 0.3 ? Math.sign(v) : 0)) as unknown as CubeRegion;
}

export interface CameraFit {
  position: KataWebPoint3;
  target: KataWebPoint3;
  /** Half the height the orthographic camera must show at zoom 1. */
  orthoHalfHeight: number;
}

/**
 * Where the camera stands to look at the whole of `bounds` from `toward`: for the perspective camera far enough that
 * a vertical field of view of `fovDeg` at `aspect` holds every corner; for the orthographic one, the half height that
 * holds them (it stands at the same place, outside the model).
 */
export function fitCamera(toward: Vector3, bounds: Bounds3, fovDeg: number, aspect: number): CameraFit {
  const target = new Vector3((bounds.min.x + bounds.max.x) / 2, (bounds.min.y + bounds.max.y) / 2, (bounds.min.z + bounds.max.z) / 2);
  const dir = toward.clone().normalize();
  const right = new Vector3(0, 0, 1).cross(dir).normalize();
  const up = dir.clone().cross(right).normalize();
  const tan = Math.tan(((fovDeg / 2) * Math.PI) / 180);
  const wide = Math.max(aspect, 0.1);
  let distance = 0;
  let half = 0;
  for (const x of [bounds.min.x, bounds.max.x])
    for (const y of [bounds.min.y, bounds.max.y])
      for (const z of [bounds.min.z, bounds.max.z]) {
        const corner = new Vector3(x, y, z).sub(target);
        const across = Math.max(Math.abs(corner.dot(up)), Math.abs(corner.dot(right)) / wide);
        half = Math.max(half, across);
        // A corner nearer the camera shortens the distance it needs.
        distance = Math.max(distance, (across / tan) * MARGIN + corner.dot(dir));
      }
  const position = target.clone().addScaledVector(dir, distance);
  return { position: toPoint(position), target: toPoint(target), orthoHalfHeight: half * MARGIN };
}

/**
 * Orbits the camera and its target around `pivot`: `azimuth` radians about the vertical, then `elevation` radians up
 * (positive = the camera rises), never past the vertical. The view turns with the camera, as in Revit.
 */
export function orbitAbout(position: Vector3, target: Vector3, pivot: Vector3, azimuth: number, elevation: number): { position: Vector3; target: Vector3 } {
  const z = new Vector3(0, 0, 1);
  const p = position.clone().sub(pivot).applyAxisAngle(z, azimuth);
  const t = target.clone().sub(pivot).applyAxisAngle(z, azimuth);
  const view = p.clone().sub(t).normalize();
  const polar = Math.acos(Math.max(-1, Math.min(1, view.z)));
  const turn = Math.max(MIN_POLAR - polar, Math.min(Math.PI - MIN_POLAR - polar, -elevation));
  const right = z.clone().cross(view).normalize();
  if (right.lengthSq() > 0 && turn !== 0) {
    // About z × view, a positive angle tips the camera down: the turn is the change of the polar angle.
    p.applyAxisAngle(right, turn);
    t.applyAxisAngle(right, turn);
  }
  return { position: p.add(pivot), target: t.add(pivot) };
}

/** The compass letters on the ViewCube's ring and the side each one looks from. */
export const COMPASS: { letter: string; region: CubeRegion }[] = [
  { letter: 'N', region: [0, 1, 0] },
  { letter: 'E', region: [1, 0, 0] },
  { letter: 'S', region: [0, -1, 0] },
  { letter: 'W', region: [-1, 0, 0] },
];

function toPoint(v: Vector3): KataWebPoint3 {
  return { x: v.x, y: v.y, z: v.z };
}
