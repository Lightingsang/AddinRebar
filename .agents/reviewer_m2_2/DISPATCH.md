# Task Assignment: Reviewer 2 — Milestone M2 Verification

## Identity
- Role: Reviewer
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m2_2
- Parent Orchestrator: orchestrator_6

## Objective
Independently review Milestone M2 deliverables for UI/UX, theming, safety cascading logic, snapshot engine reliability, and threading robustness.

## Mandatory Reading & References
- Mandatory Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (§ 2026-09-21T09:44:29Z)
- Project Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_6\PROJECT.md
- Worker M2 Handoff Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m2_1\handoff.md

## Scope of Review
1. Inspect UI theming and MaterialDesign integration:
   - `MaterialThemeBridge.cs`, `WindowsHostTheme.cs`, `ThemeLight.xaml`, `ThemeDark.xaml`, `MaterialBridge.xaml`, `ThemeInfo.cs`
2. Inspect Safety & Snapshot Engine:
   - Cascading rules in `ExcelSafetyGuard.cs`
   - `ExcelTierAnalyzer.cs` syntax walker
   - `ExcelSnapshotManager.cs` retention pruning and path resolution
3. Run builds:
   - `dotnet build HPExcel/HPExcel.McpBridge/HPExcel.McpBridge.csproj -c Debug`
   - `dotnet build HPExcel/HPExcel.McpBridge/HPExcel.McpBridge.csproj -c Release`
4. Issue verdict: `APPROVE` or `REQUEST_CHANGES`.

## Output
Write review report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m2_2\handoff.md`.
Notify parent when done via `send_message`.

## 2026-09-21T10:19:14Z
You are Reviewer 2 (reviewer_m2_2) for Milestone M2 (HPExcel Solution Setup & Bridge Engine).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m2_2

MANDATORY: Read the full user request:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (§ 2026-09-21T09:44:29Z).
Also read your assignment at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m2_2\DISPATCH.md
And read Worker M2's handoff report at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m2_1\handoff.md

Independently review Milestone M2 for UI/UX, theming, safety cascading, and snapshot retention. Build HPExcel.McpBridge in Debug and Release.
Write your review report to:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m2_2\handoff.md


## 2026-09-21T14:04:25Z
You are reviewer_m2_2 (M2 UI and Architecture Reviewer) for the HPRobot MCP Subsystem.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m2_2\
Your parent is the Project Orchestrator (orchestrator_7, conversation ID: b32c5a58-8b71-46dd-ba9a-5c9e4b6709de).

MANDATORY FIRST STEP:
Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md and g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_7\PROJECT.md.
Also read worker_m2_1's changes and handoff:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m2_1\changes.md`
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m2_1\handoff.md`

YOUR MISSION:
Review the UI and architectural integrity of `HPRobot.McpBridge`:
1. Verify MaterialDesignThemes 5.3.2 adoption: `MaterialThemeBridge.cs`, `WindowsHostTheme.cs`, `MaterialBridge.xaml`, dynamic resource usage, and theme switching.
2. Verify architectural isolation: confirm that `HPRobot/` references `../McpShared/` only and has zero references to sibling host projects.
3. Verify STA threading model and IOleMessageFilter message pump handling.
4. Run the build independently:
   `dotnet build HPRobot/HPRobot.slnx -c Debug`
5. Document your verdict (APPROVE or REQUEST_CHANGES) with concrete evidence.

DELIVERABLES:
Write your review report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m2_2\handoff.md`
When finished, send a message to your parent with your verdict and report path.
