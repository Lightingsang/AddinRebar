import { describe, expect, it } from 'vitest';
import { mockDocument, mockMetrics } from '../../bridge/mockData';
import type { ModelStatistics } from '../../bridge/protocol';
import { dashboardReducer, documentKey, initialDashboardState } from './dashboardState';

const statistics: ModelStatistics = { documentKey: mockDocument.path!, viewId: null, scope: 'model', total: 5, categories: [], metrics: mockMetrics, elapsedMs: 3 };
const noDocument = { ...mockDocument, hasDocument: false, title: null, path: null, activeView: null };

describe('dashboardReducer', () => {
  it('keeps previous statistics visible while a refresh is counting', () => {
    const loaded = dashboardReducer(initialDashboardState, { type: 'statisticsLoaded', statistics });

    const refreshing = dashboardReducer(loaded, { type: 'statisticsLoading' });

    expect(refreshing.statistics).toEqual({ status: 'loading', data: statistics, error: null });
  });

  it('marks numbers stale only once statistics exist, and clears stale on the next load', () => {
    const changes = { added: 1, deleted: 0, modified: 2 };

    const before = dashboardReducer(initialDashboardState, { type: 'modelStale', changes });
    const loaded = dashboardReducer(initialDashboardState, { type: 'statisticsLoaded', statistics });
    const stale = dashboardReducer(loaded, { type: 'modelStale', changes });
    const reloaded = dashboardReducer(stale, { type: 'statisticsLoaded', statistics });

    expect(before.stale).toBeNull();
    expect(stale.stale).toEqual(changes);
    expect(reloaded.stale).toBeNull();
  });

  it('drops statistics and selection when no document is open but keeps the chosen scope', () => {
    let state = dashboardReducer(initialDashboardState, { type: 'documentLoaded', document: mockDocument });
    state = dashboardReducer(state, { type: 'scopeChanged', scope: 'activeView' });
    state = dashboardReducer(state, { type: 'statisticsLoaded', statistics });
    state = dashboardReducer(state, { type: 'selectionChanged', count: 4 });

    const closed = dashboardReducer(state, { type: 'documentLoaded', document: noDocument });

    expect(closed.statistics.data).toBeNull();
    expect(closed.selectionCount).toBeNull();
    expect(closed.scope).toBe('activeView');
    expect(closed.document.data?.hasDocument).toBe(false);
  });

  it('clears numbers and selection when another document becomes active, keeps them for the same one', () => {
    let state = dashboardReducer(initialDashboardState, { type: 'documentLoaded', document: mockDocument });
    state = dashboardReducer(state, { type: 'statisticsLoaded', statistics });
    state = dashboardReducer(state, { type: 'selectionChanged', count: 2 });

    const sameDocumentOtherView = dashboardReducer(state, {
      type: 'documentLoaded',
      document: { ...mockDocument, activeView: { id: 1, name: 'Section 1', viewType: 'Section', scale: 50 } },
    });
    const otherDocument = dashboardReducer(state, { type: 'documentLoaded', document: { ...mockDocument, path: 'D:\\other.rvt' } });

    expect(sameDocumentOtherView.statistics.data).toBe(statistics);
    expect(otherDocument.statistics.data).toBeNull();
    expect(otherDocument.selectionCount).toBeNull();
  });

  it('settles a request that brought no numbers back to the previous status', () => {
    const loaded = dashboardReducer(initialDashboardState, { type: 'statisticsLoaded', statistics });
    const busyWithData = dashboardReducer(dashboardReducer(loaded, { type: 'statisticsLoading' }), { type: 'statisticsSettled' });
    const busyWithout = dashboardReducer(dashboardReducer(initialDashboardState, { type: 'statisticsLoading' }), { type: 'statisticsSettled' });

    expect(busyWithData.statistics.status).toBe('ready');
    expect(busyWithout.statistics.status).toBe('idle');
  });

  it('keeps the last document when a reload fails', () => {
    const loaded = dashboardReducer(initialDashboardState, { type: 'documentLoaded', document: mockDocument });

    const failed = dashboardReducer(loaded, { type: 'documentFailed', error: 'TIMEOUT' });

    expect(failed.document).toEqual({ status: 'error', data: mockDocument, error: 'TIMEOUT' });
  });
});

describe('documentKey', () => {
  it('uses the path, falls back to the title, and is null without a document', () => {
    expect(documentKey(mockDocument)).toBe(mockDocument.path);
    expect(documentKey({ ...mockDocument, path: null })).toBe(mockDocument.title);
    expect(documentKey(noDocument)).toBeNull();
    expect(documentKey(null)).toBeNull();
  });
});
