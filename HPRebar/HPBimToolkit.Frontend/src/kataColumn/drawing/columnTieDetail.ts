// The hoop and ties taken apart beside a section, as VC draws them so each shape can be read on its own: the hoop in
// the middle, wide ties moved right, tall ties up, horizontal C ties left, vertical C ties down (j-all500.json: +388,
// +348, -506 and -346 mm around a 1000 × 800 hoop). Pure: section mm in, section mm out.

import type { KataColumnSectionDrawing } from '../columnContracts';
import { mm } from './columnStyle';

export type TieDetailKind = 'hoop' | 'closed' | 'cross';

/** Where a detail's label leaves its shape: up then right, up then left, or straight right. */
export type LabelSide = 'right' | 'left' | 'beside';

export interface TieDetail {
  kind: TieDetailKind;
  number: number;
  /** "Ø10a100". */
  note: string;
  x1: number;
  y1: number;
  x2: number;
  y2: number;
  /** Where the leader starts. */
  anchorX: number;
  anchorY: number;
  side: LabelSide;
}

/** Moves as fractions of the hoop's size, the next tie the same way one step further. */
const MOVE_RIGHT = 0.4;
const MOVE_LEFT = 0.5;
const MOVE_UP_DOWN = 0.45;
const NEXT_STEP = 0.15;

/** From the section's right face to the hoop's detail: clears the h dimension and a C tie moved left. */
export const detailGap = (widthMm: number) => 0.65 * widthMm + 400;

/** The hoop and every tie of a section, moved apart, in the section's own mm. */
export function tieDetails(d: KataColumnSectionDrawing): TieDetail[] {
  const originX = d.widthMm + detailGap(d.widthMm);
  const o = d.hoopOffsetMm;
  const x2 = originX + d.widthMm - 2 * o;
  const y2 = d.depthMm - o;
  const hoopNote = `Ø${mm(d.hoopDiameterMm)}a${mm(d.hoopSpacingMm)}`;
  const hoop: TieDetail = { kind: 'hoop', number: d.hoopNumber, note: hoopNote, x1: originX, y1: o, x2, y2, anchorX: x2 - 0.1 * d.widthMm, anchorY: y2, side: 'right' };
  const tieNote = `Ø${mm(d.tieDiameterMm)}a${mm(d.tieSpacingMm)}`;
  const shiftX = originX - o;
  const taken = { right: 0, left: 0, up: 0, down: 0 };

  const ties = d.ties.map((tie): TieDetail => {
    const wide = (tie.x2 - tie.x1) / d.widthMm >= (tie.y2 - tie.y1) / d.depthMm;
    const across = tie.kind === 'cross' && tie.y1 === tie.y2;
    const along = tie.kind === 'cross' && tie.x1 === tie.x2;
    let dx = shiftX;
    let dy = 0;
    if (across) dx -= (MOVE_LEFT + NEXT_STEP * taken.left++) * d.widthMm;
    else if (along) dy -= (MOVE_UP_DOWN + NEXT_STEP * taken.down++) * d.depthMm;
    else if (wide) dx += (MOVE_RIGHT + NEXT_STEP * taken.right++) * d.widthMm;
    else dy += (MOVE_UP_DOWN + NEXT_STEP * taken.up++) * d.depthMm;

    const shape = { kind: tie.kind, number: tie.number, note: tieNote, x1: tie.x1 + dx, y1: tie.y1 + dy, x2: tie.x2 + dx, y2: tie.y2 + dy };
    if (across) return { ...shape, anchorX: shape.x1 + 0.25 * (shape.x2 - shape.x1), anchorY: shape.y1, side: 'left' };
    if (along) return { ...shape, anchorX: shape.x1, anchorY: shape.y1 + 0.25 * (shape.y2 - shape.y1), side: 'beside' };
    return { ...shape, anchorX: wide ? shape.x2 - 0.1 * d.widthMm : (shape.x1 + shape.x2) / 2, anchorY: shape.y2, side: 'right' };
  });

  return [hoop, ...ties];
}
