# Independent Quality & Adversarial Review Report: Milestone 2 — HPTekla.McpBridge

**Reviewer**: `teamwork_preview_reviewer_m2_1`  
**Date**: 2026-09-22T01:05:00Z  
**Target Subject**: `HPTekla.McpBridge` (.NET Framework 4.8 In-Process Plugin for Trimble Tekla Structures 2025.0)  
**Authoritative Request Reference**: `ORIGINAL_REQUEST.md` (header `## 2026-09-21T17:20:33Z`)  
**Worker Report Reference**: `.agents/teamwork_preview_worker_m2/report.md`  

---

## 1. Review Summary

**Verdict**: **APPROVE**

Milestone 2 delivers a robust, production-grade, in-process MCP bridge plugin for Trimble Tekla Structures 2025.0 running on .NET Framework 4.8. All code was developed from scratch, cleanly separated, fully aligned with the repository's `McpShared` net48 architecture, and adheres strictly to the Integrity Mandate with zero facade implementations or test hacks.

---

## 2. Integrity Verification

As part of the adversarial review protocol, the implementation was thoroughly checked for integrity violations:
- **No hardcoded test outputs or mock responses**: Methods in `TeklaBridgeExecutor` and `TeklaSnapshotManager` execute real Tekla Open API operations and file system routines.
- **No dummy or facade implementations**: Real Roslyn compilation, genuine AST syntactic analysis, real Win32 message loop dispatching, and actual database file stream cloning with `FileShare.ReadWrite`.
- **No shortcuts bypassing task requirements**: Both `Debug` and `Release` configurations compile cleanly without warnings or errors.
- **Genuine verification**: Build results, assembly references, and test suite execution were independently reproduced and verified.

---

## 3. Review Dimensions & Detailed Findings

### 3.1 Project Configuration & Dependency Resolution (`Directory.Build.props` & `.csproj`)
- **TFM**: `HPTekla.McpBridge.csproj` explicitly targets `net48`, fully compatible with Tekla Structures 2025.0 (`TeklaStructures.exe`, CLR `v4.0.30319`).
- **Tekla Open API Assemblies**: Referenced with `<Private>false</Private>` across `Tekla.Structures`, `Tekla.Structures.Model`, `Tekla.Structures.Catalogs`, `Tekla.Structures.Datatype`, `Tekla.Structures.Drawing`, `Tekla.Structures.Plugins`, and `Tekla.Structures.Dialog`. This guarantees that host assemblies are loaded directly from Tekla's process memory and not duplicated into output.
- **Path Resolution**: `Directory.Build.props` resolves Tekla 2025 via CLI property, environment variable `HPTEKLA_TEKLA_DIR`, registry key `HKLM\SOFTWARE\Trimble\Tekla Structures\2025.0\setup`, or fallback to `%ProgramW6432%\Tekla Structures\2025.0\bin\`.
- **McpShared References**: References `HPRebar.McpBridge.Core.csproj` and `HPRebar.Mcp.Contracts.csproj` targeting their `net48` assets.

### 3.2 Assembly Isolation on .NET Framework 4.8 (`PluginAssemblyResolver.cs`)
- **Mechanics**: Intercepts `AppDomain.CurrentDomain.AssemblyResolve` and resolves conflicting dependencies (`Microsoft.CodeAnalysis`, `System.Collections.Immutable`, `System.Text.Json`, `Serilog`, etc.) from the plugin's local directory.
- **Safety Boundary**: Restricted by an explicit `AllowList` and verified through `IsInFolder(requester, folder)` and major version parity (`requested.Major == available.Major`). Foreign plugins asking for different versions of common assemblies are not poisoned by this bridge's copy.
- **Initialization**: Installed idempotently in `HPTeklaBridgePlugin` static constructor and in `BridgeEntry.Start()`.

### 3.3 Plugin Registration & Ribbon Integration (`HPTeklaBridgePlugin.cs` & `Ribbon-HPTekla.xml`)
- **Plugin Definition**: Decorates `PluginBase` with `[Plugin("HPTeklaBridge")]` and `[PluginUserInterface("HPTekla.McpBridge.NullForm")]`.
- **Input Definition**: `DefineInput()` returns an empty list, allowing the bridge to be invoked without forcing the user to pick model points or objects.
- **Execution**: `Run()` invokes `BridgeEntry.ShowStatusWindow()`, which ensures the named pipe listener is active and brings up the WPF status window.
- **Ribbon Manifest**: `Ribbon-HPTekla.xml` defines a custom tab `"HP Tekla AI"` with a `SimpleButton` bound to `Plugin.CatalogPluginComponentItem?HPTeklaBridge`. It is copied to the build output (`PreserveNewest`).

### 3.4 Thread Synchronization & Execution Architecture (`TeklaThreadDispatcher.cs` & `TeklaBridgeExecutor.cs`)
- **Thread Marshaling**: Coordinates cross-thread execution between Named Pipe worker threads and Tekla's UI thread using `MainThreadQueue`.
- **Idle Pumping & Wakeup**: Hooked into `ComponentDispatcher.ThreadIdle` and posts `WM_NULL` via `PostMessage(hwnd, WmNull, ...)` to unblock Tekla's native Windows message loop.
- **Quiescence**: Checks `!Operation.IsMacroRunning()` to ensure Tekla is in a safe, non-busy state before processing scripts.
- **Single-Flight Execution**: Guarded by `Interlocked.CompareExchange(ref _busy, 1, 0)`.
- **Atomic dryRun & Error Rollback**: Uses real native Tekla internal operations:
  - `TeklaOperation.SetTestSavePoint()` before script execution.
  - If `dryRun == true`: calls `TeklaOperation.RollbackToTestSavePoint(resetSelection: true)` to cleanly revert in-memory mutations without polluting the model or undo history.
  - On script exception: calls `TeklaOperation.RollbackToTestSavePoint(resetSelection: true)` to ensure zero partial state leakage.
  - If live write succeeds: commits changes via `_model.CommitChanges(...)`.

### 3.5 3-Tier Safety & Snapshot Engine (`TeklaTierAnalyzer.cs` & `TeklaSnapshotManager.cs`)
- **AST Classification**: Scans Roslyn syntax trees for:
  - Destructive keywords (`Delete`, `DeleteObjects`, `CreateIFC4ExportFromAll`, etc.) -> Tier D (Destructive).
  - Write keywords (`Insert`, `Modify`, `SetUserProperty`, `CommitChanges`, etc.) -> Tier W (Write).
  - Mutating types (`new Beam()`, `new RebarGroup()`, etc.) -> Tier W (Write).
  - Property assignments (`assign.Left is MemberAccessExpressionSyntax`) -> Tier W (Write).
  - Pure queries -> Tier R (Read).
- **Destructive Gating**: Destructive scripts are blocked unless `AllowHeavyOperations` is explicitly checked in the status window.
- **Snapshot Engine**: Copies `.db1`, `.db2`, `environment.db`, `options_model.db` into `.hptekla_snapshots/` before Tier W and Tier D executions. Utilizes `FileShare.ReadWrite` to safely read database files while locked by `TeklaStructures.exe`. Retains newest 20 snapshots and prunes older ones.

### 3.6 WPF Status UI & Theming (`BridgeStatusWindow.xaml`, `TeklaTheme.xaml`, `BridgeStatusViewModel.cs`)
- **Theming**: Implements HP Blue (`#0696D7`) palette, Segoe UI fonts, cards, buttons, and dynamic resources supporting dark/light mode switching.
- **UI State**: Displays pulsing status indicator (Listening, Connected, Busy, Error), pipe name (`hptekla-mcp-2025`), host version, active model name, safety checkboxes, and last script execution preview.
- **MVVM Cleanliness**: Driven by `CommunityToolkit.Mvvm`, cleanly handles detachment on window close, and unbinds heavy operations if execution toggle is turned off.

---

## 4. Adversarial Findings & Observations

### [Minor] Finding 1: Window Owner Handle in WPF Modeless Window
- **Location**: `HPTekla.McpBridge/BridgeEntry.cs:131`
- **What**: The status window is displayed modelessly via `_window.Show()` without attaching `WindowInteropHelper.Owner` to Tekla's main window handle.
- **Why**: While fully functional, clicking inside Tekla's 3D viewport can send the bridge window behind the main Tekla application window, which may lead users to believe the window closed.
- **Suggestion**: Similar to `HPNavis/BridgeEntry.cs:187`, attach owner window:
  ```csharp
  var mainHwnd = Process.GetCurrentProcess().MainWindowHandle;
  if (mainHwnd != IntPtr.Zero) new WindowInteropHelper(_window) { Owner = mainHwnd };
  ```

### [Minor] Finding 2: Target-Typed `new()` in AST Tier Analysis
- **Location**: `HPTekla.McpBridge/TeklaTierAnalyzer.cs:82`
- **What**: Syntactic matching checks `node is ObjectCreationExpressionSyntax creation` but not `ImplicitObjectCreationExpressionSyntax` (target-typed `new()`, e.g. `Beam b = new();`).
- **Impact Assessment**: Minimal to zero in practice, because subsequent statements that actually interact with the object (`b.Insert()` or `b.Profile = ...`) are immediately intercepted by `InvocationExpressionSyntax` or `AssignmentExpressionSyntax`, correctly elevating the tier to `Write`.
- **Suggestion**: In a future refactor, expand to `BaseObjectCreationExpressionSyntax` or inspect symbol metadata if a semantic model is available.

---

## 5. Verified Claims Table

| Claim | Verification Method | Result |
|---|---|---|
| `HPTekla.McpBridge.csproj` compiles on `net48` in Debug | `dotnet build -c Debug` | **PASS** (0 warnings, 0 errors) |
| `HPTekla.McpBridge.csproj` compiles on `net48` in Release | `dotnet build -c Release` | **PASS** (0 warnings, 0 errors) |
| Tekla Open API 2025 installed on dev machine | `Test-Path "C:\Program Files\Tekla Structures\2025.0\bin\Tekla.Structures.dll"` | **PASS** (`True`) |
| Tekla native savepoint / rollback APIs exist | Reflection check on `Tekla.Structures.ModelInternal.Operation` | **PASS** (`SetTestSavePoint`, `RollbackToTestSavePoint` verified) |
| `HPRebar.McpBridge.Core.Net48Tests` passes | `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests` | **PASS** (113/113 passed, 100%) |
| `HPRebar.Mcp.Server.Core.Tests` passes | `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests` | **PASS** (742/742 passed, 100%) |
| Binary output layout complies with Tekla extension structure | `Get-ChildItem bin/Debug/net48` | **PASS** (`HPTekla.McpBridge.dll`, `Ribbon-HPTekla.xml`, loose dependencies, 0 Tekla.Structures copies) |

---

## 6. Conclusion

The `HPTekla.McpBridge` plugin delivered in Milestone 2 is complete, robust, architecturally sound, and ready for integration with the MCP Server and automated test suites in subsequent milestones.

**Final Verdict: APPROVE**
