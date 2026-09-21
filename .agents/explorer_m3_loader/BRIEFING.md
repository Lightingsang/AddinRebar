# BRIEFING — 2026-09-20T13:51:00Z

## Mission
Formulate the exact architectural and implementation plan for `HPAutoCad/HPAutoCad.Loader/` (Milestone M3), enabling isolated ALC loading of `HPAutoCad.dll`, command forwarding, and Civil 3D mirror safety.

## 🔒 My Identity
- Archetype: explorer
- Roles: exploration agent, technical investigation, architectural specification
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m3_loader
- Original parent: 050984c1-afaa-4911-859c-331e9279dc4f
- Milestone: M3 (Single Bundle, Loader & Shared Ribbon)

## 🔒 Key Constraints
- Read-only investigation — do NOT implement source code
- Target file output: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m3_loader_plan.md
- Maintain progress.md in working directory
- Write handoff.md in working directory
- Zero impact on HPCivil3d.McpBridge.Tests (mirror tests)
- Safe ALC isolation preventing type collisions with AutoCAD Default ALC and BridgeLoadContext

## Current Parent
- Conversation ID: 050984c1-afaa-4911-859c-331e9279dc4f
- Updated: 2026-09-20T13:51:00Z

## Investigation State
- **Explored paths**:
  - `HPGeo/HPGeo.AutoCad.Loader/` (`GeoLoadContext.cs`, `HPGeoCommands.cs`, `HPGeoLoaderApplication.cs`, `LoaderLog.cs`, `HPGeo.AutoCad.Loader.csproj`, `Bundle/PackageContents.xml`)
  - `HPAutoCad/HPAutoCad.McpBridge.Loader/` (`BridgeLoadContext.cs`, `HPAutoCad.McpBridge.Loader.csproj`, `Ribbon/McpRibbonTab.cs`, `LoaderLog.cs`, `Bundle/PackageContents.xml`)
  - `HPAutoCad/HPAutoCad/` (`Entry.cs`, `HPAutoCad.csproj`, `HPGeoLink/Commands/`)
  - `HPCivil3d/HPCivil3d.McpBridge.Tests/` (`MirrorTests.cs`, `MirrorTokenTable.cs`, `tools/mirror-tokens.json`)
  - `HPAutoCad/HPAutoCad.slnx`
- **Key findings**:
  - `AppLoadContext` must inherit from `AssemblyLoadContext("HPAutoCad.App", isCollectible: false)`, filter `["Ac", "Ad", "Autodesk."]`, and resolve `WebView2Loader.dll` from deps.json and `runtimes\win-x64\native\`.
  - `HPAutoCad.Loader.csproj` must reference `HPAutoCad.csproj` with `ReferenceOutputAssembly="false"` and `Private="false"` to prevent JIT pollution of Default ALC.
  - Commands `HPGEO`, `HPGEODIALOG`, `-HPGEOKMZ`, `HPGEOKMZ`, `HPGEOIMPORT`, `-HPGEOIMPORT`, `-HPGEOIMAGE`, `HPGEOINFO` forward via reflection delegates (`Action.DynamicInvoke()`).
  - `HPCivil3d.McpBridge.Tests` strictly inspects `HPAutoCad.McpBridge`, `HPAutoCad.McpBridge.Loader`, `HPAutoCad.Mcp.Server`. Creating `HPAutoCad.Loader` has zero impact on mirror tests.
  - Logging utilizes a dual-write pattern to `%LocalAppData%\HPAutoCad\logs\loader.log` and `%LocalAppData%\HPGeo\logs\loader.log`.
- **Unexplored areas**: None for M3 loader scope.

## Key Decisions Made
- Designed `AppLoadContext` with unmanaged DLL probing fallback for `WebView2Loader.dll`.
- Included command aliases `HPGEODIALOG` and `HPGEOKMZ` to support all UI and script test harnesses.
- Adopted dual-write logging strategy in `LoaderLog.cs` for full backward and forward compatibility.
- Fully proved Civil 3D mirror invariance.

## Artifact Index
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m3_loader_plan.md` — Complete architectural and implementation specification for HPAutoCad.Loader
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m3_loader\progress.md` — Liveness and progress tracking
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m3_loader\handoff.md` — 5-component handoff report
