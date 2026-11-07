// What Kata's column drawing shares between the elevation and the section painters: text sizes on the sheet, the
// pick targets, and how numbers are written.

import type { Shape } from '../../kata/elevation/shapes';

/** Text heights on the sheet (mm): kata_text 2.5 mm at 1:30, titles larger. */
export const TEXT_MM = 80;
export const LEVEL_TEXT_MM = 110;
export const TITLE_MM = 150;
export const LABEL_CIRCLE_MM = 75;
/** Section bar numbers appear once they are this many px high. */
export const NUMBER_MIN_PX = 7;

/** A bar to pick: an elevation segment or a section circle, in screen px. */
export type BarHit =
  | { number: number; t: 'segment'; x1: number; y1: number; x2: number; y2: number }
  | { number: number; t: 'circle'; x: number; y: number; r: number };

export interface PaintedSheet {
  shapes: Shape[];
  hits: BarHit[];
}

export const mm = (value: number) => Math.round(value).toString();

/** The label of a representative bar: "20Ø25". */
export const barLabel = (bar: { count: number; diameterMm: number }) => `${bar.count}Ø${mm(bar.diameterMm)}`;
