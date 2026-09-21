# Dispatch for challenger_m3_bundle

## 2026-09-20T14:01:00Z
Task: Empirical Testing & Filesystem Verification of `HPAutoCad.bundle` Packaging and Unit Test Suites.
Parent Orchestrator: orchestrator_3 (050984c1-afaa-4911-859c-331e9279dc4f)
Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md
Project Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\PROJECT.md
Worker M3 Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m3_bundle\handoff.md
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_m3_bundle\
Verdict Target: handoff.md with APPROVE or REQUEST_CHANGES

Verification Focus:
1. Run Debug and Release compilation:
   `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug`
   `dotnet build HPAutoCad/HPAutoCad.slnx -c Release`
2. Empirically inspect deployed bundle at `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\`:
   - `PackageContents.xml` exists and contains dual component entries (`HPAutoCad.McpBridge` and `HPAutoCad`).
   - `Contents/HPAutoCad.Loader.dll` and `Contents/HPAutoCad.McpBridge.Loader.dll` exist.
   - `Contents/App/` contains `HPAutoCad.dll`, `HPAutoCad.Core.dll`, `runtimes/win-x64/native/WebView2Loader.dll`, and `TileFetch/HPAutoCad.TileFetch.exe`.
   - `Contents/Bridge/` contains `HPAutoCad.McpBridge.dll`, `HPAutoCad.Aec.dll`, Roslyn DLLs, Serilog.
3. Verify that old standalone bundles (`HPAutoCad.McpBridge.bundle` and `HPGeo.bundle`) are removed or not deployed.
4. Run `HPAutoCad.Tests` (161 tests: 158 passed, 3 skipped) and `HPAutoCad.Mcp.Server.Tests` (280 passed).
