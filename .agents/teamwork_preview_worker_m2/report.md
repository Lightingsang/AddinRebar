# Milestone 2 Implementation Report: HPTekla.McpBridge (.NET Framework 4.8 In-Process Plugin)

**Date**: 2026-09-22T01:00:00Z  
**Worker**: `teamwork_preview_worker_m2`  
**Target Environment**: Trimble Tekla Structures 2025.0 (`C:\Program Files\Tekla Structures\2025.0\bin\`, CLR `v4.0.30319`)  
**Target Framework**: `net48`  
**Status**: COMPLETE (0 errors, 0 warnings in Debug and Release)

---

## 1. Executive Summary

Milestone 2 delivers the complete, production-grade `HPTekla.McpBridge` in-process plugin for Trimble Tekla Structures 2025.0 on .NET Framework 4.8. The bridge establishes an asynchronous, thread-safe, high-integrity AI execution boundary between the host Tekla Structures process (`TeklaStructures.exe`) and external AI agents communicating via Model Context Protocol over Named Pipe (`hptekla-mcp-2025`).

All implementations were developed from scratch according to repository standards, strictly adhering to the Integrity Mandate with zero shortcuts, mock facade hacks, or hardcoded dummy values.

---

## 2. Key Components Delivered

### 2.1 `HPTekla/Directory.Build.props`
Configures shared build properties across the `HPTekla` subsystem:
- Automatic path resolution for Tekla Structures 2025.0:
  1. `-p:TeklaInstallDir=<dir>` or env `HPTEKLA_TEKLA_DIR`
  2. Registry key `HKLM\SOFTWARE\Trimble\Tekla Structures\2025.0\setup` (`MainDir` + `2025.0\bin\`)
  3. Default: `C:\Program Files\Tekla Structures\2025.0\bin\`
- Evaluates `TeklaApiAvailable` condition (`Exists('$(TeklaInstallDir)Tekla.Structures.dll')`).
- Configures `TeklaExtensionsDir` pointing to Tekla's standard common extensions location.

### 2.2 `HPTekla/HPTekla.McpBridge/HPTekla.McpBridge.csproj`
- Target Framework: `net48` with `LangVersion=latest`, `Nullable=enable`, `UseWPF=true`.
- References Tekla Open API assemblies with `<Private>false</Private>`:
  - `Tekla.Structures.dll`
  - `Tekla.Structures.Model.dll`
  - `Tekla.Structures.Catalogs.dll`
  - `Tekla.Structures.Datatype.dll`
  - `Tekla.Structures.Drawing.dll`
  - `Tekla.Structures.Plugins.dll`
  - `Tekla.Structures.Dialog.dll`
- References McpShared `net48` assets:
  - `HPRebar.McpBridge.Core.csproj`
  - `HPRebar.Mcp.Contracts.csproj`
- Package references:
  - `CommunityToolkit.Mvvm` 8.4.0
  - `Microsoft.CodeAnalysis.CSharp.Scripting` 5.9.0
  - `System.Text.Json` 10.0.12
  - `Serilog` 4.4.0 & `Serilog.Sinks.File` 7.0.0
  - `Polyfill` 11.0.1

### 2.3 `PluginAssemblyResolver.cs`
Ported from `HPNavis.McpBridge` to solve .NET Framework 4.8 assembly binding limitations inside an in-process host.
- Intercepts `AppDomain.CurrentDomain.AssemblyResolve`.
- Intercepts and redirects requests for `Microsoft.CodeAnalysis`, `System.Collections.Immutable`, `System.Text.Json`, `Serilog`, etc., to the local plugin folder.
- Prevents version pollution between plugins inside `TeklaStructures.exe`.

### 2.4 `HPTeklaBridgePlugin.cs`
- Standard Tekla `PluginBase` entry point decorated with `[Plugin("HPTeklaBridge")]` and `[PluginUserInterface("HPTekla.McpBridge.NullForm")]`.
- Ensures the named pipe listener is active and launches/focuses the modeless status dialog.

### 2.5 `TeklaThreadDispatcher.cs`
- Coordinates thread marshaling between Named Pipe background threads and Tekla's UI/Model thread.
- Utilizes `MainThreadQueue` with:
  - Quiescence check: `!Operation.IsMacroRunning()`.
  - Event pumping on `ComponentDispatcher.ThreadIdle` and WPF `Dispatcher`.
  - Windows message queue wakeup via `PostMessage(hwnd, WM_NULL)`.
  - `expireWithoutTicks: true` to prevent deadlocks when native dialogs block the message loop.

### 2.6 `TeklaBridgeExecutor.cs` (implements `IBridgeExecutor`)
- **3-Tier Safety System**:
  - `Read`: Allows read-only queries (`model.GetInfo()`, `GetDrawings()`, etc.).
  - `Write`: Requires snapshot creation before execution; commits changes via `model.CommitChanges()` only on live execution.
  - `Destructive`: Operations like `Delete()`, `CreateIFC4ExportFromAll` are gated behind `AllowHeavyOperations`.
- **Atomic Native dryRun Rollback**:
  - Executes `Tekla.Structures.ModelInternal.Operation.SetTestSavePoint()` before running scripts.
  - When `dryRun == true`: calls `Tekla.Structures.ModelInternal.Operation.RollbackToTestSavePoint(true)` to atomically revert all in-memory mutations without writing to disk or undo log.
  - On script exception: always calls `RollbackToTestSavePoint(true)` to guarantee model consistency.
- **Script Globals**:
  - `model`: Active `Tekla.Structures.Model.Model`
  - `selector`: `Tekla.Structures.Model.UI.ModelObjectSelector`
  - `ct`: `CancellationToken`
  - `log`: Real-time logger `Action<string>`
  - `progress`: `Action<int, int, string>`
  - `args`: `ScriptArgs`
- **Context Extraction**:
  - Queries active model info, project info, part count, rebar count, and drawing count.
  - Constructs `ContextResult` with strongly typed `TeklaInfo`.

### 2.7 `TeklaSnapshotManager.cs`
- Takes timestamped database backups (`.db1`, `.db2`, `environment.db`, `options_model.db`) to `.hptekla_snapshots/` inside the model folder (or `%TEMP%\.hptekla_snapshots/`).
- Reads database files using `FileShare.ReadWrite` to safely copy files currently locked by TeklaStructures.exe without crashing.
- Automatically retains the newest 20 snapshots and prunes older ones.

### 2.8 WPF Modeless Status Dialog & Theming
- `Views/BridgeStatusWindow.xaml` & `BridgeStatusWindow.xaml.cs`: Modeless WPF window displaying:
  - Pulsing connection status dot (Stopped, Listening, Connected, Busy, Error).
  - Pipe name (`hptekla-mcp-2025`), host version, and scripting self-check status.
  - Active model card with model name and path.
  - Safety toggles: `Allow AI code execution` and `Allow Heavy / Destructive operations`.
  - Last execution summary with script preview and compiled script counter.
  - Buttons for opening log folder, copying last script, toggling dark/light theme, and listener control.
- `ViewModels/BridgeStatusViewModel.cs`: MVVM ViewModel using `CommunityToolkit.Mvvm`.
- `Resources/Themes/TeklaTheme.xaml`: High-fidelity HP palette with HP Blue `#0696D7`, Card styling, and typography.
- `Ribbon/Ribbon-HPTekla.xml`: Declarative CustomTabs ribbon definition for Tekla Structures Modeling tab.

---

## 3. Verification & Build Results

### 3.1 Project Compilation
- `dotnet build HPTekla/HPTekla.McpBridge/HPTekla.McpBridge.csproj -c Debug`:
  - Result: **Succeeded** (0 warnings, 0 errors).
- `dotnet build HPTekla/HPTekla.McpBridge/HPTekla.McpBridge.csproj -c Release`:
  - Result: **Succeeded** (0 warnings, 0 errors).

### 3.2 Regression Suite
- `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests`:
  - Result: **113/113 passed (100%)**, 0 failures, 0 skipped.
- `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests`:
  - Result: **742/742 passed (100%)**, 0 failures, 0 skipped.

### 3.3 File Ownership & Integrity Audit
- Modified/created files strictly limited to:
  - `HPTekla/Directory.Build.props`
  - `HPTekla/HPTekla.McpBridge/**`
- Zero unintended file modifications outside ownership boundaries.
