# BRIEFING — 2026-09-22T00:31:30Z

## Mission
Investigate Tekla Structures 2025.0 Open API environment, assemblies, and In-Process Bridge design.

## 🔒 My Identity
- Archetype: Teamwork explorer
- Roles: Read-only investigation, architectural synthesis
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_2
- Original parent: 5d7560ee-5142-428f-a172-e73cf7738ac1
- Milestone: Tekla Structures 2025.0 Open API & In-Process Bridge Survey

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Write ONLY to working directory: `.agents/teamwork_preview_explorer_survey_2/`
- Output structured report.md and 5-component handoff.md
- Send message to parent via send_message tool

## Current Parent
- Conversation ID: 5d7560ee-5142-428f-a172-e73cf7738ac1
- Updated: 2026-09-22T00:31:30Z

## Investigation State
- **Explored paths**:
  - `C:\Program Files\Tekla Structures\2025.0\bin` (TeklaStructures.exe, 2025.0.48669.0, .NET 4.8)
  - Tekla reference assemblies: `Tekla.Structures.dll`, `Tekla.Structures.Model.dll`, `Tekla.Structures.Catalogs.dll`, `Tekla.Structures.Datatype.dll`, `Tekla.Structures.Drawing.dll`, `Tekla.Structures.Plugins.dll`
  - Registry: `HKLM:\SOFTWARE\Trimble\Tekla Structures\2025.0\setup`
  - Extension paths: `C:\ProgramData\Trimble\Tekla Structures\2025.0\Environments\common\extensions`
  - Ribbon layout: `Environments\common\system\Ribbons\CustomTabs\Modeling\*.xml`
  - Macro layout: `Environments\common\macros\modeling\*.cs`
  - Model directory structure: `C:\TeklaStructuresModels\<Model>\*.db1, *.db2, environment.db`
- **Key findings**:
  1. Tekla Structures 2025.0 installation verified at `C:\Program Files\Tekla Structures\2025.0\bin`. AssemblyVersion = `2025.0.0.0`, TargetFramework = `.NETFramework,Version=v4.8`.
  2. Plugin architecture: In-Process plugin via `PluginBase` (`[Plugin("...")]`), custom ribbon tab XML (`<Tab Header="..."><SimpleButton Command="Plugin.CatalogPluginComponentItem?..."/>`), and companion macro (`.cs`).
  3. Threading: Tekla Model API must execute on the Tekla UI/main thread. Managed using `MainThreadQueue` from `McpShared/HPRebar.McpBridge.Core` (which has native `#if NET48` support), hooked to `ComponentDispatcher.ThreadIdle` / WPF Dispatcher with `PostMessage(WM_NULL)` to wake the thread.
  4. Native dryRun rollback: Discovered `Tekla.Structures.ModelInternal.Operation.SetTestSavePoint()` and `RollbackToTestSavePoint()` inside `Tekla.Structures.Model.dll`. This provides native in-memory rollback without committing changes to disk.
  5. Model snapshots: Tekla model folder contains `<Model>.db1`, `<Model>.db2`, `environment.db`. Snapshot engine can copy these files into `.hptekla_snapshots/` within milliseconds prior to mutating operations.
  6. Architecture aligns directly with `HPNavis.McpBridge` (which also targets `net48`), using `PluginAssemblyResolver` to guarantee smooth Roslyn binding on .NET Framework 4.8.
- **Unexplored areas**: None for survey scope. Ready for report & handoff.

## Key Decisions Made
- Confirmed `HPTekla.McpBridge` must target `net48` and consume `McpShared`'s `net48` asset.
- Adopted `PluginAssemblyResolver` pattern from `HPNavis.McpBridge` to prevent Roslyn assembly binding conflicts on .NET 4.8.
- Selected `SetTestSavePoint()` / `RollbackToTestSavePoint()` for transaction rollback in `dryRun = true`.

## Artifact Index
- DISPATCH.md — Task assignment
- BRIEFING.md — Persistent context & memory
- progress.md — Heartbeat & status tracking
- report.md — Comprehensive technical analysis report
- handoff.md — 5-component self-contained handoff report
