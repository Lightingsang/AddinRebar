# BRIEFING — 2026-09-20T14:00:00Z

## Mission
Implement Milestone M3: Single Bundle Packaging, ALC Loader (HPAutoCad.Loader), and Shared Ribbon Tab (HPAUTOCAD_MCP_TAB) for HPAutoCad.

## 🔒 My Identity
- Archetype: implementer
- Roles: implementer, qa, specialist
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m3_bundle\
- Original parent: 050984c1-afaa-4911-859c-331e9279dc4f
- Milestone: M3 (Single Bundle Packaging, ALC Loader & Shared Ribbon Tab)

## 🔒 Key Constraints
- Exclusively own: `HPAutoCad/HPAutoCad.Loader/` and `HPAutoCad/HPAutoCad.slnx` (plus `HPAutoCad/Directory.Build.props` if needed per bundle plan).
- DO NOT modify any files in `HPAutoCad/HPAutoCad.McpBridge.Loader/`, `HPAutoCad.McpBridge/`, `HPCivil3d/`, or `McpShared/`.
- Ensure Civil 3D mirror invariant passes 100% (`HPCivil3d.McpBridge.Tests`).
- Dedicated ALC `"HPAutoCad.App"` with host assembly passthrough for `Ac*`, `Ad*`, `Autodesk.*`, and unmanaged DLL resolution for `WebView2Loader.dll`.
- Zero direct compile-time assembly references from Loader to `HPAutoCad.dll`.
- Shared ribbon tab `HPAUTOCAD_MCP_TAB` with dual panel coexistence (`HPAUTOCAD_MCP_PANEL` and `HPGEOLINK_PANEL`).
- Dual-write logger to `%LocalAppData%\HPAutoCad\logs\loader.log` and `%LocalAppData%\HPGeo\logs\loader.log`.

## Current Parent
- Conversation ID: 050984c1-afaa-4911-859c-331e9279dc4f
- Updated: 2026-09-20T14:00:00Z

## Task Summary
- **What to build**: Single bundle deployment, `HPAutoCad.Loader` project, ALC isolation, command forwarding, vector ribbon tab, and dual logging.
- **Success criteria**: Clean builds (Debug/Release), bundle deployed to `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\`, all test suites passing (`HPAutoCad.Tests`, `Mcp.Server.Tests`, `Aec.Tests`, `HPCivil3d.McpBridge.Tests`).
- **Interface contracts**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\PROJECT.md`
- **Code layout**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\PROJECT.md § Code Layout`

## Key Decisions Made
- Project name: `HPAutoCad.Loader` targeting `net8.0-windows` with `AutoCAD.NET [25.1.0]`.
- ALC name: `"HPAutoCad.App"` (isCollectible: false).
- Ribbon Tab: `HPAUTOCAD_MCP_TAB`, panel `HPGEOLINK_PANEL`.
- `Directory.Build.props` added to suppress legacy `HPAutoCad.McpBridge.bundle` deployment in Debug builds.
- Unified bundle deployed to `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\`.

## Artifact Index
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m3_bundle\progress.md` — Liveness and progress tracker
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m3_bundle\handoff.md` — 5-component handoff report
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Loader\` — Complete loader project and ribbon integration

## Change Tracker
- **Files created/modified**:
  - `HPAutoCad/HPAutoCad.Loader/HPAutoCad.Loader.csproj`: Loader project with `DeployBundle` target.
  - `HPAutoCad/HPAutoCad.Loader/Bundle/PackageContents.xml`: Dual-component autoloader manifest.
  - `HPAutoCad/HPAutoCad.Loader/AppLoadContext.cs`: Dedicated ALC with host assembly passthrough & unmanaged DLL resolver.
  - `HPAutoCad/HPAutoCad.Loader/LoaderLog.cs`: BCL-only dual-write diagnostic logger.
  - `HPAutoCad/HPAutoCad.Loader/HPGeoCommands.cs`: AutoCAD commands forwarding to ALC delegates.
  - `HPAutoCad/HPAutoCad.Loader/HPAutoCadLoaderApplication.cs`: Extension application entry point.
  - `HPAutoCad/HPAutoCad.Loader/Ribbon/HPGeoLinkRibbonTab.cs`: Shared tab and HPGeoLink panel manager.
  - `HPAutoCad/HPAutoCad.Loader/Ribbon/RibbonCommandHandler.cs`: Safe ICommand relay.
  - `HPAutoCad/HPAutoCad.Loader/Ribbon/RibbonIcons.cs`: Vector WPF icons with dynamic dark/light ink.
  - `HPAutoCad/Directory.Build.props`: Suppress legacy bridge bundle deployment.
  - `HPAutoCad/HPAutoCad.slnx`: Registered `HPAutoCad.Loader.csproj`.
- **Build status**: Debug (PASS), Release (PASS).
- **Pending issues**: None.

## Quality Status
- **Build/test result**: All suites PASS (HPAutoCad.Tests: 158 passed / 3 skipped; Mcp.Server.Tests: 280 passed; Aec.Tests: 225 passed; Civil3d MirrorTests: 60 passed).
- **Lint status**: 0 errors.
- **Tests added/modified**: Verified all existing suites.

## Loaded Skills
None
