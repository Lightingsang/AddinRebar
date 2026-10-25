import { useCallback, useState } from 'react';
import type { KataWebRange, KataWebRebar, KataWebStationMap } from '../kataScene';
import { describeMeasure, findBar, snap, type ModelPoint } from './drawingTools';
import { TAG } from './kataCad';
import type { Point } from './kataElevationPainter';
import type { Palette } from './palette';
import type { Scene } from './scene';
import { ShapeList, type LayerId, type Shape } from './shapes';
import { SMALL_TEXT_SIZE, measure } from './text';

/** Room framed either side of a found tag (mm). */
const FIND_REACH = 1500;

interface ToolsInput {
  kata: KataWebRebar | null;
  scene: Scene | null;
  point: ((x: number, z: number) => Point) | null;
  stationMap: KataWebStationMap;
  frameRange(range: KataWebRange): void;
}

/**
 * The state of the web-only tools over the drawing: layers hidden, the measure tool (two clicks, snapped to the drawing,
 * the second point following the cursor until it is placed), and the bar found last.
 */
export function useDrawingTools({ kata, scene, point, stationMap, frameRange }: ToolsInput) {
  const [hidden, setHidden] = useState<ReadonlySet<LayerId>>(new Set());
  const [measuring, setMeasuringState] = useState(false);
  const [measurePoints, setMeasurePoints] = useState<{ a: ModelPoint; b: ModelPoint | null; cursor: ModelPoint | null } | null>(null);
  const [found, setFound] = useState<{ number: number; index: number } | null>(null);

  const toggleLayer = useCallback((layer: LayerId) => {
    setHidden((before) => {
      const next = new Set(before);
      if (!next.delete(layer)) next.add(layer);
      return next;
    });
  }, []);

  const setMeasuring = useCallback((on: boolean) => {
    setMeasuringState(on);
    setMeasurePoints(null);
  }, []);

  const snapAt = (at: Point) => (scene && point ? snap(kata, scene, stationMap, point, at).model : null);

  /** A click while measuring: the first point, then the second; a third click starts a new measure. */
  const measureClick = (at: Point) => {
    const model = snapAt(at);
    if (!model) return;
    setMeasurePoints((current) => (!current || current.b ? { a: model, b: null, cursor: null } : { ...current, b: model }));
  };

  const measureMove = (at: Point) => {
    if (!measurePoints || measurePoints.b) return;
    setMeasurePoints({ ...measurePoints, cursor: snapAt(at) });
  };

  /** Esc while measuring: drops the measure on screen first, then leaves the tool. True when it did something. */
  const escape = (): boolean => {
    if (measurePoints) setMeasurePoints(null);
    else if (measuring) setMeasuringState(false);
    else return false;
    return true;
  };

  const find = (number: number) => {
    const occurrences = kata ? findBar(kata, number) : [];
    if (occurrences.length === 0) {
      setFound(null);
      return { index: 0, count: 0 };
    }
    const index = found && found.number === number ? (found.index + 1) % occurrences.length : 0;
    setFound({ number, index });
    const at = stationMap.origin + stationMap.direction * occurrences[index]!.x;
    frameRange({ start: at - FIND_REACH, end: at + FIND_REACH });
    return { index, count: occurrences.length };
  };

  /** The measure line and its reading, the tags of the bar found: drawn over everything else. */
  const overlay = (palette: Palette): Shape[] => {
    const out = new ShapeList();
    if (!scene || !point) return out.shapes;
    const pen = { color: palette.accent, width: 1.5, dash: '6 3' };
    if (measurePoints) {
      const a = point(measurePoints.a.x, measurePoints.a.z);
      const end = measurePoints.b ?? measurePoints.cursor;
      out.circle(undefined, { color: palette.accent, width: 1.5 }, ...a, 4);
      if (end) {
        const b = point(end.x, end.z);
        out.line(pen, ...a, ...b);
        out.circle(undefined, { color: palette.accent, width: 1.5 }, ...b, 4);
        const label = measure(describeMeasure(measurePoints.a, end), SMALL_TEXT_SIZE, true);
        const lx = Math.min(scene.width - label.width - 8, Math.max(4, (a[0] + b[0]) / 2 - label.width / 2));
        const ly = Math.max(4, Math.min(a[1], b[1]) - label.height - 8);
        out.box(palette.fill, { color: palette.accent, width: 1 }, lx - 4, ly - 2, lx + label.width + 4, ly + label.height + 2);
        out.at(label, palette.accent, lx, ly);
      }
    }
    if (found && kata) {
      const s = scene.viewport.scale;
      for (const tag of findBar(kata, found.number)) {
        const [x, rowY] = point(tag.x, tag.rowZ);
        const [ix] = point(tag.insertX, tag.rowZ);
        for (const z of tag.footZ) out.line({ color: palette.accent, width: 3 }, x, point(tag.x, z)[1], x, rowY);
        out.circle(`${palette.accent}33`, { color: palette.accent, width: 2 }, ix, rowY, Math.max(8, TAG.circleRadius * s * 2.4));
      }
    }
    return out.shapes;
  };

  return { hidden, toggleLayer, measuring, setMeasuring, measureClick, measureMove, escape, find, overlay, clearFound: () => setFound(null) };
}
