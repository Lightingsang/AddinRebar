import { useCallback, useEffect, useRef, useState } from 'react';
import { errorText } from '../bridge/errorText';
import type { KataAction, KataBridge, KataUpdateRequest, KataViewState } from './kataContracts';
import type { KataFocusEvent, KataWeb3dModel, KataWebScene, KataWebSection } from './kataScene';

type Notify = (text: string, tone?: 'info' | 'success' | 'error') => void;

export interface KataController {
  state: KataViewState | null;
  /** What the page draws itself; null until the host sends it, or when the run has none. */
  scene: KataWebScene | null;
  /** The host has sent a scene frame (scene or not) since the page loaded. */
  sceneArrived: boolean;
  /** Why the host could not build the scene. */
  sceneError: string | null;
  /** The latest request to frame a span or the run (a new object each time). */
  focus: KataFocusEvent | null;
  loadError: string | null;
  update(request: KataUpdateRequest): void;
  run(action: KataAction): void;
  /** Kata's section `number` of the plan shown, built by the host when asked (null: no such cut). */
  loadSection(number: number): Promise<KataWebSection | null>;
  /** The plan shown, in 3D, built by the host when asked. */
  loadModel3d(): Promise<KataWeb3dModel>;
  /** Strikes bar groups off the plan through the view model (undo: the undoRemoveBars action). */
  removeBars(keys: string[]): void;
}

/**
 * The page's whole state comes from the host: the first `kata.getState`, then every `kata.state` / `kata.scene` /
 * `kata.focus` event (`kata.ready` asks for the first scene once the page listens). Commands only send what the user did; their answers carry
 * nothing the events do not also bring (a late answer never overwrites a newer event, and scenes older than the one
 * shown are dropped).
 */
export function useKata(bridge: KataBridge, notify: Notify): KataController {
  const [state, setState] = useState<KataViewState | null>(null);
  const [scene, setScene] = useState<KataWebScene | null>(null);
  const [sceneArrived, setSceneArrived] = useState(false);
  const [sceneError, setSceneError] = useState<string | null>(null);
  const [focus, setFocus] = useState<KataFocusEvent | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const shownScene = useRef(0);

  useEffect(() => {
    const unsubscribers = [
      bridge.on('kata.state', (next) => setState(next)),
      bridge.on('kata.scene', (frame) => {
        if (frame.revision <= shownScene.current) return;
        shownScene.current = frame.revision;
        setScene(frame.scene);
        setSceneError(frame.error ?? null);
        setSceneArrived(true);
      }),
      bridge.on('kata.focus', (next) => setFocus(next)),
    ];
    bridge
      .request('kata.getState')
      .then((first) => setState((current) => current ?? first))
      .catch((error: unknown) => setLoadError(`Không kết nối được Revit: ${errorText(error)}`));
    bridge.request('kata.ready').catch(() => undefined); // a host that cannot answer has already failed getState
    return () => unsubscribers.forEach((unsubscribe) => unsubscribe());
  }, [bridge]);

  const fail = useCallback((error: unknown) => notify(errorText(error), 'error'), [notify]);

  const update = useCallback((request: KataUpdateRequest) => void bridge.request('kata.update', request).catch(fail), [bridge, fail]);

  const run = useCallback((action: KataAction) => void bridge.request('kata.run', { action }).catch(fail), [bridge, fail]);

  const loadSection = useCallback((number: number) => bridge.request('kata.section', { number }), [bridge]);

  const loadModel3d = useCallback(() => bridge.request('kata.model3d'), [bridge]);

  const removeBars = useCallback((keys: string[]) => void bridge.request('kata.removeBars', { keys }).catch(fail), [bridge, fail]);

  return { state, scene, sceneArrived, sceneError, focus, loadError, update, run, loadSection, loadModel3d, removeBars };
}
