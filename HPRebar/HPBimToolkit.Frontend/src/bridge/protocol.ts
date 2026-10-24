// Protocol v1 between the page and the Revit add-in. Mirrors HPBimToolkit.Core/Contracts (C#); the C# WireFormatTests
// pin the JSON shape on that side. Field names are camelCase, element ids are numbers (ElementId.Value).

export const PROTOCOL_VERSION = 1;

export type ErrorCode =
  | 'BAD_REQUEST'
  | 'UNKNOWN_COMMAND'
  | 'NO_DOCUMENT'
  | 'BUSY'
  | 'TIMEOUT'
  | 'CANCELLED'
  | 'REVIT_API_ERROR'
  | 'INTERNAL';

export interface BridgeErrorInfo {
  code: ErrorCode;
  message: string;
  details?: unknown;
}

export interface RequestEnvelope {
  v: 1;
  kind: 'request';
  id: string;
  command: string;
  payload?: unknown;
}

export type ResponseEnvelope =
  | { v: 1; kind: 'response'; id: string; ok: true; payload: unknown; error?: null }
  | { v: 1; kind: 'response'; id: string; ok: false; payload?: null; error: BridgeErrorInfo };

export interface EventEnvelope {
  v: 1;
  kind: 'event';
  event: string;
  payload: unknown;
}

export type Theme = 'light' | 'dark';
export type StatisticsScope = 'model' | 'activeView';
export type ShowMode = 'select' | 'zoom';

export interface HelloResult {
  version: string;
  revitVersion: string;
  theme: Theme;
  capabilities: string[];
}

export interface ActiveViewSummary {
  id: number;
  name: string;
  viewType: string;
  /** Scale denominator (100 = 1:100); 0 for views without a scale. */
  scale: number;
}

export interface DocumentSnapshot {
  hasDocument: boolean;
  title: string | null;
  path: string | null;
  isWorkshared: boolean;
  isModified: boolean;
  isReadOnly: boolean;
  isFamily: boolean;
  activeView: ActiveViewSummary | null;
}

export interface CategoryCount {
  categoryId: number;
  name: string;
  count: number;
}

export interface ModelMetrics {
  elementTypes: number;
  views: number;
  sheets: number;
  levels: number;
  families: number;
  warnings: number;
  links: number;
  worksets: number | null;
}

export interface ModelStatistics {
  /** Path of the counted document (title while unsaved); compare with documentKey() to drop numbers of another document. */
  documentKey: string;
  /** The counted view when scope is activeView, else null. */
  viewId: number | null;
  scope: StatisticsScope;
  total: number;
  categories: CategoryCount[];
  metrics: ModelMetrics;
  elapsedMs: number;
}

export interface SelectionSnapshot {
  count: number;
  ids: number[];
  truncated: boolean;
}

export interface ShowElementsRequest {
  categoryId?: number;
  ids?: number[];
  mode: ShowMode;
  scope?: StatisticsScope;
  /** Zoom to what is already selected in Revit (the page only knows the first 200 ids); the selection is not changed. */
  currentSelection?: boolean;
}

export interface ModelStaleEvent {
  added: number;
  deleted: number;
  modified: number;
}

/** Request and response types per command, so `bridge.request` is typed end to end. */
export interface CommandMap {
  'app.hello': { request: undefined; response: HelloResult };
  'dashboard.getDocumentInfo': { request: undefined; response: DocumentSnapshot };
  'dashboard.getStatistics': { request: { scope: StatisticsScope }; response: ModelStatistics };
  'dashboard.getSelection': { request: undefined; response: SelectionSnapshot };
  /** shown = selected; zoomed = how many of them the active view shows (0: nothing to zoom to there). */
  'dashboard.showElements': { request: ShowElementsRequest; response: { shown: number; zoomed: number } };
  'dashboard.openTool': { request: { toolId: string }; response: { opened: boolean } };
}

export interface EventMap {
  'selection.changed': { count: number };
  'document.changed': DocumentSnapshot;
  'model.stale': ModelStaleEvent;
  'theme.changed': { theme: Theme };
}

export type CommandName = keyof CommandMap;
export type EventName = keyof EventMap;
