import { describe, expect, it } from 'vitest';
import { DRAG_THRESHOLD_PX, endPress, startPress, wheelNotches } from './elevationGestures';

describe('elevation gestures', () => {
  it('a left press released in place is a click where it was pressed', () => {
    const press = startPress(100, 50, 0, false)!;

    expect(endPress(press, 101, 51)).toEqual({ kind: 'click', x: 100, y: 50 });
  });

  it('a left drag without Shift is neither a click nor a pan (as in the WPF canvas)', () => {
    const press = startPress(100, 50, 0, false)!;

    expect(endPress(press, 100 + DRAG_THRESHOLD_PX + 1, 50)).toBeNull();
  });

  it('the middle button and Shift + left pan by the whole movement from the press point', () => {
    for (const press of [startPress(10, 20, 1, false)!, startPress(10, 20, 0, true)!]) {
      expect(endPress(press, 40, 5)).toEqual({ kind: 'pan', x: 10, y: 20, dx: 30, dy: -15 });
    }
  });

  it('a pan that never moved past the threshold does nothing', () => {
    expect(endPress(startPress(10, 20, 1, false)!, 11, 21)).toBeNull();
  });

  it('the right button starts nothing', () => {
    expect(startPress(0, 0, 2, false)).toBeNull();
  });

  it('wheel away from the user zooms in, one notch per 100 px, 3 lines or 1 page', () => {
    expect(wheelNotches(-100, 0)).toBe(1);
    expect(wheelNotches(200, 0)).toBe(-2);
    expect(wheelNotches(-3, 1)).toBe(1);
    expect(wheelNotches(1, 2)).toBe(-1);
  });
});
