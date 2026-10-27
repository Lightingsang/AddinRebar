// The Kata Settings page's protocol (HPRebar's KataSettingsWebContracts.cs) and the form it edits (HPRebar.Core's
// KataSettingsForm.cs), camelCase as BridgeJson writes them. settingsContracts.test.ts checks every name against the C#.
import type { TypedBridgeClient } from '../bridge/bridgeClient';

export type KataTheme = 'light' | 'dark';

/** Numbers are null when the input is empty or not a number: the host names the field in its errors. */
export interface KataDetailForm {
  closedStirrupHookAngle: number | null;
  closedStirrupHookFactor: number | null;
  crossTieHookAngle: number | null;
  crossTieHookFactor: number | null;
  roundCutExtraMm: number | null;
  sideBarAnchorageFactor: number | null;
  layerTieMinBarCount: number | null;
  crankMinDiameter: number | null;
  crankSlope: number | null;
  curtailedExtensionMm: number | null;
  denseZoneHeightFactor: number | null;
  endZoneFraction: number | null;
  bottomExtraCutFraction: number | null;
  minimumLegFactor: number | null;
  layerClearGap: number | null;
  roundLegMm: number | null;
  sideBarRequiredHeight: number | null;
  showBarEndMarks: boolean;
  createKataDrawings: boolean;
  bottomLayerNoBend: boolean;
  topNotAnchoredIntoColumnBelow: boolean;
  alwaysBend: boolean;
  alwaysBendFactor: number | null;
  cutContinuousBarsAtSpanEnds: boolean;
  differentNumbersForIdenticalBars: boolean;
  couplerMinDiameter: number | null;
  maxBarLengthMm: number | null;
  minLengthForCuttingMm: number | null;
  minBarLengthFactor: number | null;
  roundLapMm: number | null;
  topLapAtMidspan: boolean;
  topLapAtSupport: boolean;
  bottomLapAtSupport: boolean;
  bottomLapAtMidspan: boolean;
  topLapZoneFraction: number | null;
  topLapZoneFromCentre: boolean;
  bottomLapZoneFraction: number | null;
  bottomLapZoneFromCentre: boolean;
  preferFewerLaps: boolean;
}

export interface KataMassRowForm {
  diameter: number | null;
  kgPerM: number | null;
}

export interface KataLapRowForm {
  diameter: number | null;
  lapCompression: number | null;
  lapTension: number | null;
  anchorCompression: number | null;
  anchorTension: number | null;
}

export interface KataSpecialForm {
  useUnitMassTable: boolean;
  compressionLapFactor: number | null;
  allowTensionZoneLap: boolean;
  tensionLapFactor: number | null;
  crankToLapEdgeMm: number | null;
  minLapGapMm: number | null;
  cuttingToleranceMm: number | null;
  useLapTable: boolean;
  massRows: KataMassRowForm[];
  lapRows: KataLapRowForm[];
}

export interface KataJointForm {
  stirrups: string;
  stirrupCountSpec: string;
  stirrupsThroughJoint: boolean;
  hangerEnabled: boolean;
  hanger: string;
  hangerTopLengthMm: number | null;
  hangerAngleDegrees: number | null;
}

export interface KataJointsForm {
  beam: KataJointForm;
  column: KataJointForm;
}

export interface KataSettingsForm {
  detail: KataDetailForm;
  special: KataSpecialForm;
  joints: KataJointsForm;
}

/** `field` is the form path the error belongs to: "detail.endZoneFraction", "special.lapRows[2].diameter". */
export interface KataSettingsError {
  field: string;
  message: string;
}

export interface KataSettingsFormChoices {
  hookAngles: number[];
  crankMinDiameters: number[];
  crankSlopes: number[];
  couplerMinDiameters: number[];
  stirrupCountSpecs: string[];
  hangerAngles: number[];
}

export interface KataSettingsSnapshot {
  form: KataSettingsForm;
  choices: KataSettingsFormChoices;
  /** Why a saved choice is shown as its default, or null. */
  notice: string | null;
}

export interface KataSettingsPageState {
  theme: KataTheme;
  snapshot: KataSettingsSnapshot;
}

export interface KataSettingsValidation {
  errors: KataSettingsError[];
}

export interface KataSettingsSaveResult {
  saved: boolean;
  /** Saved for this Revit session, but the settings file could not be written. */
  fileFailed: boolean;
  errors: KataSettingsError[];
}

export interface KataSettingsThemeEvent {
  theme: KataTheme;
}

export interface SettingsCommandMap {
  'settings.load': { request: undefined; response: KataSettingsPageState };
  'settings.validate': { request: KataSettingsForm; response: KataSettingsValidation };
  'settings.save': { request: KataSettingsForm; response: KataSettingsSaveResult };
  'settings.defaults': { request: undefined; response: KataSettingsSnapshot };
  'settings.close': { request: undefined; response: null };
}

export interface SettingsEventMap {
  'settings.theme': KataSettingsThemeEvent;
}

export type SettingsBridge = TypedBridgeClient<SettingsCommandMap, SettingsEventMap>;
