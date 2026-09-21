# Dispatch for explorer_m3_ribbon

## 2026-09-20T13:47:00Z
Task: Plan the Shared Ribbon Tab `HPAUTOCAD_MCP_TAB` integration and `HPGEOLINK_PANEL` for Milestone M3.
Parent Orchestrator: orchestrator_3 (050984c1-afaa-4911-859c-331e9279dc4f)
Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md
Project Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\PROJECT.md
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m3_ribbon\
Output: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m3_ribbon_plan.md

Exploration Objectives:
1. Examine `HPAutoCad/HPAutoCad.McpBridge.Loader/Ribbon/McpRibbonTab.cs` and `HPGeo/HPGeo.AutoCad.Loader/Ribbon/HPGeoRibbonTab.cs`.
2. Design the Ribbon panel for HPGeoLink: `HPGEOLINK_PANEL` on shared tab `HPAUTOCAD_MCP_TAB` ("HPAutoCad").
3. Design panel layout:
   - Primary large button: `HPGEO` (Export KMZ)
   - Secondary / SplitButton: `-HPGEOKMZ`, `HPGEOIMPORT`, `-HPGEOIMPORT`, `HPGEOIMAGE`, `HPGEOINFO`.
4. Design lifecycle handlers:
   - `WSCURRENT` (Workspace switch): ensure panels are recreated if Ribbon is reloaded.
   - `COLORTHEME`: ensure vector icons dynamically adapt ink color (dark vs light).
   - `ComponentManager.ItemInitialized`: handle late ribbon initialization.
5. Detail vector icon geometry in `RibbonIcons.cs` for all buttons.
