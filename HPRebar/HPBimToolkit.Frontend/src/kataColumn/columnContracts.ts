// The Kata Column Rebar page's protocol (HPRebar's KataColumnWebContracts.cs) and the preview it shows (HPRebar.Core's
// KataColumnPreview.cs), camelCase as BridgeJson writes them. columnContracts.test.ts checks every name against the C#.
import type { TypedBridgeClient } from '../bridge/bridgeClient';

export type KataTheme = 'light' | 'dark';

/** One storey, bottom to top: the sheet cells (A, B, C, D, K) and the measured section behind them, in mm. */
export interface KataColumnPreviewRow {
  level: string;
  heightM: number;
  /** Signed as Kata reads it: negative keeps the left face of the storey below; the diameter of a circle. */
  sizeB: number;
  /** 0 for a circle. */
  sizeH: number;
  beamDepthMm: number;
  shape: 'rectangle' | 'circle';
  /** 'stub': the row above the top storey, which Kata draws only as the bars' lap. */
  placement: 'first' | 'centred' | 'leftFace' | 'stub';
  baseMm: number;
  topMm: number;
  minXMm: number;
  maxXMm: number;
  minYMm: number;
  maxYMm: number;
}

export interface KataColumnPreview {
  name: string;
  count: number;
  crossingBeamWidthMm: number | null;
  slabThicknessMm: number | null;
  pileCapBottomM: number;
  pileCapHeightM: number;
  rows: KataColumnPreviewRow[];
  warnings: string[];
}

export interface KataColumnWorkbook {
  ready: boolean;
  name: string;
  error: string;
}

/** One storey of the rebar plan (HPRebar.Core's KataColumnRebarSummary.cs). */
export interface KataColumnRebarSummaryRow {
  level: string;
  barCount: number;
  /** "24Ø25 + 16Ø20". */
  mainBars: string;
  /** "Ø10: a100 ×10, a200 ×3, a100 ×10". */
  hoops: string;
  tieCount: number;
  numbers: string;
}

export interface KataColumnRebarSummary {
  name: string;
  starterCount: number;
  barCount: number;
  rows: KataColumnRebarSummaryRow[];
  notes: string[];
  errors: string[];
  /** One section per storey (none while the sheet has errors). */
  sections: KataColumnSectionDrawing[];
  /** The elevation along b drawn beside the sections (null while the sheet has errors). */
  elevation: KataColumnElevationDrawing | null;
}

/** Elevation mm: x along b from the lowest storey's centre, z absolute (KataColumnDrawing.cs). */
export interface KataColumnElevationPoint {
  x: number;
  z: number;
}

export interface KataColumnDrawingStorey {
  storey: number;
  name: string;
  baseMm: number;
  topMm: number;
  leftMm: number;
  rightMm: number;
  beamDepthMm: number;
}

export interface KataColumnDrawingLevel {
  name: string;
  zMm: number;
}

export type KataColumnBarKind = 'starter' | 'main' | 'top';

export type KataColumnBarGroup = 'a' | 'b';

/** The outermost bar of its group on its face per storey and layer; `count` bars of that group, storey and Ø share one label, on the `labelled` bar. */
export interface KataColumnDrawingBar {
  number: number;
  storey: number;
  kind: KataColumnBarKind;
  group: KataColumnBarGroup;
  layer: number;
  diameterMm: number;
  lengthMm: number;
  count: number;
  labelled: boolean;
  points: KataColumnElevationPoint[];
}

export interface KataColumnDrawingHoopZone {
  storey: number;
  number: number;
  diameterMm: number;
  spacingMm: number;
  count: number;
  fromMm: number;
  toMm: number;
  leftMm: number;
  rightMm: number;
}

export interface KataColumnDrawingCut {
  storey: number;
  name: string;
  zMm: number;
}

/** A chain of touching dimensions (KataDimChains.cs): stations along z when vertical, the line at `lineAt`. */
export interface KataDimChain {
  vertical: boolean;
  lineAt: number;
  stations: { along: number; across: number }[];
}

export interface KataColumnElevationDrawing {
  /** Under the elevation, as VC writes it: "C1 (SL=1)". */
  title: string;
  leftMm: number;
  rightMm: number;
  bottomMm: number;
  topMm: number;
  pileCapLeftMm: number;
  pileCapRightMm: number;
  storeys: KataColumnDrawingStorey[];
  levels: KataColumnDrawingLevel[];
  bars: KataColumnDrawingBar[];
  hoopZones: KataColumnDrawingHoopZone[];
  cuts: KataColumnDrawingCut[];
  chains: KataDimChain[];
}

/** A bar of a section, section-local mm: origin = the storey's min corner, X along b, Y along h (KataColumnSectionDrawing.cs). */
export interface KataColumnSectionBar {
  x: number;
  y: number;
  diameterMm: number;
  layer: number;
  number: number;
}

/** "closed": the rectangle (x1, y1)–(x2, y2) on the tie's centre line; "cross": the leg from (x1, y1) to (x2, y2). */
export interface KataColumnSectionTie {
  kind: 'closed' | 'cross';
  x1: number;
  y1: number;
  x2: number;
  y2: number;
  number: number;
}

export interface KataColumnSectionDrawing {
  level: string;
  cutElevationMm: number;
  isRound: boolean;
  widthMm: number;
  depthMm: number;
  /** Face to the hoop's centre line: cover + hoop Ø / 2. */
  hoopOffsetMm: number;
  hoopDiameterMm: number;
  /** The storey's densest hoop zone. */
  hoopSpacingMm: number;
  hoopNumber: number;
  tieDiameterMm: number;
  tieSpacingMm: number;
  bars: KataColumnSectionBar[];
  ties: KataColumnSectionTie[];
}

export type KataColumnMode = 'export' | 'rebar';

export interface KataColumnPageState {
  theme: KataTheme;
  mode: KataColumnMode;
  /** Step the page opens on. */
  /** Step 1: the stack to write. */
  preview: KataColumnPreview | null;
  /** Step 2: the plan read from sheet Cot (null while Excel cannot be read). */
  rebar: KataColumnRebarSummary | null;
  workbook: KataColumnWorkbook;
}

export interface KataColumnWriteRequest {
  name: string;
  count: number;
}

export interface KataColumnWriteResponse {
  success: boolean;
  storeysWritten: number;
  error: string;
  /** Storeys still without a bar count (E): Kata's VC refuses them until typed. */
  rowsWithoutBars: string[];
  workbook: KataColumnWorkbook;
}

export interface KataColumnGenerateResponse {
  success: boolean;
  created: number;
  deleted: number;
  warnings: string[];
  error: string;
}

export interface KataColumnThemeEvent {
  theme: KataTheme;
}

export interface ColumnCommandMap {
  'column.load': { request: undefined; response: KataColumnPageState };
  'column.probe': { request: undefined; response: KataColumnWorkbook };
  'column.write': { request: KataColumnWriteRequest; response: KataColumnWriteResponse };
  'column.generate': { request: undefined; response: KataColumnGenerateResponse };
  'column.reload': { request: undefined; response: KataColumnPageState };
  'column.close': { request: undefined; response: null };
}

export interface ColumnEventMap {
  'column.theme': KataColumnThemeEvent;
}

export type ColumnBridge = TypedBridgeClient<ColumnCommandMap, ColumnEventMap>;
