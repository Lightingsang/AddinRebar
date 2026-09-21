# Dispatch for Worker Milestone 2: HPTekla.McpBridge (.NET Framework 4.8 In-Process Plugin)

## 2026-09-21T17:50:00Z

- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m2
- Authoritative Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (header ## 2026-09-21T17:20:33Z)
- Survey Explorer 2 Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_2\report.md
- Repo Root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

File Ownership:
You have exclusive write access to:
- `HPTekla/Directory.Build.props`
- `HPTekla/HPTekla.McpBridge/**`

Objective:
Implement the complete `HPTekla.McpBridge` in-process plugin for Tekla Structures 2025.0:
1. `Directory.Build.props`: Configure shared Tekla path (`C:\Program Files\Tekla Structures\2025.0\bin`), version constants, and output paths.
2. `HPTekla.McpBridge.csproj`:
   - TargetFramework: `net48`
   - References `../../McpShared/HPRebar.McpBridge.Core/HPRebar.McpBridge.Core.csproj`
   - References `../../McpShared/HPRebar.Mcp.Contracts/HPRebar.Mcp.Contracts.csproj`
   - References Tekla Open API assemblies (`Tekla.Structures.dll`, `Tekla.Structures.Model.dll`, `Tekla.Structures.Catalogs.dll`, `Tekla.Structures.Datatype.dll`, `Tekla.Structures.Drawing.dll`, `Tekla.Structures.Plugins.dll`) with `Private=false`.
   - References `CommunityToolkit.Mvvm`, `Microsoft.CodeAnalysis.CSharp`, `System.Text.Json`.
3. `PluginAssemblyResolver.cs`: Port the proven resolver from `HPNavis.McpBridge` for assembly binding on .NET 4.8.
4. `HPTeklaBridgePlugin.cs`: In-process Tekla plugin (`[Plugin("HPTeklaBridge")]`, `[PluginUserInterface("...")]`) initializing the bridge host on pipe `hptekla-mcp-2025`.
5. `TeklaThreadDispatcher.cs`: Main thread synchronization using `MainThreadQueue` pumped on `ComponentDispatcher.ThreadIdle` with `PostMessage(hwnd, WM_NULL)` message loop wakeup.
6. `TeklaBridgeExecutor.cs`: Implements `IBridgeExecutor`.
   - Enforces 3-tier safety (Read, Write, Destructive).
   - Gates heavy/destructive operations behind `AllowHeavyOperations`.
   - Enforces `dryRun = true` rollback via `Tekla.Structures.ModelInternal.Operation.SetTestSavePoint()` and `RollbackToTestSavePoint(true)`.
   - Commits changes via `model.CommitChanges()` only when `dryRun == false`.
   - Takes model database snapshots (`.db1`, `.db2`) before mutating scripts via `TeklaSnapshotManager`.
   - Exposes script globals: `model`, `selector`, `ct`, `log`, `args`.
7. `TeklaSnapshotManager.cs`: Handles fast snapshot copy of `.db1` and `.db2` into `.hptekla_snapshots/`.
8. UI & Ribbon:
   - Modeless WPF Status Dialog (`Views/BridgeStatusWindow.xaml`, `ViewModels/BridgeStatusViewModel.cs`) with dark/light theme, connection indicator, real-time log stream, and safety checkboxes.
   - `Ribbon-HPTekla.xml` custom tab definition for Tekla ribbon integration.
9. Build:
   - Run `dotnet build HPTekla/HPTekla.McpBridge/HPTekla.McpBridge.csproj` and ensure it compiles cleanly with 0 errors.
10. Write your report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m2\report.md` and handoff to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m2\handoff.md`.
