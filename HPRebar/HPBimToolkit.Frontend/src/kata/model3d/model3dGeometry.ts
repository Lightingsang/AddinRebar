// Geometry of the 3D view, kept free of WebGL so it can be tested: bar tubes merged from one cylinder per segment
// (a corner stays sharp, a tube along a curve would cut it), concrete boxes, colours by role, camera presets.
// Model space is the plan's own: X along the beam, Y across it, Z up — the camera's up is +Z.
import { BoxGeometry, BufferGeometry, CylinderGeometry, Matrix4, Quaternion, SphereGeometry, Vector3 } from 'three';
import { mergeGeometries } from 'three/examples/jsm/utils/BufferGeometryUtils.js';
import type { KataBarRole, KataWeb3dBox, KataWeb3dGroup, KataWeb3dModel, KataWebPoint3 } from '../kataScene';

const RADIAL_SEGMENTS = 8;
/** Room left around the model by a preset, as a share of the view. */
const MARGIN = 1.08;
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
export const ROLE_NAMES: Record<KataBarRole, string> = {
  mainTop: 'Thép chủ trên',
  mainBottom: 'Thép chủ dưới',
  extraTop: 'Thép gia cường trên',
  extraBottom: 'Thép gia cường dưới',
  sideBar: 'Thép giá',
  crossTie: 'Đai C / móc',
  stirrupClosed: 'Đai kín',
  stirrupCap: 'Đai U',
  hangerBar: 'Thép vai bò',
};

/** What the tooltip says about a bar group. */
export function describeGroup(group: KataWeb3dGroup): string {
  const count = group.offsets.length > 1 ? ` × ${group.offsets.length}` : '';
  const number = group.number > 0 ? ` · số hiệu ${group.number}` : '';
  return `${ROLE_NAMES[group.role]} Ø${group.diameter}${count}${number}`;
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

export type ViewPreset = 'iso' | 'front' | 'top' | 'end';

export const VIEW_PRESETS: { id: ViewPreset; label: string }[] = [
  { id: 'iso', label: 'Phối cảnh' },
  { id: 'front', label: 'Mặt đứng' },
  { id: 'top', label: 'Mặt bằng' },
  { id: 'end', label: 'Đầu dầm' },
];

/**
 * Where the camera stands for a preset, looking at the middle of `bounds` from just far enough that a vertical field of
 * view of `fovDeg` at `aspect` holds every corner of the bounds (each corner projected on the camera's own axes).
 */
export function presetCamera(preset: ViewPreset, bounds: Bounds3, fovDeg: number, aspect: number): { position: KataWebPoint3; target: KataWebPoint3 } {
  const target = { x: (bounds.min.x + bounds.max.x) / 2, y: (bounds.min.y + bounds.max.y) / 2, z: (bounds.min.z + bounds.max.z) / 2 };
  // From above the camera leans a hair toward -Y: straight down, with Z as its up, it would have no screen axes.
  const toward = new Vector3(...{ iso: [-0.55, -0.7, 0.45], front: [0, -1, 0], top: [0, -0.001, 1], end: [-1, 0, 0] }[preset]).normalize();
  const right = new Vector3(0, 0, 1).cross(toward).normalize();
  const up = toward.clone().cross(right).normalize();
  const tan = Math.tan(((fovDeg / 2) * Math.PI) / 180);
  let distance = 0;
  for (const x of [bounds.min.x, bounds.max.x])
    for (const y of [bounds.min.y, bounds.max.y])
      for (const z of [bounds.min.z, bounds.max.z]) {
        const corner = new Vector3(x - target.x, y - target.y, z - target.z);
        // The corner's depth toward the camera shortens the distance it needs.
        const near = corner.dot(toward);
        const across = Math.max(Math.abs(corner.dot(up)) / tan, Math.abs(corner.dot(right)) / (tan * Math.max(aspect, 0.1)));
        distance = Math.max(distance, across * MARGIN + near);
      }
  return { position: { x: target.x + toward.x * distance, y: target.y + toward.y * distance, z: target.z + toward.z * distance }, target };
}
