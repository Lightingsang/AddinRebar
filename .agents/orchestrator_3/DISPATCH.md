# Dispatch History

## 2026-09-20T12:39:24Z

Task: Project Orchestrator for migrating HPGeo geodetic toolkit into HPAutoCad as HPGeoLink and establishing closed-loop live verification cycle via MCP AutoCAD.
Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\
Parent Sentinel: 9421283c-b0a3-4634-b964-65d1982b6673
Authoritative User Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (Refer to section '## Follow-up — 2026-09-20T12:39:24Z')

Requirements:
- R1. Restructure HPAutoCad & Migrate HPGeo to HPGeoLink (HPAutoCad, HPAutoCad.Core, HPAutoCad.TileFetch, HPAutoCad.Tests; remove legacy standalone HPGeo/ after verification)
- R2. Single Bundle Packaging, ALC Loader & Shared Ribbon Tab (HPAutoCad.bundle deployed to %AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\, unified HPAutoCad.Loader with isolated ALC, shared Ribbon tab HPAutoCad with MCP and HPGeoLink panels, repack MaterialDesignThemes & WebView2Loader.dll, backward compatibility with HPAutoCad.McpBridge & mirror tests)
- R3. Mandatory Closed-Loop Verification via MCP AutoCAD (autonomous build-test loop in AutoCAD 2026 via MCP, unattended execution via harness scripts)
- R4. Standardized Rules & Repository Documentation (AGENTS.md, docs/code-standards.md, docs/system-architecture.md, docs/codebase-summary.md)

Acceptance Criteria:
- HPAutoCad.slnx builds cleanly in Debug and Release without warnings treated as errors.
- All 161 geodetic unit tests in HPAutoCad.Tests pass 100%.
- All MCP server and bridge tests in HPAutoCad.Mcp.Server.Tests pass 100%.
- Civil 3D mirror tests (HPCivil3d.McpBridge.Tests) pass 100%.
- Live AutoCAD 2026 verification via MCP (bundle loads, Ribbon tab appears, named pipe accepts AI execution, commands HPGEODIALOG/HPGEOKMZ/HPGEOIMPORT/HPGEOINFO execute, dialogs render dark/light and close cleanly, AEC tools functional).
- Repository cleanliness (standalone HPGeo/ removed, documentation updated).
