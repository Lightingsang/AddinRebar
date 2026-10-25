// One horizontal row of labels (port of KataLabelLane.cs): a label is drawn only if it stays clear of every label
// already placed, so a zoomed-out run shows fewer labels instead of overlapping ones. Labels placed first win.

const GAP = 4;

export class LabelLane {
  private readonly placed: [number, number][] = [];

  tryPlace(left: number, right: number): boolean {
    if (this.placed.some(([l, r]) => left < r + GAP && right > l - GAP)) return false;
    this.placed.push([left, right]);
    return true;
  }

  /** Places a label of `width` as near `left` as it fits, sliding right or left in steps up to `maxShift`. */
  placeNear(left: number, width: number, maxShift: number, step = 6): number | null {
    if (step <= 0) throw new RangeError('step must be positive');
    for (let shift = 0; shift <= maxShift; shift += step) {
      if (this.tryPlace(left + shift, left + shift + width)) return left + shift;
      if (shift > 0 && this.tryPlace(left - shift, left - shift + width)) return left - shift;
    }
    return null;
  }
}
