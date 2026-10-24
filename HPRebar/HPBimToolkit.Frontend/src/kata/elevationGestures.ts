// Pointer handling of the elevation, as in the WPF canvas (AutoCAD-like): the wheel zooms around the cursor, the middle
// button or Shift + left drag pans, a middle double click is Zoom Extents, a left click (no drag) picks. The page only
// classifies gestures; the host's canvas does the zooming, panning and picking.

/** A press that travels less than this is a click (the WPF canvas uses the same threshold). */
export const DRAG_THRESHOLD_PX = 4;

/** Browsers report about 100 px of wheel delta per notch in pixel mode; one notch zooms by the WPF step. */
const PIXELS_PER_NOTCH = 100;
const LINES_PER_NOTCH = 3;

export interface Press {
  x: number;
  y: number;
  button: number;
  panning: boolean;
}

export type Release = { kind: 'click'; x: number; y: number } | { kind: 'pan'; x: number; y: number; dx: number; dy: number } | null;

/** Starts a press: the middle button, or Shift with the left button, pans; the left button alone picks on release. */
export function startPress(x: number, y: number, button: number, shift: boolean): Press | null {
  if (button !== 0 && button !== 1) return null;
  return { x, y, button, panning: button === 1 || shift };
}

/** How far a pan has moved so far (for the live preview while dragging). */
export function panOffset(press: Press, x: number, y: number): { dx: number; dy: number } {
  return press.panning ? { dx: x - press.x, dy: y - press.y } : { dx: 0, dy: 0 };
}

/** What a release means: a click where it was pressed, a whole pan, or nothing (a left drag without Shift). */
export function endPress(press: Press, x: number, y: number): Release {
  const dx = x - press.x;
  const dy = y - press.y;
  const moved = Math.hypot(dx, dy) >= DRAG_THRESHOLD_PX;
  if (press.panning) return moved ? { kind: 'pan', x: press.x, y: press.y, dx, dy } : null;
  if (press.button === 0 && !moved) return { kind: 'click', x: press.x, y: press.y };
  return null;
}

/** Wheel notches, + zooms in (wheel away from the user), whatever unit the browser reports the delta in. */
export function wheelNotches(deltaY: number, deltaMode: number): number {
  const notches = deltaMode === 1 ? deltaY / LINES_PER_NOTCH : deltaMode === 2 ? deltaY : deltaY / PIXELS_PER_NOTCH;
  return -notches;
}
