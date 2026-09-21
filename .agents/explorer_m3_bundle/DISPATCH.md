# Dispatch for explorer_m3_bundle

## 2026-09-20T13:47:00Z
Task: Plan Single Bundle Packaging (`HPAutoCad.bundle`), `PackageContents.xml`, and MSBuild Deploy Targets.
Parent Orchestrator: orchestrator_3 (050984c1-afaa-4911-859c-331e9279dc4f)
Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md
Project Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\PROJECT.md
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m3_bundle\
Output: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m3_bundle_plan.md

Exploration Objectives:
1. Examine `HPAutoCad/HPAutoCad.McpBridge.Loader/Bundle/PackageContents.xml` and how `DeployBundle` targets currently deploy to `%AppData%\Autodesk\ApplicationPlugins\`.
2. Design the unified `PackageContents.xml` for `HPAutoCad.bundle`:
   - Single bundle `HPAutoCad.bundle` containing both components:
     * `HPAutoCad.McpBridge` via `./Contents/HPAutoCad.McpBridge.Loader.dll`
     * `HPAutoCad` (HPGeoLink) via `./Contents/HPAutoCad.Loader.dll`
   - Platform="AutoCAD" SeriesMin="R25.1" SeriesMax="R25.1".
3. Plan bundle directory layout:
   `HPAutoCad.bundle/`
   ├── `PackageContents.xml`
   └── `Contents/`
       ├── `HPAutoCad.McpBridge.Loader.dll`
       ├── `HPAutoCad.Loader.dll`
       ├── `Bridge/` (HPAutoCad.McpBridge + Roslyn + AEC + dependencies)
       └── `App/` (HPAutoCad + HPAutoCad.Core + WebView2Loader.dll + TileFetch/)
4. Plan the MSBuild deployment targets and solution build order so building `HPAutoCad.slnx` cleanly deploys the full bundle.
5. Verify Civil 3D mirror boundary: ensure `HPAutoCad.McpBridge.Loader/Bundle/PackageContents.xml` sha256 pin in `HPCivil3d/tools/mirror-tokens.json` remains untouched.
