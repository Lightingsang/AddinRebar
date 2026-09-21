# Handoff Report: explorer_m2_repack

**Date**: 2026-09-20T13:25:00Z  
**From**: `explorer_m2_repack`  
**To**: `orchestrator_3` (Conversation ID: `050984c1-afaa-4911-859c-331e9279dc4f`)  
**Mission**: Formulate the exact implementation specification for `HPAutoCad.csproj` MSBuild targets and solution integration (Milestone M2).  
**Primary Deliverable**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m2_repack_plan.md`  

---

## 1. Observation

1. **Existing Repack Targets**:
   - `HPGeo/HPGeo.AutoCad/HPGeo.AutoCad.csproj` (lines 60–68) and `HPAutoCad/HPAutoCad.McpBridge/HPAutoCad.McpBridge.csproj` (lines 45–53) define `RepackMaterialDesign` target using `$(PkgILRepack)\tools\ILRepack.exe /union /parallel /noRepackRes $(_RepackLib) /out:"$(OutDir)$(AssemblyName).dll" "@(IntermediateAssembly->'%(FullPath)')" "$(OutDir)MaterialDesignThemes.Wpf.dll" "$(OutDir)MaterialDesignColors.dll" "$(OutDir)Microsoft.Xaml.Behaviors.dll"`.
   - `ThemeInfo.cs` in `HPGeo/HPGeo.AutoCad/UI/ThemeInfo.cs` line 6 and `HPAutoCad/HPAutoCad.McpBridge/Resources/Themes/ThemeInfo.cs` line 6 declare `[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]`.
2. **Native WebView2 Asset Resolution**:
   - `HPGeo.AutoCad.deps.json` lines 58–62 show `runtimeTargets` pointing to `runtimes/win-x64/native/WebView2Loader.dll` for package `Microsoft.Web.WebView2/1.0.4191.47`.
   - `HPGeo/HPGeo.AutoCad.Loader/GeoLoadContext.cs` lines 37–41 demonstrate that `AssemblyDependencyResolver.ResolveUnmanagedDllToPath("WebView2Loader.dll")` resolves to `Contents\App\runtimes\win-x64\native\WebView2Loader.dll`.
3. **Out-of-Process TileFetch Helper**:
   - `HPGeo/HPGeo.AutoCad/HPGeo.AutoCad.csproj` lines 48–54 define `CopyTileFetchHelper` target copying `HPGeo.TileFetch\bin\$(Configuration)\net8.0\**\*` to `$(OutDir)TileFetch\`.
   - `HPAutoCad/HPAutoCad.TileFetch/HPAutoCad.TileFetch.csproj` lines 8–18 configure `OutputType=Exe`, `TargetFramework=net8.0`, `AssemblyName=HPAutoCad.TileFetch`.
4. **Current Test Suite & Support Hack**:
   - `HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj` runs 161 tests via command:
     `dotnet run --project HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj`
     Result: `total: 161, failed: 0, succeeded: 158, skipped: 3 (live tiles test skips without HPGEO_LIVE_TILES=1)`.
   - `HPAutoCad.Tests/HPGeoLink/Support/` currently holds 10 duplicate classes: `CrsSelectionViewModel.cs`, `GeoExportItems.cs`, `GeoExportViewModel.Commands.cs`, `GeoExportViewModel.cs`, `GeoImportViewModel.cs`, `HPGeoLog.cs`, `HelperTileFetcher.cs`, `IGeoExportShell.cs`, `ImageryPipeline.cs`, `TileStitcher.cs`.
   - Exactly 4 test files (`GeoExportViewModelTests.cs`, `GeoImportViewModelTests.cs`, `TileFetchHelperTests.cs`, `ImageryPipelineTests.cs`) reference `using HPAutoCad.Tests.HPGeoLink.Support;`.
5. **Civil 3D Mirror Gate**:
   - `HPCivil3d/HPCivil3d.McpBridge.Tests/MirrorTests.cs` lines 68 and 87 explicitly restrict mirror checks to `HPAutoCad.McpBridge`, `HPAutoCad.McpBridge.Loader`, and `HPAutoCad.Mcp.Server`.
   - Verification command: `dotnet run --project HPCivil3d/HPCivil3d.McpBridge.Tests/HPCivil3d.McpBridge.Tests.csproj`.
     Result: `total: 60, failed: 0, succeeded: 60, skipped: 0`.
6. **Solution Configuration**:
   - `HPAutoCad/HPAutoCad.slnx` currently lists `HPAutoCad.Core`, `HPAutoCad.TileFetch`, `HPAutoCad.Tests`, `HPAutoCad.McpBridge.Loader`, `HPAutoCad.Aec`, `HPAutoCad.Aec.Tests`, `HPAutoCad.McpBridge`, `HPAutoCad.Mcp.Server`, and `HPAutoCad.Mcp.Server.Tests`.
   - `HPAutoCad/HPAutoCad.csproj` has not yet been registered.

---

## 2. Logic Chain

1. **Why `IntermediateAssembly` is required as primary ILRepack input**:
   - (Observation 1) On incremental builds, if `$(OutDir)$(AssemblyName).dll` was the primary input, ILRepack would merge toolkit assemblies repeatedly into an already-merged assembly.
   - Using `"@(IntermediateAssembly->'%(FullPath)')"` (the clean obj DLL produced by the compiler) guarantees that repack is idempotent and deterministic across multiple build runs.
2. **Why `ThemeInfo.cs` is required**:
   - (Observation 1) Spike S0-A proved that without `[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]`, merged `generic.baml` is ignored by WPF's default style lookup for controls like `PackIcon` and `Card`.
3. **Why `EnableDynamicLoading` and recursive directory copying are required for WebView2**:
   - (Observation 2) `deps.json` maps `WebView2Loader.dll` to `runtimes\win-x64\native\WebView2Loader.dll`.
   - At runtime, `AssemblyDependencyResolver` resolves this path relative to the add-in DLL.
   - Preserving the folder structure in `$(OutDir)` and `Contents\App\` ensures `AppLoadContext.LoadUnmanagedDll` succeeds without crashing or polluting system directories.
4. **Why `HPAutoCad.TileFetch` uses `ReferenceOutputAssembly="false"`**:
   - (Observation 3) `TileFetch` is an out-of-process console executable, not an in-process library.
   - Setting `ReferenceOutputAssembly="false"` enforces project build ordering in MSBuild without linking compile-time binary references.
5. **How `HPAutoCad.Tests` should be refactored**:
   - (Observation 4) Referencing `HPAutoCad.csproj` in `HPAutoCad.Tests.csproj` and granting `<InternalsVisibleTo Include="HPAutoCad.Tests" />` allows the test project to directly test ViewModels, imagery services, and shell interfaces.
   - The 10 duplicate classes in `HPAutoCad.Tests/HPGeoLink/Support/` become obsolete and must be deleted.
   - Updating the 4 test files to reference `HPAutoCad.HPGeoLink.Model`, `HPAutoCad.HPGeoLink.Service`, and `HPAutoCad.HPGeoLink.ViewModel` restores full test coverage (161 tests passing).
6. **Why the Civil 3D mirror invariant is preserved**:
   - (Observation 5) The mirror test scope is strictly confined to `HPAutoCad.McpBridge`, `HPAutoCad.McpBridge.Loader`, and `HPAutoCad.Mcp.Server`.
   - Adding `HPAutoCad/HPAutoCad/HPAutoCad.csproj`, modifying `HPAutoCad.slnx`, or updating `HPAutoCad.Tests` leaves all mirrored files completely untouched, maintaining 60/60 passing tests.

---

## 3. Caveats

- `HPAutoCad.Tests` must run on a machine with WPF / Windows Desktop runtime installed (`net10.0-windows` / `net8.0-windows`), which is already satisfied by this development environment.
- Live tile downloading tests in `HPAutoCad.Tests` (3 tests) are designed to skip unless `HPGEO_LIVE_TILES=1` is set in the environment. This is expected behavior and protects offline/isolated test runs.
- No other caveats.

---

## 4. Conclusion

The implementation specification for `HPAutoCad.csproj`, its MSBuild targets (`RepackMaterialDesign`, `CopyTileFetchHelper`), native asset packaging, `HPAutoCad.Tests` refactoring, and `HPAutoCad.slnx` solution registration has been fully formulated and written to `.agents/orchestrator_3/m2_repack_plan.md`.

All 7 core requirements from the user request have been addressed with exact code snippets, proven architectural invariants, and a step-by-step implementation sequence for downstream workers.

---

## 5. Verification Method

To independently verify the technical findings and test gates:
1. **Verify Geodetic Unit Tests Baseline**:
   ```bash
   dotnet run --project HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj
   ```
   *Expected output*: `total: 161, failed: 0, succeeded: 158, skipped: 3`.
2. **Verify Civil 3D Mirror Invariant Baseline**:
   ```bash
   dotnet run --project HPCivil3d/HPCivil3d.McpBridge.Tests/HPCivil3d.McpBridge.Tests.csproj
   ```
   *Expected output*: `total: 60, failed: 0, succeeded: 60, skipped: 0`.
3. **Inspect Implementation Plan**:
   Review `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m2_repack_plan.md`.
