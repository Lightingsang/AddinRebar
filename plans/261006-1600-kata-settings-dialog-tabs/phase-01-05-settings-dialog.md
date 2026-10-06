# Phases 1–5

## 1. Core settings

- `Models/KataJointRebarSettings.cs`: `Stirrups` "5f8a50", `StirrupCountSpec` "None" (pending), `StirrupsThroughJoint` false (pending), `HangerEnabled` true, `Hanger` "2f16", `HangerTopLengthMm` 150, `HangerAngleDegrees` 45.
- `Models/KataBeamOptions.cs`: BottomLayerNoBend, TopNotAnchoredIntoColumnBelow (true), AlwaysBend + AlwaysBendFactor, CutContinuousBarsAtSpanEnds, DifferentNumbersForIdenticalBars.
- `Models/KataShopSettings.cs`: CouplerMinDiameter 30, MaxBarLengthMm 11700, MinLengthForCuttingMm 8800, MinBarLengthFactor 100, RoundLapMm 5, 4 lap-position flags, lap zones 0.25 / 0.2 L from face|centre, PreferFewerLaps, CompressionLapFactor 30, AllowTensionZoneLap, TensionLapFactor, CrankToLapEdgeMm 0, MinLapGapFactor 40, CuttingToleranceMm 50, UseUnitMassTable + UnitMassTable, UseLapTable + LapTable.
- `Models/KataSettingsFile.cs`: `(KataSettings Drawing, KataBeamOptions Pending, KataShopSettings Shop)`.
- `KataSettings`: + `CrankSlope` 6, `JointBeam`, `JointColumn`; `SettingsVersion` 3.
- `Parsers/KataSettingsJson`: sections by prefix (reflection per record), `ReadFile` / `WriteFile`; `Read` / `Write` keep working on the drawing group. Sanitize: joint texts must parse (`nfDaS`, `nfD`), angle 45 | 60, lengths ≥ 0, slope > 0; shop numbers ≥ 0; table rows that do not parse dropped.
- `Parsers/KataJointNotation.cs`: "5f8a50" → (count, Ø, spacing); "2f16" → (count, Ø).

## 2. Core rules

- `KataDetailingRules`: `JointBeam` / `JointColumn` of `KataJointRule(StirrupCount, StirrupDiameter, StirrupSpacing, HangerCount, HangerDiameter, HangerTopLength, HangerAngleDegrees)`, `JointFor(load)`; old scalar members removed. `CrankSlope` from settings.
- `KataJointStirrups`: count / spacing / Ø per load kind; a merged group of both kinds takes the smaller spacing and the larger Ø (warning). Zone gets `Diameter`.
- `KataStirrupZoneResult.Diameter` (0 = the beam's stirrup Ø); numbering and tags read it.
- `KataHangerBarLayout`: per load kind; `HangerEnabled` false → none.

## 3. Revit

- `KataStirrupSetCreator.Create`: bar type per zone Ø; missing type → the run's existing "cần RebarBarType Ø" message.
- `KataRebarTypeResolver.BuildMappingItems`: row "Đai gia cường nút" per extra Ø.

## 4. UI (KataExport)

- `View/KataSettingsView.xaml`: header, `TabControl`, message, buttons.
- `View/Tabs/KataDetailSettingsTab.xaml`, `KataSpecialSettingsTab.xaml`, `KataJointDefaultsTab.xaml`.
- `ViewModel/KataSettingsViewModel` (shell: accept / cancel / reset) + `ViewModel/Tabs/KataDetailSettingsTabViewModel`, `KataSpecialSettingsTabViewModel` (two editable tables), `KataJointDefaultsTabViewModel` (two columns: dầm giao / cột cấy).
- Pending and shop groups carry a "chưa áp dụng" caption.

## 5. Verify

- Tests: JSON round trip of the three groups, v2 file, bad values; hanger `2f14` → Ø14; `HangerEnabled` false → none; joint `4f10a100` → count / spacing / Ø; crank slope; shop / pending changes leave the layout identical; `ThemeTokenCoverageTests`.
- DWG beams pin `5f10a50`.
- Build Debug.R26; live on the copy: open dialog (UIA), edit, save, reopen; generate B01 with defaults → joint stirrups Ø8.
- Docs: rules §16.
