# Task Assignment: Survey Explorer 2 — Standalone Bridge Reference & Architecture

## Identity
- Role: Explorer
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_2
- Parent Orchestrator: orchestrator_6

## Objective
Investigate reference standalone bridge architectures in the repo, especially `HPPowerBi/` and `HPEtabs/` (and `HPSap2000/`), to determine the project layout, solution format, WPF MVVM theme setup, 3-tier safety engine, snapshot engine, and pipe listener for `HPExcel.McpBridge`.

## Context & Inputs
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (§ 2026-09-21T09:44:29Z)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\AGENTS.md
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPPowerBi\
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPEtabs\

## Scope of Investigation
1. Examine `HPPowerBi/` and `HPEtabs/` bridge structures:
   - Solution files (`.slnx`, `Directory.Build.props`, `global.json`).
   - Project configurations (`net8.0-windows`, WPF, NuGet packages: MaterialDesignThemes 5.3.2, CommunityToolkit.Mvvm).
   - How `MaterialThemeBridge.cs`, `ThemeLight.xaml`, `ThemeDark.xaml`, `MaterialBridge.xaml` are structured for standalone apps.
   - Bridge window and ViewModel design (status, port/instance detection, connection toggles, logs).
2. Examine 3-Tier Safety Engine (Tier R, W, D):
   - How `HPEtabs` and `HPPowerBi` categorize commands/scripts into safety tiers.
   - How safety toggles in the UI gate write and destructive operations.
3. Examine Snapshot Engine:
   - How file snapshots are captured before mutation (timestamping, location `.hpexcel_snapshots/` vs `%TEMP%`, metadata, error handling).
4. Examine Named Pipe listener and dispatcher implementation:
   - Named pipe creation, cancellation, request handling, response formatting.

## Output
Write detailed report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_2\handoff.md`.
Report when finished via `send_message`.

## 2026-09-21T09:46:59Z
You are Survey Explorer 2 for the HPExcel MCP Ecosystem.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_2

MANDATORY: Read the full user request first:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (§ 2026-09-21T09:44:29Z).
Also read your task assignment at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_2\DISPATCH.md

Your task is to investigate standalone WPF MCP bridge architectures in the repo, particularly HPPowerBi and HPEtabs:
1. Solution layout: HPExcel.slnx, Directory.Build.props, global.json, TFM (net8.0-windows, net10.0).
2. UI & Theme: MaterialDesignThemes 5.3.2, MaterialThemeBridge.cs, MaterialBridge.xaml, ThemeLight.xaml, ThemeDark.xaml, WindowsHostTheme.
3. Bridge application: App.xaml, MainWindow.xaml, MainWindowViewModel.cs, connection lifecycle, process detection, pipe listener on 'hpexcel-mcp-2026'.
4. 3-Tier Safety Engine (Tier R / Tier W / Tier D) and UI toggles.
5. Snapshot Backup Engine: capturing automatic timestamped .xlsx backup in .hpexcel_snapshots/ (or %TEMP%) before Tier W/D operations, returning snapshot path in ExecuteResult.Snapshot.

Write your complete findings, code templates, and project structure recommendations to:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_2\handoff.md
Send a completion message back when done.
