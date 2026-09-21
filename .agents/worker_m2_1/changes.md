# Implementation Changes — Milestone M2 (HPRobot McpBridge & Safety)

**Worker:** `worker_m2_1`  
**Milestone:** M2 — HPRobot McpBridge & 3-Tier Safety System  
**Date:** 2026-09-21  

---

## 1. Summary of Changes

Milestone M2 establishes the complete standalone desktop application `HPRobot.McpBridge` in `HPRobot/` running on .NET 8.0-windows with WPF and MaterialDesignThemes 5.3.2. It connects out-of-process via COM to Autodesk Robot Structural Analysis Professional 2026 (`robot.exe` and `Interop.RobotOM.dll`), hosts the Named Pipe `hprobot-mcp-2026`, enforces a 3-tier safety system (`Read`, `Write`, `DeleteHeavy`), executes scripts under standardized Metric units (`m`, `kN`, `kN·m`, `MPa`), and provides automated pre-run `.rtd` snapshot backups.

---

## 2. File Inventory

### Solution & Build Configuration
1. **`HPRobot/global.json`**
   - Pins .NET SDK `10.0.300` and Microsoft Testing Platform runner.
2. **`HPRobot/Directory.Build.props`**
   - Resolves `Interop.RobotOM.dll` path from environment (`HPROBOT_ROBOT_DIR`), Windows Registry (`CLSID\{F7870790-CDE5-11D1-8FF1-00A02447BAAE}\LocalServer32`), or default Program Files directory (`C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\`).
   - Defines `RobotApiAvailable` property.
3. **`HPRobot/HPRobot.slnx`**
   - Standard XML solution file referencing `HPRobot.McpBridge` and shared projects from `McpShared/` (`HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Server.Core`).
4. **`HPRobot/README.md`**
   - Architectural documentation and deliverable overview.

### Application Core & Entry
5. **`HPRobot/HPRobot.McpBridge/HPRobot.McpBridge.csproj`**
   - Target framework `net8.0-windows`, `<UseWPF>true</UseWPF>`, `<StartupObject>HPRobot.McpBridge.Program</StartupObject>`.
   - References `Interop.RobotOM.dll` with `<Private>false</Private>` and `<EmbedInteropTypes>false</EmbedInteropTypes>`.
   - PackageReferences: `CommunityToolkit.Mvvm` (8.4.0), `MaterialDesignThemes` (5.3.2), `MaterialDesignColors` (5.3.2), `Microsoft.CodeAnalysis.CSharp` (5.9.0), `Microsoft.Extensions.Hosting` (8.0.0), `Serilog` (4.4.0), `Serilog.Sinks.File` (7.0.0).
   - ProjectReferences: `HPRebar.Mcp.Contracts.csproj` and `HPRebar.McpBridge.Core.csproj`.
6. **`HPRobot/HPRobot.McpBridge/Program.cs`**
   - `[STAThread]` entry point with single-instance mutex `"Global\HPRobot.McpBridge.SingleInstance"` and global AppDomain unhandled exception handler.
7. **`HPRobot/HPRobot.McpBridge/App.xaml` & `App.xaml.cs`**
   - WPF application lifecycle, merged theme dictionaries, shutdown on main window close with running script guard.
8. **`HPRobot/HPRobot.McpBridge/BridgeEntry.cs`**
   - Bootstrap orchestrator: initializes Serilog file logger (`%LocalAppData%\HPRobot\McpBridge\logs`), installs assembly resolver, initializes STA worker, attachments, safety guard, snapshot manager, Roslyn compiler, dispatcher, and starts `McpBridgeHost` on named pipe `hprobot-mcp-2026`.

### COM Interop & STA Worker
9. **`HPRobot/HPRobot.McpBridge/Com/Marshal2.cs`**
   - P/Invoke wrapper for `CLSIDFromProgID` and `oleaut32!GetActiveObject` to locate active COM objects from the Windows Running Object Table (ROT).
10. **`HPRobot/HPRobot.McpBridge/Com/ComInteropHelper.cs`**
    - Implements native `IOleMessageFilter` handling `SERVERCALL_RETRYLATER` and modal dialog delays up to 30 seconds.
    - Utility methods for safe RCW release (`ReleaseComObject`, `FinalReleaseComObject`).
11. **`HPRobot/HPRobot.McpBridge/Com/RobotAssemblyResolver.cs`**
    - Hooks `AssemblyLoadContext.Default.Resolving` and `AppDomain.CurrentDomain.AssemblyResolve` to dynamically locate and load `Interop.RobotOM.dll` from the Robot install directory.
12. **`HPRobot/HPRobot.McpBridge/Com/RobotStaWorker.cs`**
    - Dedicated background thread configured as `ApartmentState.STA`.
    - Implements dual queues: a prioritized control lane for fast state checks/attach/detach, and a script execution lane.
13. **`HPRobot/HPRobot.McpBridge/Com/RobotAttachment.cs`**
    - Manages connection lifecycle to `Robot.Application` (`Disconnected`, `Connecting`, `Attached`).
    - Detects active model file, structure type, and queries object counts via `GetAll().Count` on nodes, bars, panels, and cases.
    - Observes host process termination via `Process.Exited`.

### Standardized Units Policy
14. **`HPRobot/HPRobot.McpBridge/Units/RobotUnitsPolicy.cs`**
    - Forces standard Metric units (`m` for length, `kN` for force, `kN*m` for moment, `MPa` for stress) on `IRobotUnitMngr` during script execution.
    - Restores previous user preferences in a `finally` block and logs warnings if restoration encounters exceptions.

### 3-Tier Safety & Snapshot Engine
15. **`HPRobot/HPRobot.McpBridge/Safety/RobotTier.cs`**
    - Enum: `Read` (0), `Write` (1), `DeleteHeavy` (2).
16. **`HPRobot/HPRobot.McpBridge/Safety/RobotTierTable.cs`**
    - Comprehensive classification dictionary of known RobotOM members and heuristic fallback rules for AST classification.
17. **`HPRobot/HPRobot.McpBridge/Safety/RobotTierAnalyzer.cs`**
    - Roslyn C# syntax and semantic analyzer classifying AST method calls, member accesses, and property assignments.
18. **`HPRobot/HPRobot.McpBridge/Safety/RobotSafetyGuard.cs`**
    - UI permission gating: `IsExecutionEnabled` (master gate) and `IsHeavyOperationsEnabled` (FEA solver and structural deletion gate).
    - Throws `BridgeRequestException(BridgeErrorCode.ExecutionDisabled, ...)` (-32001) if an action violates active gate permissions.
19. **`HPRobot/HPRobot.McpBridge/Safety/RobotSnapshotManager.cs`**
    - Pre-run snapshot manager saving timestamped `.rtd` backups to `.hprobot_snapshots/` adjacent to the model (or `%TEMP%\.hprobot_snapshots`).
    - Implements retention pruning retaining the 20 newest snapshots.

### Host Dispatcher & Roslyn Globals
20. **`HPRobot/HPRobot.McpBridge/Host/RobotScriptGlobals.cs`**
    - Defines script global contract: `robot` (`IRobotApplication`), `structure` (`IRobotStructure`), `units` (`IRobotUnitMngr`), `args` (`ScriptArgs`), `log` (`Action<string>`), `progress` (`Action<int, int?, string?>`), `ct` (`CancellationToken`).
21. **`HPRobot/HPRobot.McpBridge/Host/RobotBridgeExecutor.cs`**
    - Implements `IBridgeExecutor`. Coordinates guard validation (`GuardProfile.Robot`), AST tier analysis, safety permission gating, pre-run snapshot capture, Metric units enforcement, STA execution, result serialization, and context assembly (`ContextResult` with `RobotInfo`).
22. **`HPRobot/HPRobot.McpBridge/Host/RobotDispatcher.cs`**
    - Hooked into `McpBridgeHost` for custom JSON-RPC methods (`robot.attach`, `robot.detach`).

### UI (WPF / MVVM / Themes)
23. **`HPRobot/HPRobot.McpBridge/ViewModels/MainWindowViewModel.cs`**
    - MVVM ViewModel with connection state, model file path, node/bar/panel/case counts, safety gating checkboxes, and command bindings.
24. **`HPRobot/HPRobot.McpBridge/Views/MainWindow.xaml` & `MainWindow.xaml.cs`**
    - MaterialDesignThemes 5.3.2 interface featuring pipe listener status card, Robot connection card, 3-tier safety controls, compilation activity, and diagnostic folder links.
25. **`HPRobot/HPRobot.McpBridge/Resources/Themes/ThemeInfo.cs`**
26. **`HPRobot/HPRobot.McpBridge/Resources/Themes/IHostTheme.cs`**
27. **`HPRobot/HPRobot.McpBridge/Resources/Themes/WindowsHostTheme.cs`**
28. **`HPRobot/HPRobot.McpBridge/Resources/Themes/MaterialThemeBridge.cs`**
29. **`HPRobot/HPRobot.McpBridge/Resources/Themes/MaterialBridge.xaml`**
30. **`HPRobot/HPRobot.McpBridge/Resources/Themes/ThemeLight.xaml`**
31. **`HPRobot/HPRobot.McpBridge/Resources/Themes/ThemeDark.xaml`**
32. **`HPRobot/HPRobot.McpBridge/Resources/Themes/RobotTheme.xaml`**

---

## 3. Verification

- `dotnet build HPRobot/HPRobot.McpBridge/HPRobot.McpBridge.csproj -c Debug`: **0 Errors, 0 Warnings**.
- `dotnet build HPRobot/HPRobot.slnx -c Debug`: **0 Errors, 0 Warnings**.
- `dotnet build HPRobot/HPRobot.slnx -c Release`: **0 Errors, 0 Warnings**.
- `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj`: **385 Passed, 0 Failed**.
- `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj`: **71 Passed, 0 Failed**.
