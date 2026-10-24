import type { DocumentSnapshot, ModelStaleEvent, ModelStatistics, StatisticsScope } from '../../bridge/protocol';

export type LoadStatus = 'idle' | 'loading' | 'ready' | 'error';

export interface Loadable<T> {
  status: LoadStatus;
  data: T | null;
  error: string | null;
}

export interface DashboardState {
  document: Loadable<DocumentSnapshot>;
  statistics: Loadable<ModelStatistics>;
  scope: StatisticsScope;
  /** null until the first selection read or event. */
  selectionCount: number | null;
  /** Element changes since the statistics were read; null when the numbers are current. */
  stale: ModelStaleEvent | null;
}

export type DashboardAction =
  | { type: 'documentLoading' }
  | { type: 'documentLoaded'; document: DocumentSnapshot }
  | { type: 'documentFailed'; error: string }
  | { type: 'statisticsLoading' }
  | { type: 'statisticsLoaded'; statistics: ModelStatistics }
  | { type: 'statisticsFailed'; error: string }
  | { type: 'statisticsSettled' }
  | { type: 'scopeChanged'; scope: StatisticsScope }
  | { type: 'selectionChanged'; count: number }
  | { type: 'modelStale'; changes: ModelStaleEvent };

const idle = <T,>(): Loadable<T> => ({ status: 'idle', data: null, error: null });

export const initialDashboardState: DashboardState = {
  document: idle(),
  statistics: idle(),
  scope: 'model',
  selectionCount: null,
  stale: null,
};

/** Identity of a document across events: its path, or its title while it was never saved. */
export const documentKey = (document: DocumentSnapshot | null): string | null =>
  document?.hasDocument ? (document.path ?? document.title) : null;

export function dashboardReducer(state: DashboardState, action: DashboardAction): DashboardState {
  switch (action.type) {
    case 'documentLoading':
      return { ...state, document: { ...state.document, status: 'loading', error: null } };
    case 'documentLoaded': {
      const document: Loadable<DocumentSnapshot> = { status: 'ready', data: action.document, error: null };
      // Another document (or none): its numbers and selection are not this one's.
      return documentKey(action.document) === documentKey(state.document.data)
        ? { ...state, document }
        : { ...initialDashboardState, scope: state.scope, document };
    }
    case 'documentFailed':
      return { ...state, document: { status: 'error', data: state.document.data, error: action.error } };
    case 'statisticsLoading':
      // Keep the previous numbers on screen while counting, so a refresh does not flash the empty state.
      return { ...state, statistics: { ...state.statistics, status: 'loading', error: null } };
    case 'statisticsLoaded':
      return { ...state, statistics: { status: 'ready', data: action.statistics, error: null }, stale: null };
    case 'statisticsFailed':
      return { ...state, statistics: { status: 'error', data: state.statistics.data, error: action.error } };
    case 'statisticsSettled':
      // A request that ended without new numbers (BUSY, an answer for another document): back to what is on screen.
      return { ...state, statistics: { ...state.statistics, status: state.statistics.data ? 'ready' : 'idle' } };
    case 'scopeChanged':
      return { ...state, scope: action.scope };
    case 'selectionChanged':
      return { ...state, selectionCount: action.count };
    case 'modelStale':
      return state.statistics.data ? { ...state, stale: action.changes } : state;
  }
}
