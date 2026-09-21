# Dispatch for reviewer_m3_ribbon

## 2026-09-20T14:01:00Z
Task: Review Milestone M3 Shared Ribbon Tab Integration (`HPGeoLinkRibbonTab`, `RibbonIcons`, `RibbonCommandHandler`).
Parent Orchestrator: orchestrator_3 (050984c1-afaa-4911-859c-331e9279dc4f)
Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md
Project Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\PROJECT.md
Worker M3 Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m3_bundle\handoff.md
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m3_ribbon\
Verdict Target: handoff.md with APPROVE or REQUEST_CHANGES

Review Focus:
1. Inspect `HPGeoLinkRibbonTab.cs`: verify cooperative shared tab protocol on `HPAUTOCAD_MCP_TAB` ("HPAutoCad") and panel `HPGEOLINK_PANEL` ("HPGeoLink").
2. Verify panel controls: KMZ large button, Import SplitButton with dropdown items (`-HPGEOIMAGE`, `HPGEOINFO`, `-HPGEOKMZ`, `-HPGEOIMPORT`).
3. Verify event listeners: `WSCURRENT` (workspace switch) and `COLORTHEME` (theme switch) with deferred `Application.Idle` ticks and `removeEmptyTab: false` on panel rebuild.
4. Inspect `RibbonIcons.cs`: verify pure WPF vector geometries (`DrawingImage`) with dynamic dark/light theme ink.
5. Inspect `RibbonCommandHandler.cs`: verify exception safety during AutoCAD command execution.
