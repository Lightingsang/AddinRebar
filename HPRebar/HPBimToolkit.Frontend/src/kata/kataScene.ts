// What the Kata Export page draws itself, in model millimetres. Written by hand from
// HPRebar/HPRebar.Core/KataExport/Web/KataWebSceneModels.cs (and the Core enums it uses) — keep both in step;
// kataContracts.test.ts checks every field and enum member.

/** KataDrawingPen: the Kata layer of a line (colour, lineweight, linetype). */
export type KataDrawingPen = 'outline' | 'hidden' | 'grid' | 'thin' | 'bar' | 'stirrup';

/** KataDimStyle: kata_dim_25 (ticks, text) or kata_rai_thep (a stirrup run, arrows, no text). */
export type KataDimStyle = 'kata' | 'run';

export type KataColumnKind = 'support' | 'span' | 'joint' | 'freeEnd';

export type KataSupportKind = 'column' | 'foundation' | 'beam';

export type KataTagKind = 'bars' | 'sideBars' | 'stirrups';

export type KataLeaderArrow = 'none' | 'dotBlank' | 'dotSmall' | 'closed';

export type KataTagLayout = 'oneLine' | 'spacingBelow' | 'centred';

export interface KataWebPoint {
  x: number;
  z: number;
}

export interface KataWebRange {
  start: number;
  end: number;
}

export interface KataWebStationMap {
  origin: number;
  direction: number;
}

export interface KataWebColumn {
  index: number;
  letter: string;
  kind: KataColumnKind;
  extent: KataWebRange;
  row11: string;
  row19: string;
  row20: string;
  row21: string;
  row22: string;
  row23: string;
  spanNumber: number | null;
  spanSection: string | null;
}

export interface KataWebBeam {
  extent: KataWebRange;
  topMm: number;
  bottomMm: number;
  heightMm: number;
}

export interface KataWebUpper {
  extent: KataWebRange;
  text: string;
}

export interface KataWebSupport {
  columnIndex: number;
  kind: KataSupportKind;
  extent: KataWebRange;
  sectionHeightMm: number | null;
  upper: KataWebUpper | null;
  crossingBeamX: number | null;
}

export interface KataWebGrid {
  name: string;
  x: number;
  offsetText: string | null;
}

export interface KataWebDimension {
  extent: KataWebRange;
  text: string;
}

export interface KataWebElevation {
  columns: KataWebColumn[];
  beams: KataWebBeam[];
  supports: KataWebSupport[];
  grids: KataWebGrid[];
  gridDimensions: KataWebDimension[];
  bounds: KataWebRange;
  topMm: number;
  bottomMm: number;
  maxBeamHeightMm: number;
}

export interface KataWebLine {
  pen: KataDrawingPen;
  points: KataWebPoint[];
  keys: string[];
}

export interface KataWebDim {
  x1: number;
  z1: number;
  x2: number;
  z2: number;
  vertical: boolean;
  lineAt: number;
  style: KataDimStyle;
  text: string;
}

export interface KataWebFlag {
  x: number;
  z: number;
  number: number;
  below: boolean;
}

export interface KataWebBubble {
  x: number;
  z: number;
  name: string;
}

export interface KataWebLevel {
  x: number;
  z: number;
  text: string;
}

export interface KataWebTitle {
  x: number;
  z: number;
  name: string;
  scale: string;
}

export interface KataWebTag {
  numbers: number[];
  text: string;
  x: number;
  rowZ: number;
  above: boolean;
  pointsLeft: boolean;
  footZ: number[];
  leaderLength: number;
  insertX: number;
  kind: KataTagKind;
}

export interface KataWebCut {
  number: number;
  x: number;
  spanIndex: number;
}

/** Kata's rebar elevation, tags and cuts in the layout's local X (map with the scene's stationMap). */
export interface KataWebRebar {
  lines: KataWebLine[];
  dims: KataWebDim[];
  flags: KataWebFlag[];
  bubbles: KataWebBubble[];
  level: KataWebLevel | null;
  title: KataWebTitle | null;
  minX: number;
  maxX: number;
  top: number;
  bottom: number;
  tags: KataWebTag[];
  cuts: KataWebCut[];
  barCount: number;
  steelWeightKg: number;
}

export interface KataWebScene {
  elevation: KataWebElevation;
  rebar: KataWebRebar | null;
  stationMap: KataWebStationMap;
}

export interface KataWebVertex {
  x: number;
  z: number;
  bulge: number;
}

export interface KataWebPolyline {
  pen: KataDrawingPen;
  vertices: KataWebVertex[];
}

export interface KataWebSectionBar {
  x: number;
  z: number;
  diameter: number;
  number: number;
}

export interface KataWebLeader {
  points: KataWebPoint[];
  arrow: KataLeaderArrow;
  arrowSize: number;
}

export interface KataWebMark {
  x: number;
  z: number;
  radius: number;
}

export interface KataWebSectionTag {
  x: number;
  z: number;
  pointsRight: boolean;
  text: string;
  numbers: number[];
  spacing: string;
  layout: KataTagLayout;
  blockState: string;
}

/** Kata's section n-n: X across the beam from its centre line, Z up from its top. */
export interface KataWebSection {
  number: number;
  mirror: boolean;
  lines: KataWebPolyline[];
  bars: KataWebSectionBar[];
  leaders: KataWebLeader[];
  marks: KataWebMark[];
  tags: KataWebSectionTag[];
  dims: KataWebDim[];
  title: KataWebTitle;
  minX: number;
  maxX: number;
  top: number;
  bottom: number;
}

export interface KataSceneFrame {
  revision: number;
  scene: KataWebScene | null;
}

export interface KataFocusEvent {
  columnIndex: number | null;
  serial: number;
}

// The 3D view (KataWeb3dModels.cs): the layout's local mm, X along the beam, Y across from its centre line, Z up from
// its top.

export type KataBarRole =
  | 'mainTop'
  | 'mainBottom'
  | 'extraTop'
  | 'extraBottom'
  | 'sideBar'
  | 'crossTie'
  | 'stirrupClosed'
  | 'stirrupCap'
  | 'hangerBar';

export type KataWeb3dBoxKind = 'beam' | 'column';

export interface KataWebPoint3 {
  x: number;
  y: number;
  z: number;
}

/** One bar shape laid at each offset along X; `key` empty for bars that go only with their zone. */
export interface KataWeb3dGroup {
  key: string;
  role: KataBarRole;
  diameter: number;
  number: number;
  points: KataWebPoint3[];
  closed: boolean;
  offsets: number[];
}

export interface KataWeb3dBox {
  x1: number;
  x2: number;
  y1: number;
  y2: number;
  z1: number;
  z2: number;
  kind: KataWeb3dBoxKind;
}

export interface KataWeb3dModel {
  groups: KataWeb3dGroup[];
  concrete: KataWeb3dBox[];
  length: number;
  barCount: number;
}
