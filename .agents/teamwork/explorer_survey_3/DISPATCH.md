# Dispatch: Explorer Survey 3 (Revit Rebar Generation, MVVM UI & Ribbon in HPRebar)

- **Role**: teamwork_preview_explorer
- **Task**: Map existing Revit rebar creation, element tagging & idempotency, WPF MVVM dialogs, and ribbon integration in `HPRebar`.
- **Authoritative Request**: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (under ## 2026-09-27T15:57:37Z)
- **Scope**:
  1. Investigate `HPRebar/Beam Rebar/` (`BeamRebarCreationService.cs`, `BeamStirrupCreator.cs`, `BeamMainBarCreator.cs`, `BeamAdditionalBarCreator.cs`) and `HPRebar/KataExport`.
  2. Examine how `Rebar.CreateFromCurves` / `CreateFromCurvesAndShape` is invoked in Revit 2026 (.NET 8).
  3. Investigate `RebarBarType` resolution by diameter (~18mm +/- 0.5mm) and standard `RebarHookType` in the Revit document.
  4. Investigate idempotency: locating existing rebars tagged with `Comments = "HPRebar_Kata_{BeamName}"` and cleanly deleting them before recreation.
  5. Investigate UI and Ribbon: how `KataExport` registered its button in `HPRebar/Application.cs`, how WPF MVVM views use `ThemeDark`/`ThemeLight` dynamic resources and MaterialDesign 5.3.2 tokens.
- **Deliverable**: Write a comprehensive survey report to `report.md` in your working directory and deliver `handoff.md`.
