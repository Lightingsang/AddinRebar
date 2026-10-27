# Phase 01 — Core form DTO + rules + tests

Priority: high · Status: DONE 2026-10-25 · Type: AFK

## Context
- Source of rules: `HPRebar/HPRebar/KataExport/ViewModel/Tabs/KataDetailSettingsTabViewModel.cs`, `KataSpecialSettingsTabViewModel.cs`,
  `KataJointDefaultsTabViewModel.cs`, `KataShopTableRows.cs`, `KataSettingsViewModel.Collect`.
- Models: `HPRebar.Core/KataRebar/Models/KataSettings*.cs`, `KataShopSettings.cs`, `KataJointRebarSettings.cs`; `KataShopTables`, `KataJointNotation`.

## Requirements
- `KataSettingsForm` record tree: `Detail` (drawing + pending + shop fields), `Special` (+ `MassRows`, `LapRows`), `Joints` (`Beam`, `Column`).
  Numbers as `double?`/`int?` so an emptied input reaches validation instead of failing JSON.
- `KataSettingsFormMapper.FromFile(KataSettingsFile) → (form, notice)`; `ToFile(form) → KataSettingsFile` (only after Validate ok).
- `KataSettingsRules.Validate(form) → IReadOnlyList<KataSettingsError(field, message)>`; field = dotted path (`detail.endZoneFraction`, `special.lapRows[2].diameter`).
  Messages Vietnamese, identical to today's.
- `KataSettingsChoicesDto`: hook angles, crank Ø, slopes, coupler Ø, stirrup specs, hanger angles.

## Steps
1. Characterisation tests first: for each VM rule, a test on the current VM? (VMs are WPF-side, not testable from Core.Tests) → write the rule table in the test from the VM source, one `[Theory]` row per rule.
2. Implement form, mapper, rules; round trip `FromFile(ToFile(f)) == f` on default + a file with tables.
3. Fixture `src/kataSettings/__fixtures__/kata-settings-default.json` written under `HPREBAR_UPDATE_KATA_FIXTURES=1`.

## Success
- Core.Tests green; every VM `Validate` branch has a test row; round trip equal.

## Risks
- Hidden coercion in VMs (`KataSettingsChoices.Pick`, blank table rows ignored) → mapper tests cover both.

## Result
- Code: `HPRebar.Core/KataExport/Settings/` — `KataSettingsForm.cs` (form, rows, error, snapshot), `KataSettingsFormChoices.cs`,
  `KataSettingsFormMapper.cs` (`Snapshot(file)`, `ToFile(form)` throws ArgumentException when invalid), `KataSettingsRules.cs`.
- Tests: `KataSettingsRulesTests` (52 table rows, each edge both sides; all errors reported; refused form never written),
  `KataSettingsFormMapperTests` (defaults, full round trip of a file with every value edited, blank rows, trim/None, notice, fixtures).
- Shared test helper `KataPageFixtures` (wire options + fixture check), scene test now uses it (no behaviour change, its fixtures untouched).
- Fixtures: `HPBimToolkit.Frontend/src/kataSettings/__fixtures__/kata-settings-{default,edited,notice}.json`.
- `dotnet test HPRebar.Core.Tests`: 1690/1690.

## Deliberate differences from the WPF dialog
- One error per wrong field (path + short message) instead of one message per tab; ranges unchanged.
- Select fields (hook angles, crank Ø, slope, coupler Ø, hanger angle) must be a listed value. A saved value outside its list
  opens as the default + notice (WPF did this for crank Ø / slope / coupler Ø only; hook and hanger angles now too).
