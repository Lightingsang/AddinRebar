# BRIEFING — 2026-09-21T14:04:00Z

## Mission
Build the complete standalone desktop application `HPRobot.McpBridge` in `HPRobot/` with 3-tier safety system, COM attachment, Roslyn analyzer, units policy, snapshot manager, and MaterialDesignThemes UI.

## 🔒 My Identity
- Archetype: worker
- Roles: implementer, qa, specialist
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m2_1\
- Original parent: orchestrator_7 (b32c5a58-8b71-46dd-ba9a-5c9e4b6709de)
- Milestone: M2 — HPRobot McpBridge & 3-Tier Safety System

## 🔒 Key Constraints
- Pure standalone desktop app (`HPRobot.McpBridge`, `net8.0-windows`, WPF).
- Robot Structural Analysis Professional 2026 COM API: `Interop.RobotOM.dll` (`IRobotApplication`, `IRobotStructure`, `IRobotUnitMngr`).
- No NuGet package for Robot API; reference installed DLL or fallback from `C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll`.
- Do not reference other MCP bridge projects directly — only reference `McpShared/HPRebar.McpBridge.Core`.
- 3-tier safety: Read, Write, Delete/Heavy with AST semantic analyzer, safety guard (AllowExecution, AllowHeavyOperations), and automated pre-run snapshots.
- UI: MaterialDesignThemes 5.3.2, dark/light theme matching, live logs, connection state, safety checkboxes.
- DO NOT CHEAT: Genuine implementation, real state, real behavior.

## Current Parent
- Conversation ID: b32c5a58-8b71-46dd-ba9a-5c9e4b6709de
- Updated: 2026-09-21T14:04:00Z

## Task Summary
- **What to build**: `HPRobot/HPRobot.slnx`, `HPRobot/global.json`, `HPRobot/Directory.Build.props`, and full `HPRobot/HPRobot.McpBridge/` project.
- **Success criteria**: Zero build warnings, zero errors on `dotnet build`. Complete 3-tier safety, COM lifecycle, Units policy, Snapshot manager, MVVM UI, pipe listener.
- **Interface contracts**: Follow `McpShared` contracts and sibling bridges (`HPSap2000`, `HPEtabs`, `HPExcel`).
- **Code layout**: Standalone project under `HPRobot/HPRobot.McpBridge/`.

## Key Decisions Made
- Used `RobotStaWorker` to run all COM calls on a dedicated STA background thread, preventing RPC_E_WRONG_THREAD and cross-apartment deadlocks.
- Implemented `ComInteropHelper` with native `IOleMessageFilter` to automatically handle `SERVERCALL_RETRYLATER` and modal Robot dialogs up to 30 seconds.
- Implemented `RobotUnitsPolicy` to enforce Metric standard (`m`, `kN`, `kN·m`, `MPa`) during script execution with state restoration in `finally`.
- Implemented `RobotTierTable` and `RobotTierAnalyzer` (Roslyn AST semantic classifier) to gate Tier W and Tier D actions.
- Implemented `RobotSnapshotManager` with automated retention pruning to keep 20 newest `.rtd` backups.

## Artifact Index
- `changes.md` — Complete implementation record of all 32 files created.
- `handoff.md` — 5-component handoff report for Project Orchestrator.

## Change Tracker
- **Files modified**: 32 files created across `HPRobot/` and `HPRobot/HPRobot.McpBridge/`.
- **Build status**: `dotnet build HPRobot/HPRobot.slnx -c Debug` -> 0 warnings, 0 errors; `Release` -> 0 warnings, 0 errors.
- **Pending issues**: None.

## Quality Status
- **Build/test result**: Pass (HPRobot.slnx builds cleanly; McpShared tests 456/456 passing).
- **Lint status**: 0 violations.
- **Tests added/modified**: Milestone M2 focuses on bridge and safety system; test projects are in Milestone M4.

## Loaded Skills
- None
