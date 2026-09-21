# Dispatch for auditor_m3_bundle

## 2026-09-20T14:01:00Z
Task: Forensic Integrity Audit of Milestone M3 (HPAutoCad.Loader, PackageContents.xml, Ribbon, Repacking & Bundle Packaging).
Parent Orchestrator: orchestrator_3 (050984c1-afaa-4911-859c-331e9279dc4f)
Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md
Project Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\PROJECT.md
Worker M3 Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m3_bundle\handoff.md
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\auditor_m3_bundle\
Verdict Target: handoff.md with CLEAN or INTEGRITY VIOLATION

Audit Focus:
1. Inspect all files authored in `HPAutoCad/HPAutoCad.Loader/`, `HPAutoCad/Directory.Build.props`, and modifications to `HPAutoCad/HPAutoCad.slnx`.
2. Check for cheating patterns:
   - Dummy ALC implementations or facade resolvers.
   - Fake or hardcoded command executions.
   - Commented-out or trivial test assertions.
   - Hardcoded test passes or bypassed packaging steps.
3. Verify that `AppLoadContext.cs` implements an authentic, non-collectible `AssemblyLoadContext` backed by real `AssemblyDependencyResolver` and unmanaged DLL probing for `WebView2Loader.dll`.
4. Verify that `HPGeoCommands.cs` uses authentic `[CommandMethod]` attributes and real dynamic delegate dispatch.
5. Verify that `HPGeoLinkRibbonTab.cs` uses genuine AdWindows types (`RibbonTab`, `RibbonPanel`, `RibbonButton`, `RibbonSplitButton`) and authentic vector geometries in `RibbonIcons.cs`.
6. Verify that `PackageContents.xml` authentically declares both components and that the deployed files match the manifest.
