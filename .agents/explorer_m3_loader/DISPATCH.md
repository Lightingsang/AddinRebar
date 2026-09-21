# Dispatch for explorer_m3_loader

## 2026-09-20T13:47:00Z
Task: Plan the `HPAutoCad.Loader` project for isolated ALC loading of `HPAutoCad.dll` and command forwarding.
Parent Orchestrator: orchestrator_3 (050984c1-afaa-4911-859c-331e9279dc4f)
Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md
Project Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\PROJECT.md
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m3_loader\
Output: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m3_loader_plan.md

Exploration Objectives:
1. Examine `HPGeo/HPGeo.AutoCad.Loader/` (`GeoLoadContext.cs`, `HPGeoCommands.cs`, `HPGeoLoaderApplication.cs`, `LoaderLog.cs`, `.csproj`).
2. Design `HPAutoCad/HPAutoCad.Loader/HPAutoCad.Loader.csproj` targeting `net8.0-windows` with `AutoCAD.NET 25.1.0`.
3. Design `AppLoadContext` (or `GeoLoadContext`): how it resolves `Contents\App\HPAutoCad.dll`, dependencies via `AssemblyDependencyResolver`, native libraries (`WebView2Loader.dll`), and isolates from AutoCAD Default ALC and `BridgeLoadContext`.
4. Design `HPGeoCommands`: register AutoCAD commands (`HPGEO`, `-HPGEOKMZ`, `HPGEOIMPORT`, `-HPGEOIMPORT`, `-HPGEOIMAGE`, `HPGEOINFO`) in Default ALC with `[CommandMethod]`, and forward to delegates in `HPAutoCad.dll`.
5. Verify that creating `HPAutoCad.Loader` does NOT affect `HPCivil3d.McpBridge.Tests` (mirror tests).
