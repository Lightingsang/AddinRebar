// Protocol of the Kata Export window (HPRebar, Revit 2026). Written by hand from
// HPRebar/HPRebar/KataExport/View/Web/KataWebContracts.cs — keep both in step (field names are the C# names in camelCase).

import type { TypedBridgeClient } from '../bridge/bridgeClient';
import type { KataFocusEvent, KataSceneFrame, KataWeb3dModel, KataWebSection } from './kataScene';

export type KataTheme = 'dark' | 'light';

export interface KataHeaderState {
  beamName: string;
  beamCount: string;
  height: string;
  width: string;
  slabThickness: string;
  axisGrid: string;
  axisOffset: string;
  elevation: string;
  elementCount: string;
  totalLength: string;
}

/** One Kata column (C, D, …) as it will be written: what it is and the cells of rows 11 and 19–23. */
export interface KataPreviewColumn {
  letter: string;
  kind: string;
  row11: string;
  row19: string;
  row20: string;
  row21: string;
  row22: string;
  row23: string;
}

export interface KataCommandStates {
  export: boolean;
  repick: boolean;
  loadRebar: boolean;
  generateRebar: boolean;
  undoRemoveBars: boolean;
}

export interface KataViewState {
  theme: KataTheme;
  header: KataHeaderState;
  parameters: string[];
  nameParameter: string;
  countParameter: string;
  isReverse: boolean;
  isBusy: boolean;
  status: string;
  hasError: boolean;
  workbook: string;
  warnings: string[];
  columns: KataPreviewColumn[];
  selectedColumn: number;
  selectionText: string;
  showRebar: boolean;
  showSection: boolean;
  showBarTags: boolean;
  can: KataCommandStates;
}

export interface KataUpdateRequest {
  nameParameter?: string;
  countParameter?: string;
  reverse?: boolean;
  selectedColumn?: number;
  showRebar?: boolean;
  showSection?: boolean;
  showBarTags?: boolean;
}

export type KataAction =
  | 'export'
  | 'repick'
  | 'refreshExcel'
  | 'loadRebar'
  | 'generateRebar'
  | 'undoRemoveBars'
  | 'previousSpan'
  | 'nextSpan'
  | 'frameAll'
  | 'close';

export type KataGesture = 'zoom' | 'pan' | 'click' | 'zoomExtents' | 'key';

export interface KataCanvasInput {
  kind: KataGesture;
  x?: number;
  y?: number;
  dx?: number;
  dy?: number;
  notches?: number;
  key?: 'Delete' | 'Escape' | 'z';
  ctrl?: boolean;
}

export interface KataDrawingFrame {
  revision: number;
  width: number;
  height: number;
  svg: string;
  hasBarSelection: boolean;
}

export interface KataSectionRequest {
  number: number;
  /** Seen from the other side; left out = as the elevation shows the run. */
  mirror?: boolean | null;
}

export interface KataRemoveBarsRequest {
  keys: string[];
}

export interface KataCommandMap {
  'kata.getState': { request: undefined; response: KataViewState };
  'kata.update': { request: KataUpdateRequest; response: KataViewState };
  'kata.run': { request: { action: KataAction }; response: { started: boolean } };
  'kata.viewport': { request: { width: number; height: number }; response: null };
  'kata.canvas': { request: KataCanvasInput; response: { handled: boolean } };
  'kata.section': { request: KataSectionRequest; response: KataWebSection | null };
  'kata.removeBars': { request: KataRemoveBarsRequest; response: null };
  'kata.model3d': { request: undefined; response: KataWeb3dModel };
}

export interface KataEventMap {
  'kata.state': KataViewState;
  'kata.drawing': KataDrawingFrame;
  'kata.scene': KataSceneFrame;
  'kata.focus': KataFocusEvent;
}

export type KataBridge = TypedBridgeClient<KataCommandMap, KataEventMap>;
