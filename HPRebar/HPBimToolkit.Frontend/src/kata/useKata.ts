import { useCallback, useEffect, useRef, useState } from 'react';
import { errorText } from '../bridge/errorText';
import type { KataAction, KataBridge, KataCanvasInput, KataDrawingFrame, KataUpdateRequest, KataViewState } from './kataContracts';

type Notify = (text: string, tone?: 'info' | 'success' | 'error') => void;

export interface KataController {
  state: KataViewState | null;
  drawing: KataDrawingFrame | null;
  loadError: string | null;
  update(request: KataUpdateRequest): void;
  run(action: KataAction): void;
  gesture(input: KataCanvasInput): Promise<boolean>;
  setViewport(width: number, height: number): void;
}

/**
 * The page's whole state comes from the host: the first `kata.getState`, then every `kata.state` / `kata.drawing`
 * event. Commands only send what the user did; their answers carry nothing the events do not also bring (a late answer
 * never overwrites a newer event, and drawings older than the one shown are dropped).
 */
export function useKata(bridge: KataBridge, notify: Notify): KataController {
  const [state, setState] = useState<KataViewState | null>(null);
  const [drawing, setDrawing] = useState<KataDrawingFrame | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const shownRevision = useRef(0);

  useEffect(() => {
    const unsubscribers = [
      bridge.on('kata.state', (next) => setState(next)),
      bridge.on('kata.drawing', (frame) => {
        if (frame.revision <= shownRevision.current) return;
        shownRevision.current = frame.revision;
        setDrawing(frame);
      }),
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

  const setViewport = useCallback(
    (width: number, height: number) => void bridge.request('kata.viewport', { width: Math.round(width), height: Math.round(height) }).catch(fail),
    [bridge, fail],
  );

  return { state, drawing, loadError, update, run, gesture, setViewport };
}
