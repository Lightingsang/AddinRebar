# Dispatch for reviewer_m3_loader

## 2026-09-20T14:01:00Z
Task: Review Milestone M3 ALC Loader Architecture (`HPAutoCad.Loader`, `AppLoadContext`, `HPGeoCommands`).
Parent Orchestrator: orchestrator_3 (050984c1-afaa-4911-859c-331e9279dc4f)
Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md
Project Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\PROJECT.md
Worker M3 Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m3_bundle\handoff.md
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m3_loader\
Verdict Target: handoff.md with APPROVE or REQUEST_CHANGES

Review Focus:
1. Inspect `HPAutoCad/HPAutoCad.Loader/HPAutoCad.Loader.csproj`: verify `<ReferenceOutputAssembly="false">` prevents JIT loading into Default ALC.
2. Inspect `AppLoadContext.cs`: verify non-collectible ALC, host assembly fallback (`Ac*`, `Ad*`, `Autodesk.*`), and unmanaged DLL resolution for `WebView2Loader.dll`.
3. Inspect `HPGeoCommands.cs`: verify `[assembly: CommandClass]` and commands `HPGEO`, `HPGEODIALOG`, `-HPGEOKMZ`, `HPGEOKMZ`, `HPGEOIMPORT`, `-HPGEOIMPORT`, `-HPGEOIMAGE`, `HPGEOINFO`.
4. Inspect `HPAutoCadLoaderApplication.cs`: verify `IExtensionApplication` lifecycle (`Initialize`, `Terminate`).
5. Verify builds and test suites pass.
