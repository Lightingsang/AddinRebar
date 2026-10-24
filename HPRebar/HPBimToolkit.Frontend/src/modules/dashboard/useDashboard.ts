import { useCallback, useEffect, useReducer, useRef } from 'react';
import { BridgeError, type BridgeClient } from '../../bridge/bridgeClient';
import { errorText } from '../../bridge/errorText';
import type { CategoryCount, DocumentSnapshot, ShowMode, StatisticsScope } from '../../bridge/protocol';
import type { ToastTone } from '../../ui/Toast';
import { dashboardReducer, documentKey, initialDashboardState } from './dashboardState';

type Notify = (text: string, tone?: ToastTone) => void;

/** Longer than the host's statistics budget (DashboardModule.StatisticsTimeout, 120 s) so the host's TIMEOUT comes first. */
export const STATISTICS_TIMEOUT_MS = 130_000;

/** Selecting ~20 000 elements froze Revit for 18 s on a 73k-element model (measured); above this the page asks first. */
export const LARGE_SELECTION = 5_000;

const confirmLarge = (question: string): boolean => window.confirm(question);

/**
 * Dashboard state and actions over the bridge. Statistics are read on load, on refresh, on a scope change, when
 * another document becomes active and — for the active-view scope — when another view does; Revit edits only mark
 * them stale. Answers that belong to another document than the one on screen are dropped.
 */
export function useDashboard(bridge: BridgeClient, notify: Notify) {
  const [state, dispatch] = useReducer(dashboardReducer, initialDashboardState);
  const currentKey = useRef<string | null>(null);
  const currentViewId = useRef<number | null>(null);
  const scopeRef = useRef<StatisticsScope>(state.scope);

  const loadStatistics = useCallback(
    async (scope: StatisticsScope) => {
      dispatch({ type: 'statisticsLoading' });
      try {
        const statistics = await bridge.request('dashboard.getStatistics', { scope }, { timeoutMs: STATISTICS_TIMEOUT_MS });
        if (statistics.documentKey !== currentKey.current || statistics.scope !== scopeRef.current) {
          dispatch({ type: 'statisticsSettled' }); // counted for a document or scope no longer on screen
          return;
        }
        dispatch({ type: 'statisticsLoaded', statistics });
      } catch (error) {
        if (error instanceof BridgeError && error.code === 'BUSY') {
          dispatch({ type: 'statisticsSettled' });
          notify(errorText(error), 'info');
          return;
        }
        dispatch({ type: 'statisticsFailed', error: errorText(error) });
        if (!(error instanceof BridgeError && error.code === 'NO_DOCUMENT')) {
          notify(`Không thống kê được: ${errorText(error)}`, 'error');
        }
      }
    },
    [bridge, notify],
  );

  const loadSelection = useCallback(async () => {
    try {
      dispatch({ type: 'selectionChanged', count: (await bridge.request('dashboard.getSelection')).count });
    } catch {
      // The count also arrives with the next selection.changed event; a failed first read is not worth a toast.
    }
  }, [bridge]);

  const applyDocument = useCallback(
    (document: DocumentSnapshot) => {
      dispatch({ type: 'documentLoaded', document });
      const key = documentKey(document);
      const viewId = document.activeView?.id ?? null;
      const switchedDocument = key !== currentKey.current;
      const switchedView = viewId !== currentViewId.current;
      currentKey.current = key;
      currentViewId.current = viewId;

      if (!key) {
        return;
      }
      if (switchedDocument) {
        void loadStatistics(scopeRef.current);
        void loadSelection();
      } else if (switchedView && scopeRef.current === 'activeView') {
        void loadStatistics('activeView');
      }
    },
    [loadStatistics, loadSelection],
  );

  const loadDocument = useCallback(async () => {
    dispatch({ type: 'documentLoading' });
    try {
      applyDocument(await bridge.request('dashboard.getDocumentInfo'));
    } catch (error) {
      dispatch({ type: 'documentFailed', error: errorText(error) });
    }
  }, [bridge, applyDocument]);

  useEffect(() => {
    const unsubscribers = [
      bridge.on('document.changed', applyDocument),
      bridge.on('selection.changed', ({ count }) => dispatch({ type: 'selectionChanged', count })),
      bridge.on('model.stale', (changes) => dispatch({ type: 'modelStale', changes })),
    ];
    void loadDocument();
    return () => unsubscribers.forEach((unsubscribe) => unsubscribe());
  }, [bridge, applyDocument, loadDocument]);

  const refresh = useCallback(async () => {
    await loadDocument();
    if (currentKey.current) {
      await Promise.all([loadStatistics(scopeRef.current), loadSelection()]);
    }
  }, [loadDocument, loadStatistics, loadSelection]);

  const setScope = useCallback(
    (scope: StatisticsScope) => {
      scopeRef.current = scope;
      dispatch({ type: 'scopeChanged', scope });
      void loadStatistics(scope);
    },
    [loadStatistics],
  );

  const showCategory = useCallback(
    async (category: CategoryCount, mode: ShowMode) => {
      const { categoryId, name, count } = category;
      if (count > LARGE_SELECTION && !confirmLarge(`Chọn ${count.toLocaleString('vi-VN')} phần tử ${name}? Revit có thể đứng vài giây đến vài chục giây.`)) {
        return;
      }
      try {
        const { shown, zoomed } = await bridge.request('dashboard.showElements', { categoryId, mode, scope: scopeRef.current });
        notify(
          mode === 'zoom' && shown > 0 && zoomed === 0
            ? `Đã chọn ${shown} phần tử ${name}; view hiện hành không hiển thị phần tử nào để phóng tới.`
            : `Đã chọn ${shown} phần tử ${name}.`,
          'success',
        );
      } catch (error) {
        notify(`Không chọn được ${name}: ${errorText(error)}`, 'error');
      }
    },
    [bridge, notify],
  );

  const zoomToSelection = useCallback(async () => {
    try {
      const { shown, zoomed } = await bridge.request('dashboard.showElements', { currentSelection: true, mode: 'zoom' });
      if (shown === 0) {
        notify('Chưa chọn phần tử nào trong Revit.', 'info');
      } else if (zoomed === 0) {
        notify('View hiện hành không hiển thị phần tử nào đang chọn — mở view chứa chúng rồi thử lại.', 'info');
      }
    } catch (error) {
      notify(`Không phóng tới được: ${errorText(error)}`, 'error');
    }
  }, [bridge, notify]);

  const openTool = useCallback(
    async (toolId: string, label: string) => {
      try {
        await bridge.request('dashboard.openTool', { toolId });
      } catch (error) {
        notify(`Không mở được ${label}: ${errorText(error)}`, 'error');
      }
    },
    [bridge, notify],
  );

  return { state, refresh, setScope, showCategory, zoomToSelection, openTool };
}
