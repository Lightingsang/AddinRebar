## 2026-09-21T13:49:12Z
You are worker_m2_1 (HPRobot Bridge & Safety Worker) for the HPRobot MCP Subsystem.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m2_1\
Your parent is the Project Orchestrator (orchestrator_7, conversation ID: b32c5a58-8b71-46dd-ba9a-5c9e4b6709de).

MANDATORY FIRST STEP:
Read:
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (specifically ## 2026-09-21T13:16:14Z)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_7\PROJECT.md
- Survey findings in:
  - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_robot_1\handoff.md`
  - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_robot_2\handoff.md`
  - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_robot_3\handoff.md`
- Sibling references for architectural pattern:
  - `HPSap2000/HPSap2000.McpBridge/` and `HPEtabs/HPEtabs.McpBridge/` and `HPExcel/HPExcel.McpBridge/`

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

YOUR MISSION (Milestone M2 — HPRobot McpBridge & 3-Tier Safety System):
Build the complete standalone desktop application `HPRobot.McpBridge` in `HPRobot/`:

1. Solution & Build Setup:
   - Create `HPRobot/global.json` (SDK 10.0.300, Microsoft Testing Platform).
   - Create `HPRobot/Directory.Build.props` (resolving `Interop.RobotOM.dll` from `C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll` or setting fallback, and referencing `McpShared/`).
   - Create `HPRobot/HPRobot.slnx` containing `HPRobot.McpBridge`.

2. Project `HPRobot/HPRobot.McpBridge/`:
   - Target framework: `net8.0-windows`, `<UseWPF>true</UseWPF>`.
   - Packages: `MaterialDesignThemes` (5.3.2), `MaterialDesignColors` (5.3.2), `Microsoft.CodeAnalysis.CSharp` (4.12.0), `Microsoft.Extensions.Hosting`.
   - ProjectReference: `../../McpShared/HPRebar.McpBridge.Core/HPRebar.McpBridge.Core.csproj`.
   - COM reference to `Interop.RobotOM.dll` with `<EmbedInteropTypes>false</EmbedInteropTypes>` and `<Private>false</Private>`.
   - `Com/RobotAssemblyResolver.cs` to resolve `Interop.RobotOM.dll` from the Robot install directory if needed.
   - `Com/RobotAttachment.cs`: Detect running `robot.exe`, attach via `Marshal2.GetActiveObject("Robot.Application")`, handle COM lifecycle, connection states (Disconnected, Connecting, Attached).
   - `Units/RobotUnitsPolicy.cs`: Enforce Metric units (Meter, kN, kN·m, MPa) during script execution and restore user preferences in a `finally` block.
   - `Safety/`:
     - `RobotTier.cs`: Read, Write, Delete/Heavy.
     - `RobotTierTable.cs`: Known RobotOM methods/properties mapped to tiers.
     - `RobotTierAnalyzer.cs`: Roslyn AST semantic classification analyzing AST method calls against RobotOM API surface.
     - `RobotSafetyGuard.cs`: Check permissions (`AllowExecution` for Write, `AllowHeavyOperations` for Delete/Heavy). Refuse with -32001 if disallowed.
     - `RobotSnapshotManager.cs`: Automated pre-run `.rtd` model snapshot backup to `.hprobot_snapshots/` beside model or `%TEMP%`. Prune retention to 20 files. Return snapshot file name.
   - `Host/`:
     - `RobotScriptGlobals.cs`: Defines globals `robot` (`IRobotApplication`), `structure` (`IRobotStructure`), `units` (`IRobotUnitMngr`), `args`, `log`, `progress`, `ct`.
     - `RobotBridgeExecutor.cs`: Implements `IBridgeExecutor`.
     - `RobotDispatcher.cs`: Named pipe listener on `hprobot-mcp-2026`, dispatching `robot.ping`, `robot.context`, `robot.execute`, `robot.cancel`, `robot.analyze`.
   - `ViewModels/MainWindowViewModel.cs`: MVVM ViewModel with connection state, model file path, node/bar/panel counts, live execution log, and two safety checkboxes: `AllowExecution` and `AllowHeavyOperations`.
   - `Views/MainWindow.xaml`, `MainWindow.xaml.cs`: Clean MaterialDesignThemes 5.3.2 UI with auto Dark/Light theme switching.
   - `Resources/Themes/`: MaterialThemeBridge, ThemeLight.xaml, ThemeDark.xaml, RobotTheme.xaml.

3. Verify Build:
   - Build `HPRobot/HPRobot.McpBridge/HPRobot.McpBridge.csproj` or `HPRobot/HPRobot.slnx` using `dotnet build`.
   - Ensure 0 warnings and 0 errors.

WRITE OWNERSHIP:
You own exclusively the files in `HPRobot/HPRobot.McpBridge/`, `HPRobot/HPRobot.slnx`, `HPRobot/Directory.Build.props`, and `HPRobot/global.json`.
