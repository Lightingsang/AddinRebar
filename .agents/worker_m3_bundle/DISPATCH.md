# Dispatch for worker_m3_bundle

## 2026-09-20T13:53:00Z
Task: Implement Milestone M3: Single Bundle Packaging, ALC Loader (`HPAutoCad.Loader`), and Shared Ribbon Tab (`HPAUTOCAD_MCP_TAB`).
Parent Orchestrator: orchestrator_3 (050984c1-afaa-4911-859c-331e9279dc4f)
Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md
Project Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\PROJECT.md
Loader Plan: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m3_loader_plan.md
Ribbon Plan: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m3_ribbon_plan.md
Bundle Plan: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m3_bundle_plan.md
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m3_bundle\
Handoff Target: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m3_bundle\handoff.md

Mandatory Integrity Warning:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A forensic auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Scope Boundaries & File Ownership:
- You exclusively own `HPAutoCad/HPAutoCad.Loader/` and `HPAutoCad/HPAutoCad.slnx`.
- DO NOT modify any files in `HPAutoCad/HPAutoCad.McpBridge.Loader/` or `HPCivil3d/` (preserve mirror test integrity).
- DO NOT modify `McpShared/`.
- Ensure `HPCivil3d.McpBridge.Tests` (60 tests) pass 100%.

Implementation Checklist:
1. Create `HPAutoCad/HPAutoCad.Loader/HPAutoCad.Loader.csproj`:
   - `net8.0-windows`, `UseWPF=true`, `AutoCAD.NET [25.1.0]` (compile-time only: `PrivateAssets="all"` `ExcludeAssets="runtime"`).
   - ProjectReference to `HPAutoCad.csproj` with `ReferenceOutputAssembly="false"` and `Private="false"`.
   - ProjectReference to `HPAutoCad.McpBridge.csproj` with `ReferenceOutputAssembly="false"` and `Private="false"`.
   - `DeployBundle` target deploying to `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\`.
2. Create `HPAutoCad/HPAutoCad.Loader/Bundle/PackageContents.xml`:
   - Unified manifest with dual component entries (`HPAutoCad.McpBridge` and `HPAutoCad`).
3. Create `HPAutoCad/HPAutoCad.Loader/AppLoadContext.cs`:
   - Dedicated ALC `"HPAutoCad.App"` with `AssemblyDependencyResolver` and unmanaged DLL resolution for `WebView2Loader.dll`.
4. Create `HPAutoCad/HPAutoCad.Loader/HPGeoCommands.cs`:
   - `[assembly: CommandClass(typeof(HPAutoCad.Loader.HPGeoCommands))]`.
   - Commands: `HPGEO`, `HPGEODIALOG`, `-HPGEOKMZ`, `HPGEOKMZ`, `HPGEOIMPORT`, `-HPGEOIMPORT`, `-HPGEOIMAGE`, `HPGEOINFO`.
5. Create `HPAutoCad/HPAutoCad.Loader/HPAutoCadLoaderApplication.cs`:
   - `IExtensionApplication` (`Initialize`, `Terminate`).
6. Create `HPAutoCad/HPAutoCad.Loader/Ribbon/HPGeoLinkRibbonTab.cs`, `RibbonCommandHandler.cs`, `RibbonIcons.cs`:
   - Shared tab `HPAUTOCAD_MCP_TAB` with `HPGEOLINK_PANEL`, `KMZ` large button, `Import` split button, vector icons, `WSCURRENT` and `COLORTHEME` handlers.
7. Create `HPAutoCad/HPAutoCad.Loader/LoaderLog.cs`:
   - Dual-write logger to `%LocalAppData%\HPAutoCad\logs\loader.log` and `%LocalAppData%\HPGeo\logs\loader.log`.
8. Register `HPAutoCad.Loader.csproj` in `HPAutoCad/HPAutoCad.slnx`.
9. Build solution in Debug and Release.
10. Verify bundle deployment and run all test suites (`HPAutoCad.Tests`, `Mcp.Server.Tests`, `Aec.Tests`, `HPCivil3d.McpBridge.Tests`).
