import { useCallback, useEffect, useState } from 'react';
import type { KataWebRebar, KataWebSection } from '../kataScene';
import { SAME_SPOT_PX, barHits } from './kataHit';
import type { Point } from './kataElevationPainter';
import type { SectionState } from './SectionPanel';

/** A bar selection kept above the drawing, so the elevation and the 3D view share it (keys of the plan shown). */
export interface SharedBarSelection {
  keys: string[] | null;
  set(keys: string[] | null): void;
}

/**
 * The bar group selected on Kata's elevation, for the plan it was picked on (a new plan — bars removed, the sheet read
 * again — drops it, as the WPF canvas did); a second click on the same spot takes the next line under it. With
 * `shared`, the keys live there (the 3D view reads and sets the same ones).
 */
export function useBarSelection(rebar: KataWebRebar | null, shared?: SharedBarSelection) {
  const [selection, setSelection] = useState<{ rebar: KataWebRebar; keys: string[]; line: number; at: Point } | null>(null);
  const own = selection && selection.rebar === rebar ? selection.keys : null;
  const keys = shared ? (rebar ? shared.keys : null) : own;

  /** Selects the group of the line under `at`; false when there is none. */
  const pick = useCallback(
    (at: Point, point: (x: number, z: number) => Point): boolean => {
      if (!rebar) return false;
      const hits = barHits(rebar.lines, at, point);
      if (hits.length === 0) return false;
      let index = 0;
      if (selection && selection.rebar === rebar && Math.hypot(at[0] - selection.at[0], at[1] - selection.at[1]) <= SAME_SPOT_PX) {
        const current = hits.indexOf(rebar.lines[selection.line]!);
        if (current >= 0) index = (current + 1) % hits.length;
      }
      const line = hits[index]!;
      setSelection({ rebar, keys: line.keys, line: rebar.lines.indexOf(line), at });
      shared?.set(line.keys);
      return true;
    },
    [rebar, selection, shared],
  );

  const clear = useCallback(() => {
    setSelection(null);
    shared?.set(null);
  }, [shared]);
  return { keys, pick, clear };
}

/**
 * Sections fetched from the host (`kata.section`), one per plan and number; a new plan fetches again. The host builds
 * each section only when the page asks for it.
 */
export function useSection(rebar: KataWebRebar | null, number: number | null, load: (number: number) => Promise<KataWebSection | null>): SectionState | null {
  const [loaded, setLoaded] = useState<{ rebar: KataWebRebar | null; sections: Map<number, SectionState> }>({ rebar: null, sections: new Map() });
  const sections = loaded.rebar === rebar ? loaded.sections : null;
  const known = number === null ? null : sections?.get(number);

  useEffect(() => {
    if (number === null || !rebar || known) return;
    let alive = true;
    const store = (state: SectionState) =>
      alive &&
      setLoaded((current) => {
        const map = new Map(current.rebar === rebar ? current.sections : []);
        map.set(number, state);
        return { rebar, sections: map };
      });
    load(number).then(
      (section) => store(section ? { status: 'ready', section } : { status: 'failed', message: '' }),
      (error: unknown) => store({ status: 'failed', message: error instanceof Error ? error.message : String(error) }),
    );
    return () => {
      alive = false;
    };
  }, [rebar, number, known, load]);

  return number === null ? null : (known ?? { status: 'loading' });
}
