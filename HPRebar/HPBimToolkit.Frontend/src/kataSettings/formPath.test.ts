import { describe, expect, it } from 'vitest';
import { parseNumber } from './fields';
import { getIn, parsePath, setIn, tabOf } from './formPath';

describe('form paths', () => {
  const form = { detail: { endZoneFraction: 0.25 }, special: { lapRows: [{ diameter: 16 }, { diameter: 20 }] } };

  it('reads the paths the host names its errors by', () => {
    expect(parsePath('special.lapRows[1].diameter')).toEqual(['special', 'lapRows', 1, 'diameter']);
    expect(getIn(form, 'special.lapRows[1].diameter')).toBe(20);
    expect(getIn(form, 'special.massRows[0].kgPerM')).toBeUndefined();
    expect(() => parsePath('detail..x')).toThrow();
  });

  it('writes a copy, sharing what the path does not cross', () => {
    const next = setIn(form, 'special.lapRows[0].diameter', 12);
    expect(next.special.lapRows[0]!.diameter).toBe(12);
    expect(form.special.lapRows[0]!.diameter).toBe(16);
    expect(next.special.lapRows[1]).toBe(form.special.lapRows[1]);
    expect(next.detail).toBe(form.detail);
  });

  it('names the tab of a field', () => {
    expect(tabOf('special.lapRows[2].diameter')).toBe('special');
    expect(tabOf('joints.beam.stirrups')).toBe('joints');
  });
});

describe('number boxes', () => {
  it('read empty or wrong text as null for the host to refuse, a comma as the decimal point', () => {
    expect(parseNumber('')).toBeNull();
    expect(parseNumber('abc')).toBeNull();
    expect(parseNumber('0,25')).toBe(0.25);
    expect(parseNumber(' 11700 ')).toBe(11700);
    expect(parseNumber('2.5', true)).toBeNull();
    expect(parseNumber('3', true)).toBe(3);
  });
});
