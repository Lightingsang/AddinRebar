# BRIEFING — 2026-10-17T14:31:31Z

## Mission
Survey HPKata.Revit commands (FramingRebar, ColumnRebar, WallRebar), element geometry extraction, picker fallback logic, and HPKata.UI.Tests infrastructure for MahApps.Metro migration & geometry extraction upgrade.

## 🔒 My Identity
- Archetype: explorer
- Roles: investigator, synthesizer
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_3
- Original parent: b20bb4a5-dd75-4c48-9f4d-621e02412393
- Milestone: M0 Exploration & Architecture Survey
- New parent (2026-10-17): 6ca2dcc3-e478-4dfe-bcd3-44e77f9ffa52 (orchestrator_19)
- Current Milestone: Survey for MahApps.Metro Migration & Revit Geometry Reader

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Clean Code Governance (HP_CLEAN_CODE_CORE.md, host-appendix/revit.md, AGENTS.md)
- Output findings to handoff.md
- Seeds design must strictly respect < 300 lines rule, 0 empty catch, no forbidden namespaces
- In HPKata.Revit, 304.8 must appear only inside RevitUnitConverter.cs (Rule R5)
- Every C# file <= 300 lines, methods <= 50 lines
- --disable-build-servers -m:1 for any build/test invocation

## Current Parent
- Conversation ID: 6ca2dcc3-e478-4dfe-bcd3-44e77f9ffa52 (orchestrator_19)
- Updated: 2026-10-17T14:31:31Z

## Investigation State
- **Explored paths**:
  - `HPKata/HPKata.Revit/Commands/FramingRebarCommands.cs`, `ColumnRebarCommands.cs`, `WallRebarCommands.cs`
  - `HPKata/HPKata.Revit/Services/` (FramingRebarGenerator, ColumnRebarGenerator, WallRebarGenerator, RevitFramingAnalyzer)
  - `HPKata/HPKata.Revit/Units/RevitUnitConverter.cs` (Rule R5 boundary)
  - `HPKata/HPKata.UI/ViewModels/` (BeamDetailingViewModel, ColumnDetailingViewModel, WallDetailingViewModel)
  - `HPKata/HPKata.UI/Themes/Theme.xaml` and `MaterialBridge.xaml`
  - `HPKata/HPKata.UI/Views/` (all 13 `.xaml` and `.xaml.cs` views)
  - `HPKata/HPKata.UI.Tests/` (`HPKata.UI.Tests.csproj`, `ViewModels/`, `Converters/`)
  - `HPKata/HPKata.Revit.Tests/`
- **Key findings**:
  - `RevitElementGeometryReader.cs` does not exist in `HPKata.Revit/Services/` and must be created.
  - Commands currently bypass pre-selection (`GetElementIds()`), abort on ESC rather than fallback to manual mode, and do not extract dimensions $b \times h \times L/H$.
  - `Theme.xaml` is missing explicit keys `StandardDataGrid`, `Button.Primary`, `Button.Secondary`, `GroupBox.Header`, `Text.Label`, causing runtime `StaticResourceExtension` failures in Revit.
  - `HPKata.UI.Tests` currently lacks `Views/AllWindowsXamlInstantiationTests.cs`; STA thread pattern is required for instantiating the 13 MetroWindows.
- **Unexplored areas**: None for survey scope. Implementation plan handed off.

## Key Decisions Made
- Outlined complete architecture for `RevitElementGeometryReader.cs`, picker fallback in commands, and STA test suite in `handoff.md`.

## Artifact Index
- handoff.md — Comprehensive survey and architecture report
- progress.md — Liveness heartbeat
