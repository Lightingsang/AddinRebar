import { useCallback, useEffect, useRef, useState } from 'react';
import { errorText } from '../bridge/errorText';
import type { KataAction, KataBridge, KataCanvasInput, KataDrawingFrame, KataUpdateRequest, KataViewState } from './kataContracts';
import type { KataFocusEvent, KataWebScene, KataWebSection } from './kataScene';

type Notify = (text: string, tone?: 'info' | 'success' | 'error') => void;

export interface KataController {
  state: KataViewState | null;
  drawing: KataDrawingFrame | null;
  /** What the page draws itself; null before a run is picked. */
  scene: KataWebScene | null;
  /** The latest request to frame a span or the run (a new object each time). */
  focus: KataFocusEvent | null;
  loadError: string | null;
  update(request: KataUpdateRequest): void;
  run(action: KataAction): void;
  gesture(input: KataCanvasInput): Promise<boolean>;
  /** Kata's section `number` of the plan shown, built by the host when asked (null: no such cut). */
  loadSection(number: number): Promise<KataWebSection | null>;
  /** Strikes bar groups off the plan through the view model (undo: the undoRemoveBars action). */
  removeBars(keys: string[]): void;
  setViewport(width: number, height: number): void;
}

/**
 * The page's whole state comes from the host: the first `kata.getState`, then every `kata.state` / `kata.drawing` /
 * `kata.scene` / `kata.focus` event. Commands only send what the user did; their answers carry nothing the events do not also bring (a late answer
 * never overwrites a newer event, and drawings older than the one shown are dropped).
 */
export function useKata(bridge: KataBridge, notify: Notify): KataController {
  const [state, setState] = useState<KataViewState | null>(null);
  const [drawing, setDrawing] = useState<KataDrawingFrame | null>(null);
  const [scene, setScene] = useState<KataWebScene | null>(null);
  const [focus, setFocus] = useState<KataFocusEvent | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const shownRevision = useRef(0);
  const shownScene = useRef(0);

  useEffect(() => {
    const unsubscribers = [
      bridge.on('kata.state', (next) => setState(next)),
      bridge.on('kata.drawing', (frame) => {
        if (frame.revision <= shownRevision.current) return;
        shownRevision.current = frame.revision;
        setDrawing(frame);
      }),
      bridge.on('kata.scene', (frame) => {
        if (frame.revision <= shownScene.current) return;
        shownScene.current = frame.revision;
        setScene(frame.scene);
      }),
      bridge.on('kata.focus', (next) => setFocus(next)),
    ];
    bridge
      .request('kata.getState')
      .then((first) => setState((current) => current ?? first))
      .catch((error: unknown) => setLoadError(`Không kết nối được Revit: ${errorText(error)}`));
    return () => unsubscribers.forEach((unsubscribe) => unsubscribe());
  }, [bridge]);

  const fail = useCallback((error: unknown) => notify(errorText(error), 'error'), [notify]);

  const update = useCallback((request: KataUpdateRequest) => void bridge.request('kata.update', request).catch(fail), [bridge, fail]);

  const run = useCallback((action: KataAction) => void bridge.request('kata.run', { action }).catch(fail), [bridge, fail]);

  const gesture = useCallback(
    (input: KataCanvasInput) =>
      bridge.request('kata.canvas', input).then(
        (result) => result.handled,
        (error: unknown) => {
          fail(error);
          return false;
        },
      ),
    [bridge, fail],
  );

  const loadSection = useCallback((number: number) => bridge.request('kata.section', { number }), [bridge]);

  const removeBars = useCallback((keys: string[]) => void bridge.request('kata.removeBars', { keys }).catch(fail), [bridge, fail]);

  const setViewport = useCallback(
    (width: number, height: number) => void bridge.request('kata.viewport', { width: Math.round(width), height: Math.round(height) }).catch(fail),
    [bridge, fail],
  );

  return { state, drawing, scene, focus, loadError, update, run, gesture, loadSection, removeBars, setViewport };
}
