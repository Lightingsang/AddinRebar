# Code review — Kata settings dialog (3 tabs, one JSON file, joint defaults)

Date 2026-10-06. Scope: uncommitted `git diff` + untracked files of HPRebar.Core/KataRebar, HPRebar/KataRebar/Service, HPRebar/KataExport (dialog VM + tabs + XAML), KataSettingsFileTests. Excluded: KataStirrupSetCreator.CheckBoundingBox, docs.

## Checks run

| Check | Result |
|---|---|
| `dotnet test --project HPRebar.Core.Tests` (binaries newer than sources) | ✅ 1602/1602 |
| `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false` (scratch artifacts) | ✅ 0 errors, no Kata warnings |
| ThemeTokenCoverageTests (inside the 1602) | ✅ every DynamicResource key defined |
| Dialog in Revit, dark + light | CHƯA TEST |

## Verdict

No Critical / High. Contract holds:
- Diameter flow: zone.Diameter → numbering signature (KataBarNumbering.cs:86), tag text (KataBarTagBuilder.cs:97), runs (KataStirrupRuns.cs:32), curve diameter → weight (KataStirrupZoneLayout.cs:96-98), bar-type rows (KataRebarTypeResolver.cs:93), needed diameters (KataRebarWorkflow.cs:91, also gates KataExport "unmatched" check), per-zone bar type (KataStirrupSetCreator.cs:36-41). Missing type fails early in Workflow.Generate with "Thiếu RebarBarType", creator skip is only defensive. Section painter / section cuts / inner stirrups / ties exclude joint zones, so beam ds stays correct there.
- Centreline geometry: d/2 from out-to-out on every side (yMin+inset, yMax−inset, zBot+inset, zTop=top−b−d/2) — consistent with box and with Revit ScaleToBox + CheckFirstStirrup (barType diameter).
- Pending/Shop never reach rules: Store.Load() returns Drawing only; builder sanitises (KataDetailingRuleBuilder.cs:30); pinned by test.
- JSON: v2 versioned → kept + bumped to 3; unversioned → migrated as before; missing keys default; unknown ignored; Fill works on fresh `with {}` copies (round-trip test); every double written is sanitised first, so no `NaN` token can make the file unreadable.
- Accept: every KataSettings / KataBeamOptions / KataShopSettings / KataJointRebarSettings property is exported by exactly one tab (checked one by one).

## Medium

### M1 — Merged group of two kinds with different spacings breaks into one-stirrup zones labelled with the wrong spacing
KataJointStirrups.cs:82-85 merges at `min(spacing)`; Stretches (KataJointStirrups.cs:119-131) starts a stretch only if the second station is ≤ that spacing + 1. Beam a50 + stub column a100 within merge distance → each column stirrup becomes its own zone, NominalSpacing 50, tag "Ø…a50" per stirrup, Revit single layout each. Defaults (both 5f8a50) unaffected → only after the user sets different spacings per kind. Fix: start a stretch on any step ≤ max spacing of the merged kinds (keep per-group `MaxSpacing`), and label each stretch with its own measured step.

### M2 — Shop tables lose MaterialDesign style (dark theme legibility) — GIẢ ĐỊNH CHƯA XÁC MINH
KataSpecialSettingsTab.xaml:31-42 keyed `Style x:Key="Table" TargetType="DataGrid"` without BasedOn, applied at :58 and :98. An explicit Style suppresses the toolkit's implicit DataGrid style → system (Aero2) template: light column headers with inherited light foreground in dark theme. BasedOn MD key via StaticResource is not reachable from a UserControl during InitializeComponent. Fix: drop the Style, set the properties inline like ColumnRebar/View/Tabs/BarsDivisionTabView.xaml:29-38 (Background/RowBackground/Foreground/BorderBrush DynamicResource). Check dark + light in theme gallery.

## Low

- L1 CrankSlope int round trip: KataDetailSettingsTabViewModel.cs:76 `(int)Math.Round` — hand-edited 6.5 saved back as 6 on any Accept; 7/12 shows an empty ComboBox (value kept); 0.3 → 0 → Validate blocks with a message that never mentions "cổ chai". Fix: keep double, or snap to nearest option and name it in the message.
- L2 Hanger validated while disabled: KataJointDefaultsTabViewModel.cs:61 — unchecked "vai bò" with a bad/empty text blocks Accept. Validate only when HangerEnabled (sanitizer already restores default).
- L3 Joint notation unbounded: KataJointNotation.cs:30 accepts "5f8a0.5" / "5f0.5a50"; spacing < bar diameter → overlapping stirrups, Revit set likely fails → singles. Require spacing ≥ diameter (and diameter ≥ 6) in both TryParseStirrups callers.
- L4 Table rows: KataShopTables.cs:43 `seen.Add(d)` before the row's values are validated → "10:x;10:0.617" drops both rows. Move `seen.Add` after `ok`.
- L5 Editable ComboBox (KataJointDefaultsTab.xaml:29) autocompletes "W+H" to "W+H-1" (IsTextSearchEnabled). Value is "chưa áp dụng"; set `IsTextSearchEnabled="False"` if free expressions matter.
- L6 DataGrid edit then Accept — GIẢ ĐỊNH CHƯA XÁC MINH: a cell still in edit mode when Accept is clicked may not reach the row VM (row BindingGroup). Shop values only. `UpdateSourceTrigger=PropertyChanged` on the column bindings removes the doubt.
- L7 Two defaults: KataDetailingRules.cs:147/150 default `KataJointRule.Kata` (Ø 0 = G6, KataJointRule.cs:17) vs KataSettings default "5f8a50" (Ø8). Only reachable by building rules without the builder (none in product code). Align or document.
- L8 Main/hanger positions use beam ds (KataHangerBarLayout.cs:131-135, ComputeTransverseYPositions); inside a Ø8 joint zone with ds 6 the outer bars overlap the joint stirrup by (8−6) mm. Matches Kata's own simplification; note only.

## Informational
- With G6 ≠ Ø8, joint zones get their own number and later numbers shift by one — expected consequence of the approved diameter change.
- Hanger count > 2 warns per load per re-plan (KataHangerBarLayout.cs:93-94) — fine.

## Unresolved questions
None needing the user.
