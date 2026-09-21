# Handoff Report: Milestone M1 — HPAutoCad.Tests Migration & Solution Integration

**Agent**: `explorer_m1_tests`  
**Recipient**: `orchestrator_3` (Conversation ID: `050984c1-afaa-4911-859c-331e9279dc4f`)  
**Target Plan**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m1_tests_plan.md`  
**Date**: 2026-09-20T12:58:00Z  
**Type**: Hard Handoff (Task Complete)  

---

## 1. Observation

1. **Legacy Test Suite Inventory (`HPGeo/HPGeo.Tests/`)**:
   - Exactly **12 test fixture files** exist:
     - `ConverterTests.cs` (12 tests)
     - `ProjectionGoldenTests.cs` (21 tests)
     - `ProvinceCatalogTests.cs` (6 tests)
     - `ImportTests.cs` (8 tests)
     - `KmlTests.cs` (10 tests)
     - `ImageryTests.cs` (28 tests)
     - `ImageArgumentsTests.cs` (6 tests)
     - `ReviewRegressionTests.cs` (10 tests)
     - `GeoExportViewModelTests.cs` (14 tests)
     - `GeoImportViewModelTests.cs` (5 tests)
     - `ImageryPipelineTests.cs` (38 tests)
     - `TileFetchHelperTests.cs` (3 tests)
   - Exactly **1 helper class**: `Fixtures/GoldenFixtures.cs` (strongly typed JSON reader).
   - Exactly **3 JSON golden files** in `Fixtures/`:
     - `golden-vn2000-to-wgs84.json`
     - `golden-wgs84-to-vn2000.json`
     - `golden-webmercator.json`
   - Total test count: **161 tests** (158 passed, 3 skipped when `HPGEO_LIVE_TILES != 1`). Verified via `dotnet test` in `HPGeo/` (exit code 0, duration 1.725s).

2. **Project Configuration Analysis**:
   - `HPGeo.Tests.csproj` targets `net10.0-windows` with `OutputType=Exe`, `UseMicrosoftTestingPlatformRunner=true`, `xunit.v3` 3.1.0, and `xunit.runner.visualstudio` 3.1.5.
   - Project references in legacy: `..\HPGeo.Core\HPGeo.Core.csproj` and `..\HPGeo.AutoCad\HPGeo.AutoCad.csproj`.
   - `HPAutoCad/global.json` pins `sdk.version=10.0.300` and `test.runner=Microsoft.Testing.Platform`.
   - Peer test projects in `HPAutoCad/` (`HPAutoCad.Aec.Tests.csproj`, `HPAutoCad.Mcp.Server.Tests.csproj`) both target `net10.0-windows` with `OutputType=Exe` and `UseMicrosoftTestingPlatformRunner=true`.

3. **Solution Structure (`HPAutoCad/HPAutoCad.slnx`)**:
   - Current solution has 6 projects + 3 shared McpShared projects.
   - `dotnet test` in `HPAutoCad/` currently executes 505 tests across `HPAutoCad.Mcp.Server.Tests` (49 tests) and `HPAutoCad.Aec.Tests` (456 tests), passing 100% in 8.6s.

4. **Mirror Test Boundary (`HPCivil3d/HPCivil3d.McpBridge.Tests/`)**:
   - `MirrorTests.cs` lines 68 & 87 strictly scan source files in three specific folders:
     `"HPAutoCad.McpBridge"`, `"HPAutoCad.McpBridge.Loader"`, `"HPAutoCad.Mcp.Server"`.
   - `HPCivil3d/tools/mirror-tokens.json` does NOT track `HPAutoCad.slnx`, `HPAutoCad.Core`, `HPAutoCad.TileFetch`, or `HPAutoCad.Tests`.
   - Live test execution of `dotnet test HPCivil3d.McpBridge.Tests` passes 60/60 tests (exit code 0, 591ms).

---

## 2. Logic Chain

1. **Step 1 — Test Division & Dependency Resolution**:
   - Observation: 8 of the 12 test fixtures (101 tests) depend solely on `HPGeo.Core.*` (migrating to `HPAutoCad.Core.HPGeoLink.*`).
   - Observation: 4 test fixtures (60 tests) depend on `GeoExportViewModel`, `GeoImportViewModel`, `CrsSelectionViewModel`, `IGeoExportShell`, `IGeoImportShell`, `GeoExportItems`, `HelperTileFetcher`, and `TileStitcher`.
   - Inference: In Milestone M1, `HPAutoCad` (the add-in project) does not yet exist (it is planned for M2). If `HPAutoCad.Tests` only references `HPAutoCad.Core` and `HPAutoCad.TileFetch`, the 4 fixtures would fail compilation unless the 9 support classes are made available to the test compilation.
   - Conclusion: Co-locating the 9 host-free support classes in `HPAutoCad.Tests/HPGeoLink/Support/` with `<UseWPF>true</UseWPF>` and `CommunityToolkit.Mvvm` (8.4.0) resolves all compilation symbols immediately, allowing all 161 tests to pass in M1. In M2, these classes are promoted to `HPAutoCad` and referenced via `<ProjectReference Include="..\HPAutoCad\HPAutoCad.csproj" />`.

2. **Step 2 — Test Runner & Golden Data Resolution**:
   - Observation: `GoldenFixtures.cs` resolves JSON files via `Path.Combine(AppContext.BaseDirectory, "Fixtures", name)`.
   - Observation: `ImageryTests.cs` resolves `golden-webmercator.json` via `Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden-webmercator.json")`.
   - Inference: The 3 golden JSON files in `HPAutoCad.Tests/HPGeoLink/Fixtures/` must be deployed to `$(OutDir)Fixtures\`.
   - Conclusion: Configuring `<None Include="HPGeoLink\Fixtures\*.json" CopyToOutputDirectory="PreserveNewest" Link="Fixtures\%(Filename)%(Extension)" />` in `HPAutoCad.Tests.csproj` guarantees identical runtime file paths.

3. **Step 3 — Mirror Safety Guarantee**:
   - Observation: `MirrorTests.Every_AutoCAD_bridge_source_file_is_either_mirrored_or_an_owned_counterpart` passes `root, "HPAutoCad.McpBridge", "HPAutoCad.McpBridge.Loader", "HPAutoCad.Mcp.Server"` to `MirrorTokenTable.SourceFiles`.
   - Inference: New directories (`HPAutoCad.Core`, `HPAutoCad.TileFetch`, `HPAutoCad.Tests`) and changes to `HPAutoCad.slnx` fall outside the enumerated folders.
   - Conclusion: Adding the 3 new projects has zero impact on `HPCivil3d.McpBridge.Tests`.

---

## 3. Caveats

1. **Live Network Tests**:
   - 3 tests (`ImageryPipelineTests.Live_spike_fetches_four_real_tiles_and_stitches_512x512`, `Live_prefetch_fills_the_default_cache_for_the_acceptance_ring`, and `TileFetchHelperTests.Live_helper_fetches_four_real_tiles_into_a_temp_cache_through_the_host_side_fetcher`) require external internet access to tile providers and are gated behind `HPGEO_LIVE_TILES=1`. In standard CI and local unit test execution, these 3 tests are deliberately skipped, yielding exactly 158 passed and 3 skipped.
2. **TileFetch Companion Helper Exe Path**:
   - `TileFetchHelperTests.cs` checks `HelperExe` path at `..\..\..\..\HPAutoCad.TileFetch\bin\Debug\net8.0\HPAutoCad.TileFetch.exe`. `HPAutoCad.Tests.csproj` must declare a ProjectReference to `HPAutoCad.TileFetch.csproj` so the executable is built before the tests execute.
3. **M2 Migration Cleanup**:
   - When M2 creates `HPAutoCad/HPAutoCad.csproj`, the temporary support files in `HPAutoCad.Tests/HPGeoLink/Support/` should be removed in favor of referencing `HPAutoCad.csproj`.

---

## 4. Conclusion

The implementation plan for `HPAutoCad.Tests` is complete, verified, and formulated into the authoritative document:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m1_tests_plan.md`

All 161 tests, 12 fixtures, `GoldenFixtures.cs`, and 3 JSON golden files are mapped. `HPAutoCad.Tests.csproj` is fully specified with `net10.0-windows`, `xunit.v3` 3.1.0, MTP runner, and ProjectReferences. The solution `HPAutoCad.slnx` modification is detailed with exact diffs. Mirror test invariants have been validated live (60/60 pass). The worker agent has an unambiguous, step-by-step implementation guide.

---

## 5. Verification Method

To independently verify the plan and its execution by `worker_m1`:

1. **Verify Individual Test Execution**:
   ```bash
   dotnet test HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj -p:DeployBundle=false
   ```
   *Expected Result*: Total: 161, Failed: 0, Succeeded: 158, Skipped: 3, Duration: ~1.7s.

2. **Verify Solution-Wide Tests**:
   ```bash
   cd HPAutoCad
   dotnet test
   ```
   *Expected Result*: Total: 666, Failed: 0, Succeeded: 663, Skipped: 3, Duration: ~8.8s (covering `HPAutoCad.Mcp.Server.Tests`, `HPAutoCad.Aec.Tests`, and `HPAutoCad.Tests`).

3. **Verify Civil 3D Mirror Invariant**:
   ```bash
   cd HPCivil3d
   dotnet test HPCivil3d.McpBridge.Tests
   ```
   *Expected Result*: Total: 60, Failed: 0, Succeeded: 60, Skipped: 0. Zero drift.
