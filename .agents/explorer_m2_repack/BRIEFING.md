# BRIEFING — 2026-09-20T13:25:00Z

## Mission
Formulate the exact implementation specification for HPAutoCad.csproj MSBuild targets and solution integration (Milestone M2).

## 🔒 My Identity
- Archetype: explorer
- Roles: Teamwork explorer
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m2_repack
- Original parent: 050984c1-afaa-4911-859c-331e9279dc4f
- Milestone: M2 (Project Packaging & Solution Integration)

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Do NOT modify source code or project files
- Must verify Civil 3D mirror test remains 100% untouched and passing
- Deliver technical plan to .agents/orchestrator_3/m2_repack_plan.md
- Maintain progress.md and handoff.md in working directory
- Send message back to parent via send_message

## Current Parent
- Conversation ID: 050984c1-afaa-4911-859c-331e9279dc4f
- Updated: 2026-09-20T13:15:00Z

## Investigation State
- **Explored paths**:
  - `HPGeo/HPGeo.AutoCad/HPGeo.AutoCad.csproj`, `HPGeo/HPGeo.AutoCad.Loader/GeoLoadContext.cs`, `HPGeo/HPGeo.Tests/HPGeo.Tests.csproj`
  - `HPAutoCad/HPAutoCad.McpBridge/HPAutoCad.McpBridge.csproj`
  - `HPAutoCad/HPAutoCad.Core/HPAutoCad.Core.csproj`, `HPAutoCad/HPAutoCad.TileFetch/HPAutoCad.TileFetch.csproj`, `HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj`
  - `HPAutoCad/HPAutoCad.slnx`
  - `HPCivil3d/HPCivil3d.McpBridge.Tests/MirrorTests.cs`, `HPCivil3d/tools/mirror-tokens.json`
- **Key findings**:
  - `HPAutoCad.csproj` must use `net8.0-windows`, `EnableDynamicLoading=true`, `UseWPF=true`, `AutoCAD.NET 25.1.0` (exclude runtime), `CommunityToolkit.Mvvm 8.4.0`, `MaterialDesignThemes 5.3.2`, `Microsoft.Web.WebView2 1.0.4191.47`, `ILRepack 2.0.46` (with `GeneratePathProperty=true`), and project reference `HPAutoCad.Core`.
  - `RepackMaterialDesign` merges `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, `Microsoft.Xaml.Behaviors.dll` into `HPAutoCad.dll` with intermediate assembly as primary input; requires `[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]`.
  - Native `WebView2Loader.dll` is bundled into `runtimes\win-x64\native\` and resolved via `HPAutoCad.deps.json` and `AssemblyDependencyResolver`.
  - `CopyTileFetchHelper` target copies `HPAutoCad.TileFetch.exe` outputs into `$(OutDir)TileFetch\`.
  - `HPAutoCad.Tests` currently has 10 duplicate support classes in `HPGeoLink/Support/` that must be retired when referencing `HPAutoCad.csproj`. All 161 tests verified passing.
  - `HPAutoCad.slnx` requires adding `<Project Path="HPAutoCad/HPAutoCad.csproj" />`.
  - Civil 3D mirror tests (60/60 passing) only scan `HPAutoCad.McpBridge`, `HPAutoCad.McpBridge.Loader`, and `HPAutoCad.Mcp.Server`. Creating `HPAutoCad.csproj` and updating `HPAutoCad.Tests` and `HPAutoCad.slnx` will not touch mirror-tracked files.
- **Unexplored areas**: None for M2 repack scope; plan fully formulated.

## Key Decisions Made
- Comprehensive technical plan written to `.agents/orchestrator_3/m2_repack_plan.md`.
- All 7 requirements rigorously specified with ready-to-use XML and C# code snippets.

## Artifact Index
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m2_repack_plan.md — Technical plan for M2 repack and packaging
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m2_repack\handoff.md — Handoff report
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m2_repack\progress.md — Liveness progress
