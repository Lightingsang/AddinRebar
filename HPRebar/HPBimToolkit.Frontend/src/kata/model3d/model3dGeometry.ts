// Geometry of the 3D view, kept free of WebGL so it can be tested: bar tubes merged from one cylinder per segment
// (a corner stays sharp, a tube along a curve would cut it), concrete boxes, colours by role, the model's bounds.
// Model space is the plan's own: X along the beam, Y across it, Z up — the camera's up is +Z.
import { BoxGeometry, BufferGeometry, CylinderGeometry, Matrix4, Quaternion, SphereGeometry, Vector3 } from 'three';
import { mergeGeometries } from 'three/examples/jsm/utils/BufferGeometryUtils.js';
import type { KataBarRole, KataWeb3dBox, KataWeb3dGroup, KataWeb3dModel, KataWebPoint3 } from '../kataScene';
import { KATA_STRINGS, type KataStrings } from '../i18n';

const RADIAL_SEGMENTS = 8;
const UP = new Vector3(0, 1, 0);

/** The segments a bar is drawn with: its polyline, closed back to the first point when the bar is a loop. */
export function barSegments(points: KataWebPoint3[], closed: boolean): [KataWebPoint3, KataWebPoint3][] {
  const segments: [KataWebPoint3, KataWebPoint3][] = [];
  for (let i = 0; i + 1 < points.length; i++) segments.push([points[i]!, points[i + 1]!]);
  if (closed && points.length > 2) segments.push([points[points.length - 1]!, points[0]!]);
  return segments.filter(([a, b]) => Math.hypot(b.x - a.x, b.y - a.y, b.z - a.z) > 0.01);
}

/** One bar as a tube of its diameter: a cylinder per segment and a ball at each bend, merged into one geometry. */
export function barGeometry(group: KataWeb3dGroup): BufferGeometry {
  const radius = Math.max(group.diameter, 4) / 2;
  const parts: BufferGeometry[] = [];
  const segments = barSegments(group.points, group.closed);
  segments.forEach(([a, b], i) => {
    const start = new Vector3(a.x, a.y, a.z);
    const direction = new Vector3(b.x, b.y, b.z).sub(start);
    const length = direction.length();
    const cylinder = new CylinderGeometry(radius, radius, length, RADIAL_SEGMENTS, 1, true);
    const rotation = new Quaternion().setFromUnitVectors(UP, direction.clone().normalize());
    cylinder.applyMatrix4(new Matrix4().compose(start.clone().addScaledVector(direction, 0.5), rotation, new Vector3(1, 1, 1)));
    parts.push(cylinder);
    if (i > 0 || group.closed) parts.push(new SphereGeometry(radius, RADIAL_SEGMENTS, 4).translate(a.x, a.y, a.z));
  });
  if (parts.length === 0) return new BufferGeometry();
  const merged = mergeGeometries(parts, false) ?? new BufferGeometry();
  parts.forEach((p) => p.dispose());
  return merged;
}

export function boxGeometry(box: KataWeb3dBox): BufferGeometry {
  return new BoxGeometry(box.x2 - box.x1, box.y2 - box.y1, box.z2 - box.z1).translate((box.x1 + box.x2) / 2, (box.y1 + box.y2) / 2, (box.z1 + box.z2) / 2);
}

/** Colours by role, as Kata's rebar drawing tells layers apart: main bars warm, bottom bars cool, stirrups grey. */
export function roleColor(role: KataBarRole, dark: boolean): number {
  switch (role) {
    case 'mainTop':
      return 0xe0641e;
    case 'extraTop':
      return 0xf2a33a;
    case 'mainBottom':
      return 0x2f7fd8;
    case 'extraBottom':
      return 0x35b6c9;
    case 'sideBar':
      return 0x3fa34d;
    case 'hangerBar':
      return 0xc04fc0;
    case 'crossTie':
      return 0x9a7fd1;
    case 'stirrupClosed':
    case 'stirrupCap':
      return dark ? 0xb8bcc4 : 0x6b7079;
  }
}

export const SELECTED_COLOR = 0xffd400;
export const HOVER_COLOR = 0xfff2a6;

export function isStirrup(role: KataBarRole): boolean {
  return role === 'stirrupClosed' || role === 'stirrupCap' || role === 'crossTie';
}

/** Vietnamese names of the roles, as the tooltip and the legend write them. */
export const ROLE_NAMES: Record<KataBarRole, string> = KATA_STRINGS.vi.roles3d;

/** What the tooltip says about a bar group, in the page's language. */
export function describeGroup(group: KataWeb3dGroup, t: KataStrings = KATA_STRINGS.vi): string {
  const count = group.offsets.length > 1 ? ` × ${group.offsets.length}` : '';
  const number = group.number > 0 ? t.barNumber(group.number) : '';
  return `${t.roles3d[group.role]} Ø${group.diameter}${count}${number}`;
}

export interface Bounds3 {
  min: KataWebPoint3;
  max: KataWebPoint3;
}

/** Everything drawn: bars at all their offsets and the concrete. */
export function modelBounds(model: KataWeb3dModel): Bounds3 {
  const min = { x: Infinity, y: Infinity, z: Infinity };
  const max = { x: -Infinity, y: -Infinity, z: -Infinity };
  const add = (x: number, y: number, z: number) => {
    min.x = Math.min(min.x, x);
    min.y = Math.min(min.y, y);
    min.z = Math.min(min.z, z);
    max.x = Math.max(max.x, x);
    max.y = Math.max(max.y, y);
    max.z = Math.max(max.z, z);
  };
  for (const box of model.concrete) {
    add(box.x1, box.y1, box.z1);
    add(box.x2, box.y2, box.z2);
  }
  for (const group of model.groups) {
    const low = Math.min(...group.offsets);
    const high = Math.max(...group.offsets);
    for (const p of group.points) {
      add(p.x + low, p.y, p.z);
      add(p.x + high, p.y, p.z);
    }
  }
  if (!Number.isFinite(min.x)) return { min: { x: 0, y: 0, z: 0 }, max: { x: 1, y: 1, z: 1 } };
  return { min, max };
}
