# BRIEFING — 2026-09-21T14:16:00Z

## Mission
Independent review and adversarial challenge of HPRobot.McpBridge UI, architecture, threading, theming, and COM interop for Milestone M2.

## 🔒 My Identity
- Archetype: teamwork_preview_reviewer
- Roles: reviewer, critic
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m2_2
- Original parent: 4d88b310-8910-4f85-b5a8-50216392bc6b
- Milestone: Milestone 2 (HPPowerBi.McpBridge)
- Instance: 1 of 1
- Current parent: a6affb02-3586-4014-be6f-de9dfcd816bd
- Current Milestone: Milestone M2 (HPExcel Solution Setup & Bridge Engine)
- Current Parent: orchestrator_7 (conversation ID: b32c5a58-8b71-46dd-ba9a-5c9e4b6709de)
- Current Milestone: Milestone M2 (HPRobot McpBridge UI & Architecture Review)

## 🔒 Key Constraints
- Review-only — do NOT modify implementation code
- Check for integrity violations (hardcoded test results, facade implementations, shortcuts)
- Evidence-based review and adversarial stress-testing
- Build HPExcel.McpBridge in Debug and Release
- Never place source code or test files in .agents/
- Review UI and architectural integrity of HPRobot.McpBridge
- MaterialDesignThemes 5.3.2 adoption: MaterialThemeBridge.cs, WindowsHostTheme.cs, MaterialBridge.xaml, dynamic resource usage, theme switching
- Architectural isolation: HPRobot/ references ../McpShared/ only, 0 references to sibling host projects
- STA threading model and IOleMessageFilter message pump handling
- Run `dotnet build HPRobot/HPRobot.slnx -c Debug` independently

## Current Parent
- Conversation ID: b32c5a58-8b71-46dd-ba9a-5c9e4b6709de
- Updated: 2026-09-21T14:16:00Z

## Review Scope
- **Files reviewed**:
  - `HPRobot/HPRobot.slnx`
  - `HPRobot/Directory.Build.props`
  - `HPRobot/global.json`
  - `HPRobot/HPRobot.McpBridge/HPRobot.McpBridge.csproj`
  - `HPRobot/HPRobot.McpBridge/Program.cs`
  - `HPRobot/HPRobot.McpBridge/App.xaml` & `App.xaml.cs`
  - `HPRobot/HPRobot.McpBridge/BridgeEntry.cs`
  - `HPRobot/HPRobot.McpBridge/Com/Marshal2.cs`
  - `HPRobot/HPRobot.McpBridge/Com/ComInteropHelper.cs`
  - `HPRobot/HPRobot.McpBridge/Com/RobotAssemblyResolver.cs`
  - `HPRobot/HPRobot.McpBridge/Com/RobotStaWorker.cs`
  - `HPRobot/HPRobot.McpBridge/Com/RobotAttachment.cs`
  - `HPRobot/HPRobot.McpBridge/Units/RobotUnitsPolicy.cs`
  - `HPRobot/HPRobot.McpBridge/Safety/RobotTier.cs`
  - `HPRobot/HPRobot.McpBridge/Safety/RobotTierTable.cs`
  - `HPRobot/HPRobot.McpBridge/Safety/RobotTierAnalyzer.cs`
  - `HPRobot/HPRobot.McpBridge/Safety/RobotSafetyGuard.cs`
  - `HPRobot/HPRobot.McpBridge/Safety/RobotSnapshotManager.cs`
  - `HPRobot/HPRobot.McpBridge/Host/RobotScriptGlobals.cs`
  - `HPRobot/HPRobot.McpBridge/Host/RobotBridgeExecutor.cs`
  - `HPRobot/HPRobot.McpBridge/Host/RobotDispatcher.cs`
  - `HPRobot/HPRobot.McpBridge/ViewModels/MainWindowViewModel.cs`
  - `HPRobot/HPRobot.McpBridge/Views/MainWindow.xaml` & `MainWindow.xaml.cs`
  - `HPRobot/HPRobot.McpBridge/Resources/Themes/ThemeInfo.cs`
  - `HPRobot/HPRobot.McpBridge/Resources/Themes/IHostTheme.cs`
  - `HPRobot/HPRobot.McpBridge/Resources/Themes/WindowsHostTheme.cs`
  - `HPRobot/HPRobot.McpBridge/Resources/Themes/MaterialThemeBridge.cs`
  - `HPRobot/HPRobot.McpBridge/Resources/Themes/MaterialBridge.xaml`
  - `HPRobot/HPRobot.McpBridge/Resources/Themes/ThemeLight.xaml`
  - `HPRobot/HPRobot.McpBridge/Resources/Themes/ThemeDark.xaml`
  - `HPRobot/HPRobot.McpBridge/Resources/Themes/RobotTheme.xaml`

## Key Decisions Made
- Confirmed independent build `dotnet build HPRobot/HPRobot.slnx -c Debug` succeeds with 0 errors, 0 warnings.
- Confirmed architectural isolation: `HPRobot/` references `../McpShared/` only, zero sibling host references.
- Verified STA threading model in `RobotStaWorker` and `IOleMessageFilter` in `ComInteropHelper`.
- Detected Major Finding 1: `FindThemeDictionary(window.Resources)` in `MaterialThemeBridge.cs:39` returns null for `MainWindow`, bypassing `overlay.SetTheme(theme)` and breaking dynamic theme switching.
- Detected Major Finding 2: `RobotDispatcher.HandleAttach` and `HandleDetach` call COM methods on MTA thread-pool thread instead of dispatching to `_executor.StaWorker.RunOnControlLaneAsync`, violating COM apartment boundaries.
- Rendered Verdict: REQUEST_CHANGES.

## Artifact Index
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m2_2\DISPATCH.md` — Task assignment & instructions
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m2_2\BRIEFING.md` — Working memory & state
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m2_2\progress.md` — Liveness heartbeat
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m2_2\handoff.md` — Review report

## Review Checklist
- **Items reviewed**:
  - `HPRobot/HPRobot.slnx`: Valid XML, references HPRobot.McpBridge and 3 McpShared projects. PASS.
  - `HPRobot/Directory.Build.props`: Resolves `Interop.RobotOM.dll` from env/reg/ProgramFiles. PASS.
  - Sibling isolation: zero references to other deliverables. PASS.
  - `WindowsHostTheme.cs`: Registry query & SystemEvents preference handling. PASS.
  - `MaterialThemeBridge.cs`: Scoping defect identified in `FindThemeDictionary`. MAJOR DEFECT.
  - `ThemeLight.xaml`, `ThemeDark.xaml`, `MaterialBridge.xaml`, `RobotTheme.xaml`: Token coverage. PASS.
  - `RobotSafetyGuard.cs`: Master execution & heavy operations gating. PASS.
  - `RobotTierAnalyzer.cs` & `RobotTierTable.cs`: AST analysis & property mutation escalation. PASS.
  - `RobotSnapshotManager.cs`: Retention pruning, `.rtd` file backup. PASS.
  - `RobotUnitsPolicy.cs`: Metric units standardization and restoration in finally. PASS.
  - `RobotStaWorker.cs` & `ComInteropHelper.cs`: STA queue, IOleMessageFilter, and RCW management. PASS.
  - `RobotBridgeExecutor.cs`: STA task dispatch for snapshot and script execution. PASS.
  - `RobotDispatcher.cs`: Apartment boundary violation on `HandleAttach`/`HandleDetach`. MAJOR DEFECT.
- **Verdict**: REQUEST_CHANGES
- **Unverified claims**: Worker M2 claimed full solution setup and safety gating; however, theming dictionary lookup fails on `MainWindow`, and `RobotDispatcher` bypasses the STA worker.

## Attack Surface
- **Hypotheses tested**:
  - Does `MaterialThemeBridge` find `CustomColorTheme` in `MainWindow.Resources`? Result: NO (`FindThemeDictionary` returns null; `overlay.SetTheme` bypassed).
  - Are COM calls from named pipe custom methods (`robot.attach`) dispatched to the STA worker thread? Result: NO (`_attachment.Attach()` invoked directly on thread-pool thread).
  - Can mutating operations bypass Write gating? Result: NO (`RobotTierAnalyzer` escalates property/element assignments on member expressions to `Write`).
  - Does `HPRobot.slnx` build cleanly? Result: YES (0 errors, 0 warnings).
  - Are sibling projects referenced? Result: NO (0 references).
- **Vulnerabilities found**:
  - Theming scoping defect in `MaterialThemeBridge.cs:39`.
  - Thread apartment boundary violation in `RobotDispatcher.cs:57, 80`.
