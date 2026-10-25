// Kata's elevation tags (ports of KataElevationBarTagPainter.cs and KataCadTag.cs): kata_block_KHT on its LEADER in
// model millimetres scaled like CAD — an arrow on each bar, the leader up (or down) to its row and along it, the text on
// the leader before the insertion point, the number circles touching it beyond; the stirrup tag is one line centred on
// its row (state T13) with its circle after it.

import type { KataWebRebar, KataWebStationMap } from '../kataScene';
import { TAG, arrow, cadText, cadTextCentredOn } from './kataCad';
import type { Palette } from './palette';
import { isVisible, sx, sy, type Scene } from './scene';
import { ShapeList, type Shape } from './shapes';

export function paintTags(scene: Scene, palette: Palette, rebar: KataWebRebar, map: KataWebStationMap): Shape[] {
  const out = new ShapeList();
  const s = scene.viewport.scale;
  const k = palette.kata;
  const leader = leaderPen(palette);
  const X = (localX: number) => sx(scene, map.origin + map.direction * localX);
  const Y = (z: number) => sy(scene, scene.elevation.topMm + z);

  for (const tag of rebar.tags) {
    const x = X(tag.x);
    const rowY = Y(tag.rowZ);
    // Kata's stirrup tag sits 125 mm right of its zone's middle as drawn, whichever way the run is listed.
    const insertX = tag.kind === 'stirrups' ? X(tag.x - TAG.stirrupShift) + TAG.stirrupShift * s : X(tag.insertX);
    const reach = (TAG.leaderPerChar * tag.text.length + 4 * TAG.circleRadius) * s;
    if (!isVisible(scene, Math.min(x, insertX) - reach, Math.max(x, insertX) + reach)) continue;

    if (tag.kind === 'stirrups') {
      cadTextCentredOn(out, tag.text, k.tagText, TAG.textHeight, s, insertX - TAG.textGapRight * s, rowY, 'right');
      tagCircles(out, palette, s, tag.numbers, insertX, rowY, true);
      continue;
    }

    for (const z of tag.footZ) {
      const footY = Y(z);
      out.line(leader, x, footY, x, rowY);
      arrow(out, k.leader, x, footY, x, rowY, TAG.arrowSize * s);
    }
    out.line(leader, x, rowY, insertX, rowY);

    // On screen the tag may run the other way than in the layout (the drawing lists the run backwards).
    tagOnLeader(out, palette, s, tag.text, tag.numbers, insertX, rowY, insertX >= x);
  }
  return out.shapes;
}

/** KataCanvasPalette's leader and circle pens: 1 px wide, not device-pixel lineweights. */
export const leaderPen = (palette: Palette) => ({ color: palette.kata.leader, width: 1 });

/**
 * kata_block_KHT at the end of a leader: `right` when the leader runs right to it — the text before the insertion
 * point, the circles after it; a stirrup's `spacing` under the leader (states P11, P21, T11, T21, P12).
 */
export function tagOnLeader(out: ShapeList, palette: Palette, s: number, text: string, numbers: number[], insertX: number, rowY: number, right: boolean, spacing = ''): void {
  const x = right ? insertX - TAG.textGapRight * s : insertX + TAG.textGapLeft * s;
  const align = right ? 'right' : 'left';
  cadText(out, text, palette.kata.tagText, TAG.textHeight, s, x, rowY - TAG.textLift * s, align);
  cadText(out, spacing, palette.kata.tagText, TAG.textHeight, s, x, rowY + TAG.spacingDrop * s, align);
  tagCircles(out, palette, s, numbers, insertX, rowY, right);
}

/** Touching circles beyond the insertion point, the numbers in reading order left to right (Kata's SH3 SH4 / SH1 SH2). */
export function tagCircles(out: ShapeList, palette: Palette, s: number, numbers: number[], insertX: number, rowY: number, right: boolean): void {
  const r = TAG.circleRadius * s;
  const ring = { color: palette.kata.circle, width: 1 };
  numbers.forEach((n, i) => {
    const cx = right ? insertX + r * (2 * i + 1) : insertX - r * (2 * (numbers.length - i) - 1);
    out.circle(undefined, ring, cx, rowY, r);
    cadTextCentredOn(out, String(n), palette.kata.number, TAG.textHeight, s, cx, rowY, 'centre');
  });
}

/** Room the tags take over the highest beam top and under the band bottom `bandDepth` below it (KataBarTagBuilder.Band), mm. */
export function tagBand(rebar: KataWebRebar, bandDepth: number): { above: number; below: number } {
  let above = 0;
  let below = 0;
  for (const t of rebar.tags) {
    if (t.above) above = Math.max(above, t.rowZ + TAG.circleRadius);
    else below = Math.max(below, -t.rowZ - bandDepth + TAG.circleRadius);
  }
  return { above, below };
}
