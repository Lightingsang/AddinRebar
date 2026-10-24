// Generated from HPBimToolkit.Core/Contracts by TypeScriptContractsTests (HPBimToolkit.Core.Tests). Do not edit by hand:
// change the C# contracts, then run `dotnet test` in HPBimToolkit.Core.Tests with HPBIM_UPDATE_CONTRACTS=1.

export type ErrorCode =
  | 'BAD_REQUEST'
  | 'UNKNOWN_COMMAND'
  | 'NO_DOCUMENT'
  | 'BUSY'
  | 'TIMEOUT'
  | 'CANCELLED'
  | 'REVIT_API_ERROR'
  | 'INTERNAL';

export type Theme = 'light' | 'dark';

export type StatisticsScope = 'model' | 'activeView';

export type ShowMode = 'select' | 'zoom';

export interface HelloResult {
  version: string;
  revitVersion: string;
  theme: Theme;
  capabilities: string[];
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

export interface ActiveViewSummary {
  id: number;
  name: string;
  viewType: string;
  scale: number;
}

export interface StatisticsRequest {
  scope?: StatisticsScope;
}

export interface ModelStatistics {
  documentKey: string;
  viewId: number | null;
  scope: StatisticsScope;
  total: number;
  categories: CategoryCount[];
  metrics: ModelMetrics;
  elapsedMs: number;
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

export interface SelectionSnapshot {
  count: number;
  ids: number[];
  truncated: boolean;
}

export interface ShowElementsRequest {
  categoryId?: number | null;
  ids?: number[] | null;
  mode?: ShowMode;
  scope?: StatisticsScope;
  currentSelection?: boolean;
}

export interface ShowElementsResult {
  shown: number;
  zoomed: number;
}

export interface OpenToolRequest {
  toolId: string;
}

export interface OpenToolResult {
  opened: boolean;
}

export interface SelectionChangedEvent {
  count: number;
}

export interface ModelStaleEvent {
  added: number;
  deleted: number;
  modified: number;
}

export interface ThemeChangedEvent {
  theme: Theme;
}

/** Request and response payload per command, so `bridge.request` is typed end to end. */
export interface CommandMap {
  'app.hello': { request: undefined; response: HelloResult };
  'dashboard.getDocumentInfo': { request: undefined; response: DocumentSnapshot };
  'dashboard.getStatistics': { request: StatisticsRequest; response: ModelStatistics };
  'dashboard.getSelection': { request: undefined; response: SelectionSnapshot };
  'dashboard.showElements': { request: ShowElementsRequest; response: ShowElementsResult };
  'dashboard.openTool': { request: OpenToolRequest; response: OpenToolResult };
}

export interface EventMap {
  'selection.changed': SelectionChangedEvent;
  'document.changed': DocumentSnapshot;
  'model.stale': ModelStaleEvent;
  'theme.changed': ThemeChangedEvent;
}

export type CommandName = keyof CommandMap;
export type EventName = keyof EventMap;
