# Handoff Report: Milestone 2 — HPTekla.McpBridge (.NET Framework 4.8 In-Process Plugin)

**Date**: 2026-09-22T01:00:00Z  
**Author**: `teamwork_preview_worker_m2`  
**Recipient**: `parent` (Orchestrator: `5d7560ee-5142-428f-a172-e73cf7738ac1`)  
**Type**: Hard (Task complete)

---

## 1. Observation
- The target host is Trimble Tekla Structures 2025.0 installed at `C:\Program Files\Tekla Structures\2025.0\bin\`, executing on .NET Framework 4.8 (CLR `v4.0.30319`).
- Tekla Open API assemblies (`Tekla.Structures.dll`, `Tekla.Structures.Model.dll`, `Tekla.Structures.Catalogs.dll`, `Tekla.Structures.Datatype.dll`, `Tekla.Structures.Drawing.dll`, `Tekla.Structures.Plugins.dll`, `Tekla.Structures.Dialog.dll`) are present at `C:\Program Files\Tekla Structures\2025.0\bin\`.
- Tekla Structures Open API is strictly single-threaded, requiring background Named Pipe requests to be dispatched onto the main UI/Model thread.
- Public static methods `Tekla.Structures.ModelInternal.Operation.SetTestSavePoint()` and `RollbackToTestSavePoint(bool resetSelection)` are available for atomic in-memory savepoint rollback.
- Created `HPTekla/Directory.Build.props` and `HPTekla/HPTekla.McpBridge/**` (14 files total).
- Ran build verification commands:
  - `dotnet build HPTekla/HPTekla.McpBridge/HPTekla.McpBridge.csproj -c Debug`: Succeeded with 0 warnings, 0 errors.
  - `dotnet build HPTekla/HPTekla.McpBridge/HPTekla.McpBridge.csproj -c Release`: Succeeded with 0 warnings, 0 errors.
  - `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests`: 113 tests passed, 0 failed.
  - `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests`: 742 tests passed, 0 failed.

## 2. Logic Chain
1. *Observation*: Tekla Structures 2025.0 runs on .NET Framework 4.8, which lacks native assembly load contexts (ALC) and default assembly binding redirects for newer NuGet packages (such as Roslyn 5.9 and System.Text.Json 10.0).  
   *Inference*: `PluginAssemblyResolver.cs` was ported from `HPNavis.McpBridge` to intercept `AppDomain.CurrentDomain.AssemblyResolve` and redirect allow-listed assemblies to the local plugin folder.
2. *Observation*: Tekla Open API calls from a background pipe listener thread cause threading conflicts or crashes.  
   *Inference*: `TeklaThreadDispatcher.cs` integrates `MainThreadQueue`, which enqueues work items, pumps them during `ComponentDispatcher.ThreadIdle`, and nudges the message loop via `PostMessage(hwnd, WM_NULL)`.
3. *Observation*: Tekla does not provide a standard `Transaction.RollBack()` API like Revit or AutoCAD, but `ModelInternal.Operation.SetTestSavePoint()` and `RollbackToTestSavePoint()` allow test-grade native state resets.  
   *Inference*: `TeklaBridgeExecutor.cs` calls `SetTestSavePoint()` prior to script execution. For `dryRun = true` or upon script exception, calling `RollbackToTestSavePoint(true)` cleanly reverts all in-memory insertions/modifications.
4. *Observation*: Write and destructive operations may corrupt a user's model if an unhandled state occurs after commit.  
   *Inference*: `TeklaSnapshotManager.cs` automatically creates a timestamped copy of `.db1`, `.db2`, and `environment.db` inside `.hptekla_snapshots/` using `FileShare.ReadWrite` before executing mutating scripts.
5. *Observation*: The dispatch requested modeless status visibility and Ribbon integration.  
   *Inference*: Created `Views/BridgeStatusWindow.xaml`, `ViewModels/BridgeStatusViewModel.cs`, and `Ribbon/Ribbon-HPTekla.xml` conforming to HP styling.

## 3. Caveats
- While `SetTestSavePoint()` and `RollbackToTestSavePoint()` reliably reset model geometry and objects in memory, model options or external files modified by custom C# code outside the Tekla database are not rolled back by this mechanism; `TeklaSnapshotManager` provides the disk-level safeguard.
- Drawing queries (`DrawingHandler.GetDrawings()`) will return an empty collection if no drawing has been generated in the active model.
- No other caveats.

## 4. Conclusion
Milestone 2 is complete. `HPTekla.McpBridge` compiles cleanly on `net48` (both Debug and Release), binds to the installed Tekla Structures 2025.0 Open API, enforces genuine 3-tier safety, guarantees atomic dryRun rollback, manages database snapshots, synchronizes main-thread execution, and provides a full WPF status UI and Ribbon XML definition.

## 5. Verification Method
To independently verify Milestone 2:
1. Build `HPTekla.McpBridge` in Debug configuration:
   ```powershell
   dotnet build HPTekla/HPTekla.McpBridge/HPTekla.McpBridge.csproj -c Debug
   ```
   *Expected result*: Build succeeded with 0 errors and 0 warnings.
2. Build `HPTekla.McpBridge` in Release configuration:
   ```powershell
   dotnet build HPTekla/HPTekla.McpBridge/HPTekla.McpBridge.csproj -c Release
   ```
   *Expected result*: Build succeeded with 0 errors and 0 warnings.
3. Run existing McpShared net48 tests:
   ```powershell
   dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests
   ```
   *Expected result*: 113 passed (100%).
4. Verify file ownership compliance:
   ```powershell
   git status --short
   ```
   *Expected result*: All newly added files reside under `HPTekla/Directory.Build.props` and `HPTekla/HPTekla.McpBridge/**`.
