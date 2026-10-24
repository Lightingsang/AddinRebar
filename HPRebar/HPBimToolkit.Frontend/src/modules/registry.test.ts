import { describe, expect, it } from 'vitest';
import { moduleFromHash } from './registry';

describe('moduleFromHash', () => {
  it.each([
    ['#/dashboard', 'dashboard'],
    ['', 'dashboard'],
    ['#/reports', 'dashboard'],
    ['#/does-not-exist', 'dashboard'],
  ])('maps %s to %s (unavailable modules fall back)', (hash, expected) => {
    expect(moduleFromHash(hash)).toBe(expected);
  });
});
