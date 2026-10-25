// The web-only tools over Kata's elevation, as pure functions: layers to show or hide, measuring between two points
// snapped to the drawing, and finding a bar number among the tags.

import type { KataWebRebar, KataWebStationMap } from '../kataScene';
import type { Point } from './kataElevationPainter';
import type { Scene } from './scene';
import type { LayerId, Shape } from './shapes';
import { toStation } from './viewport';

export const LAYERS: { id: LayerId; label: string }[] = [
  { id: 'concrete', label: 'Bê tông' },
  { id: 'bars', label: 'Thép chủ / gia cường' },
  { id: 'stirrups', label: 'Thép đai' },
  { id: 'tags', label: 'Số hiệu thanh' },
  { id: 'dims', label: 'Kích thước' },
  { id: 'flags', label: 'Cờ mặt cắt' },
  { id: 'grid', label: 'Trục' },
  { id: 'notes', label: 'Cao độ, tên dầm' },
];

export const visibleShapes = (shapes: Shape[], hidden: ReadonlySet<LayerId>) =>
  hidden.size === 0 ? shapes : shapes.filter((s) => !s.layer || !hidden.has(s.layer));

/** A point of the drawing in millimetres: X along the layout (local), Z up from the beam top. */
export interface ModelPoint {
  x: number;
  z: number;
}

/** The drawing point under a screen point (inverse of kataPoint). */
export function toModel(scene: Scene, map: KataWebStationMap, [sx, sy]: Point): ModelPoint {
  const station = toStation(scene.viewport, sx);
  return { x: (station - map.origin) / map.direction, z: (scene.viewport.offsetY - sy) / scene.viewport.scale };
}

/** Snapping reach of the measure tool (px). */
export const SNAP_PX = 8;

/**
 * The drawing vertex nearest `at` within SNAP_PX — the points of Kata's lines and the feet of its dimensions — or the
 * point under the cursor itself when none is that close.
 */
export function snap(rebar: KataWebRebar | null, scene: Scene, map: KataWebStationMap, point: (x: number, z: number) => Point, at: Point): { model: ModelPoint; snapped: boolean } {
  let best: ModelPoint | null = null;
  let distance = SNAP_PX;
  const consider = (x: number, z: number) => {
    const [px, py] = point(x, z);
    const d = Math.hypot(px - at[0], py - at[1]);
    if (d <= distance) {
      distance = d;
      best = { x, z };
    }
  };
  for (const line of rebar?.lines ?? []) for (const q of line.points) consider(q.x, q.z);
  for (const dim of rebar?.dims ?? []) {
    consider(dim.x1, dim.z1);
    consider(dim.x2, dim.z2);
  }
  return best ? { model: best, snapped: true } : { model: toModel(scene, map, at), snapped: false };
}

/** "Δx 1 234 · Δz 56 · L 1 235 mm" between two drawing points. */
export function describeMeasure(a: ModelPoint, b: ModelPoint): string {
  const n = (v: number) => Math.round(v).toLocaleString('vi-VN');
  return `Δx ${n(Math.abs(b.x - a.x))} · Δz ${n(Math.abs(b.z - a.z))} · L ${n(Math.hypot(b.x - a.x, b.z - a.z))} mm`;
}

/** Every tag carrying bar `number`, left to right in the layout: where it is and the bars it points at. */
export function findBar(rebar: KataWebRebar, number: number) {
  return rebar.tags
    .filter((t) => t.numbers.includes(number))
    .map((t) => ({ x: t.x, insertX: t.insertX, rowZ: t.rowZ, footZ: t.footZ }))
    .sort((a, b) => a.x - b.x);
}
