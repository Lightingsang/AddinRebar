// Picking on Kata's elevation (ports of KataElevationCanvas.Edit / KataElevationCadPainter.FlagHit): the bar or stirrup
// lines within a few pixels of a click, nearest first; the section flag under it; and a reading of the bar keys for
// the tooltip (KataLayoutRemoval.Key: "bar|Role|id|Ø|length|layer|y|x0", "zone|span|zone|count|first").

import type { KataWebFlag, KataWebLine } from '../kataScene';
import { STYLE, TAG } from './kataCad';
import { FLAG, type Point } from './kataElevationPainter';
import { KATA_STRINGS, type KataStrings } from '../i18n';

/** Reach of a click around a drawn line (px), and of a click "on the same spot" (px). */
export const LINE_REACH_PX = 6;
export const SAME_SPOT_PX = 4;

export function segmentDistance([px, py]: Point, [ax, ay]: Point, [bx, by]: Point): number {
  const dx = bx - ax;
  const dy = by - ay;
  const length2 = dx * dx + dy * dy;
  const t = length2 < 1e-9 ? 0 : Math.max(0, Math.min(1, ((px - ax) * dx + (py - ay) * dy) / length2));
  return Math.hypot(px - (ax + t * dx), py - (ay + t * dy));
}

/** The lines carrying bar keys within reach of `at` (screen), nearest first. */
export function barHits(lines: KataWebLine[], at: Point, point: (x: number, z: number) => Point): KataWebLine[] {
  return lines
    .filter((l) => l.keys.length > 0)
    .map((line) => {
      let best = Infinity;
      for (let i = 1; i < line.points.length; i++) {
        const a = line.points[i - 1]!;
        const b = line.points[i]!;
        best = Math.min(best, segmentDistance(at, point(a.x, a.z), point(b.x, b.z)));
      }
      return { line, best };
    })
    .filter((h) => h.best <= LINE_REACH_PX)
    .sort((a, b) => a.best - b.best)
    .map((h) => h.line);
}

/** The section flag under `at`: its stem, its flag and the number before it, with a few pixels to spare. */
export function flagAt(flags: KataWebFlag[], at: Point, point: (x: number, z: number) => Point, scale: number): KataWebFlag | null {
  const spare = 4;
  for (const flag of flags) {
    const numberReach = FLAG.textGap + STYLE.charWidthRatio * TAG.textHeight * String(flag.number).length;
    const [ox, oy] = point(flag.x, flag.z);
    const up = flag.below ? 1 : -1;
    const y1 = oy + up * FLAG.stemBottom * scale;
    const y2 = oy + up * FLAG.top * scale;
    if (at[0] >= ox - numberReach * scale - spare && at[0] <= ox + FLAG.width * scale + spare && at[1] >= Math.min(y1, y2) - spare && at[1] <= Math.max(y1, y2) + spare) {
      return flag;
    }
  }
  return null;
}

/** One line per kind of bar or stirrup zone the keys stand for, e.g. "Thép chủ trên Ø20 · L 12 340 · lớp 1 · 2 thanh". */
export function describeKeys(keys: readonly string[], t: KataStrings = KATA_STRINGS.vi): string[] {
  const groups = new Map<string, number>();
  const barTexts = new Set<string>();
  for (const key of keys) {
    const f = key.split('|');
    const text =
      f[0] === 'bar'
        ? t.barLine(t.barRoles[f[1] ?? ''] ?? f[1] ?? '', f[3] ?? '', Number(f[4]).toLocaleString(t.numberLocale), f[5] ?? '')
        : f[0] === 'zone'
          ? t.zoneLine(Number(f[1]) + 1, f[3] ?? '')
          : key;
    if (f[0] === 'bar') barTexts.add(text);
    groups.set(text, (groups.get(text) ?? 0) + 1);
  }
  return [...groups].map(([text, count]) => (count > 1 && barTexts.has(text) ? `${text} · ${t.barCount(count)}` : text));
}
