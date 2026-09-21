# BRIEFING — 2026-09-20T13:52:00Z

## Mission
Formulate exact packaging and deployment specification for `HPAutoCad.bundle` (unified bundle for HPAutoCad.McpBridge + HPAutoCad/HPGeoLink), including PackageContents.xml, directory layout, MSBuild deployment targets, and Civil 3D mirror boundary preservation.

## 🔒 My Identity
- Archetype: explorer
- Roles: Teamwork explorer (read-only investigation, packaging & deployment design)
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m3_bundle\
- Original parent: 050984c1-afaa-4911-859c-331e9279dc4f (orchestrator_3)
- Milestone: M3 (Single Bundle Packaging & Deployment)

## 🔒 Key Constraints
- Read-only investigation — do NOT modify product source files directly in this role.
- Output plan must be written to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m3_bundle_plan.md`.
- Preserve Civil 3D mirror boundary: ensure `HPAutoCad.McpBridge.Loader/Bundle/PackageContents.xml` sha256 pin in `HPCivil3d/tools/mirror-tokens.json` remains untouched.
- Single bundle `HPAutoCad.bundle` deployed to `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\`.
- Maintain self-contained handoff.md and progress updates.

## Current Parent
- Conversation ID: 050984c1-afaa-4911-859c-331e9279dc4f
- Updated: 2026-09-20T13:52:00Z

## Investigation State
- **Explored paths**: `HPAutoCad/HPAutoCad.McpBridge.Loader/Bundle/PackageContents.xml`, `HPGeo/HPGeo.AutoCad.Loader/Bundle/PackageContents.xml`, `HPCivil3d/tools/mirror-tokens.json`, `HPCivil3d.McpBridge.Tests/MirrorTests.cs`, `HPAutoCad.Loader.csproj` design, `HPAutoCad.slnx`, `HPAutoCad/HPAutoCad/Entry.cs`, `HPAutoCad/tools/harness/run-bridge-unattended.ps1`, `%AppData%\Autodesk\ApplicationPlugins\`.
- **Key findings**:
  1. PackageContents.xml in unified bundle declares both `HPAutoCad.McpBridge` (`./Contents/HPAutoCad.McpBridge.Loader.dll`) and `HPAutoCad` (`./Contents/HPAutoCad.Loader.dll`) with `Platform="AutoCAD"` and `SeriesMin="R25.1" SeriesMax="R25.1"`.
  2. The SHA-256 of `HPAutoCad.McpBridge.Loader/Bundle/PackageContents.xml` (`22d566c8faec996572452d3fab99fdceaa0d89732f6f42b28df86f3602c1ef28`) is 100% preserved because that file and `HPAutoCad.McpBridge.Loader.csproj` remain completely unmodified.
  3. `HPAutoCad.Loader.csproj` owns the unified `DeployBundle` target. To prevent `HPAutoCad.McpBridge.Loader` from deploying the legacy standalone bundle, `HPAutoCad/Directory.Build.props` sets `<DeployBundle>false</DeployBundle>` for `HPAutoCad.McpBridge.Loader`.
  4. Both loaders cooperatively register the shared ribbon tab `HPAUTOCAD_MCP_TAB` ("HPAutoCad") with panels `HPAUTOCAD_MCP_PANEL` ("MCP") and `HPGEOLINK_PANEL` ("HPGeoLink") with zero order dependency.
  5. Tested and verified: `HPCivil3d.McpBridge.Tests` (60 passed), `HPAutoCad.Tests` (161 passed), `HPAutoCad.Mcp.Server.Tests` (280 passed).
- **Unexplored areas**: None for M3 bundle packaging specification.

## Key Decisions Made
- Unified manifest placed at `HPAutoCad/HPAutoCad.Loader/Bundle/PackageContents.xml`.
- `DeployBundle` implemented in `HPAutoCad.Loader.csproj` with pre-flight checks, lock probe, legacy bundle purging, and atomic payload copying.
- Full comprehensive plan authored and published to `.agents/orchestrator_3/m3_bundle_plan.md`.

## Artifact Index
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m3_bundle_plan.md` — Final M3 bundle packaging and deployment plan.
