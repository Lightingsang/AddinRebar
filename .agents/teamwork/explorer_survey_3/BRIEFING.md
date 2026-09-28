# BRIEFING — 2026-09-27T16:02:00Z

## Mission
Investigate Revit 2026 rebar creation APIs, RebarBarType/RebarHookType resolution, idempotency & cleanup, transaction handling, and WPF MVVM UI/Ribbon integration for KataRebar.

## 🔒 My Identity
- Archetype: explorer
- Roles: teamwork_preview_explorer
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3
- Original parent: aa8876fc-b61d-4725-aacd-616632eb9cc0
- Milestone: Kata Rebar Revit-Layer Survey

## 🔒 Key Constraints
- Read-only investigation — do NOT implement source code
- Work exclusively inside .agents/teamwork/explorer_survey_3/
- Strict 5-component handoff report format

## Current Parent
- Conversation ID: aa8876fc-b61d-4725-aacd-616632eb9cc0
- Updated: not yet

## Investigation State
- **Explored paths**: `HPRebar/BeamRebar/Service/` (`BeamMainBarCreator.cs`, `BeamStirrupCreator.cs`, `BeamAdditionalBarCreator.cs`, `BeamSideBarCreator.cs`, `BeamRebarOrchestrator.cs`, `RebarTypeCatalog.cs`, `RebarFailureHandling.cs`), `HPRebar/FoundationRebar/Service/FoundationRebarCreationService.cs`, `HPRebar/KataExport/` (`KataExportCommand.cs`, `KataExportExternalEventHandler.cs`, `View/KataExportView.xaml`, `View/KataExportView.xaml.cs`, `Service/ExcelComAttach.cs`), `HPRebar/Application.cs`, `HPRebar/Resources/Icons/RibbonIcons.cs`, `HPRebar/Resources/Themes/MaterialThemeBridge.cs`.
- **Key findings**: 
  - `Rebar.CreateFromCurves` with `#pragma warning disable CS0618` and `Polyline3.Simplify(1.0)` is standard for longitudinal and extra bars; `Rebar.CreateFromRebarShape` + `ScaleToBox` + `SetLayoutAsNumberWithSpacing` for stirrup sets.
  - `RebarBarType` lookup matches diameter $\pm 0.5\text{ mm}$ + `BarModelType.Deformed` for main bars $\ge 12\text{ mm}$.
  - Idempotency via `Comments = $"HPRebar_Kata_{beamName}"` + `Partition = $"Kata_{beamName}"`, collector query on host beam IDs, clean deletion in sub-transaction 1.
  - `TransactionGroup.Assimilate()` ensures single Undo item and full atomic rollback on failure; `IFailuresPreprocessor` (`SwallowWarnings`) suppresses non-fatal warnings.
  - Modeless WPF MVVM window with `ExternalEvent` + `IExternalEventHandler`, dynamic theme adaptation via `MaterialThemeBridge.Attach` and vector icon in `RibbonIcons.cs`.
- **Unexplored areas**: None. All 6 tasks investigated and synthesized.

## Key Decisions Made
- Architecture proposed with clean separation: `HPRebar.Core/KataRebar/` (0 Revit references) and `HPRebar/KataRebar/` (Revit add-in feature layer).

## Artifact Index
- `report.md`: Detailed investigation report on Rebar creation, BarType resolution, Idempotency, TransactionGroup, UI/Ribbon, and proposed architecture.
- `handoff.md`: 5-component hard handoff report for parent agent.
- `progress.md`: Liveness tracker.
