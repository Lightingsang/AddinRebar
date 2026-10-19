# Handoff Report: Tier 1 & Tier 2 Pseudo-Tests Remediation

**Agent**: `explorer_gh_m5_it3_1` (Tier 1 & Tier 2 Pseudo-Tests Remediation Explorer)  
**Parent Agent**: `65736fe1-0fe9-4743-9fff-fd7f010af55d`  
**Date**: 2026-10-10T14:38:00Z  
**Scope**: Authentic C# Drop-in Replacements for Tier 1 and Tier 2 Acceptance Tests  
**Report Document**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it3_1\tier1_tier2_remediation.md`

---

## 1. Observation

Reviewer `reviewer_gh_m5_it2_1` rejected Gate M5 Iteration 2 (REQUEST_CHANGES, tagged INTEGRITY VIOLATION) citing residual self-certifying pseudo-tests in Tier 1 and Tier 2:

1. `Tier1_NamedPipeAndWpfTests.cs:149-163` (`TC_T1_WPF_05_MaterialDesignThemeSynchronization`):
   ```csharp
   var validThemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Light", "Dark" };
   Assert.Contains("Light", validThemes);
   bool isDark = true;
   string activeThemeFile = isDark ? "ThemeDark.xaml" : "ThemeLight.xaml";
   Assert.Equal("ThemeDark.xaml", activeThemeFile);
   ```
   *Flaw*: Evaluates a local C# ternary expression and local HashSet; tests zero theme synchronization or XAML resources.

2. `Tier1_RegistryAndSeedsTests.cs:77-90` (`TC_T1_SED_01_SeedCatalogCompleteness`):
   ```csharp
   var seeds = new[] { "create_levels_grids", "generate_columns_beams", ... };
   Assert.Equal(12, seeds.Length);
   ```
   *Flaw*: Validates a hardcoded string array; does not query dynamic assembly seeds.

3. `Tier1_CanvasAndPlayerToolsTests.cs:89-115` (`TC_T1_RPL_02` & `TC_T1_RPL_03`):
   ```csharp
   var inputs = new Dictionary<string, object> { ["FloorCount"] = 25, ["FloorHeight"] = 3.8 };
   Assert.Equal(2, inputs.Count);
   var outputs = new Dictionary<string, object> { ["TotalHeight"] = 95.0, ... };
   var filtered = outputs.Where(...).ToDictionary(...);
   Assert.Equal(2, filtered.Count);
   ```
   *Flaw*: Populates and filters local dictionaries in memory; zero player or bridge input injection logic is executed.

4. `Tier1_EditAndExportToolsTests.cs:124-129` (`TC_T1_IMG_04_Base64SuppressionOptimization`):
   ```csharp
   var returnBase64 = false;
   string base64Output = returnBase64 ? "base64data" : string.Empty;
   Assert.Empty(base64Output);
   ```
   *Flaw*: Asserts a local ternary expression without invoking image export options.

5. `Tier2_RegistryAndSeedBoundariesTests.cs:48-57` (`TC_T2_REG_04_PublishWithoutPriorTestingNegativeTest`):
   ```csharp
   static bool CanPublishState(string state) => string.Equals(state, "tested", StringComparison.OrdinalIgnoreCase);
   Assert.False(CanPublishState("draft"));
   ```
   *Flaw*: Tests an ad-hoc local static function rather than production `ToolLifecycleService` or `ToolValidator`.

6. Tier 2 BCL/primitive tests:
   - `Tier2_CanvasAndPlayerBoundariesTests.cs:73-128`: `TC_T2_RPL_01` (calls `File.Exists`), `TC_T2_RPL_04` (calls `double.TryParse`), `TC_T2_RPL_05` (calls `Path.GetFileName`).
   - `Tier2_EditAndExportBoundariesTests.cs:76-108`: `TC_T2_IMG_02`, `03` (calls `Math.Clamp`), `TC_T2_IMG_04` (calls `Math.Max`).
   - `Tier2_RegistryAndSeedBoundariesTests.cs:118-127`: `TC_T2_SED_05` (calls `new DateTime(2026, 13, 1)`).

---

## 2. Logic Chain

1. In accordance with `TEST_INFRA.md` and repository integrity rules, every test must exercise actual production deliverables, public contracts, or bridge infrastructure rather than local arithmetic, local collections, or local dummy helpers.
2. In `HPGrasshopper.McpBridge`, `ThemeDark.xaml` and `ThemeLight.xaml` define brush tokens (`Brush.Background`, `Brush.Surface`, etc.), and `GrasshopperBridgeStatusView.xaml` binds to them. `TC_T1_WPF_05` is remediated by parsing both XAML files, verifying exact token symmetry between Dark and Light palettes, and verifying that the WPF view merges `ThemeDark.xaml` and references these brush tokens.
3. In `HPGrasshopper.Mcp.Server`, the 12 seeds are embedded resources. `GrasshopperE2EContracts.EmbeddedSeeds` calls `SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly)`. `TC_T1_SED_01` is remediated by querying `GrasshopperE2EContracts.EmbeddedSeeds` directly.
4. In `GrasshopperE2EBridge`, `ExecuteRequest.Args` feeds into `ScriptArgs` in Roslyn scripts. `TC_T1_RPL_02` and `TC_T1_RPL_03` are remediated by dispatching execution requests through `bridge.ExecuteAsync` and asserting evaluated script return values.
5. In `HPGrasshopper.McpBridge.Tools.ImageExporter` and `ExportGrasshopperCanvasImageTool`, `returnBase64=false` suppresses the Base64 payload while outputting a PNG file to disk. `TC_T1_IMG_04` is remediated by invoking `GrasshopperE2EHarness.ExportCanvasImage` with `returnBase64=false`, asserting file creation on disk and an empty Base64 string payload.
6. In `HPRebar.Mcp.Server.Core.Registry.ToolLifecycleService`, `Publish(toolName)` enforces that tools in `ToolStatus.Draft` or `ToolStatus.Deprecated` throw `ToolNotRunnableException`. `TC_T2_REG_04` is remediated by invoking `ToolLifecycleService.Publish` on draft and deprecated tool records.
7. In `PlayerRunner`, non-existent definition paths and invalid extensions are rejected with error descriptions. `TC_T2_RPL_01` and `TC_T2_RPL_05` are remediated by calling `GrasshopperE2EHarness.RunPlayer` and validating `ScriptGuard` denial of process execution.
8. In `ScriptArgs`, non-numeric strings return null via `DoubleOrNull`. `TC_T2_RPL_04` is remediated by executing a Roslyn script that parses `args.DoubleOrNull("SliderValue")` and throws when non-numeric input is supplied.
9. In `ImageExporter.RenderCanvasToBitmap`, zoom is clamped between 0.25 and 4.0, and canvas dimensions have a 200x200 minimum fallback. `TC_T2_IMG_02`, `03`, `04` are remediated by calling `GrasshopperE2EHarness.ExportCanvasImage` with boundary zoom and zero-component canvas.
10. In `evaluate_solar_radiation` (embedded seed tool), solar geometry is evaluated against surface normals. `TC_T2_SED_05` is remediated by loading the real seed code via `SeedInstaller` and executing it with zero sun vector and opposing normal vectors.

---

## 3. Caveats

- **Scope Scoped to Tier 1 & Tier 2**: Tier 3 and Tier 5 contain additional pseudo-tests identified by the reviewer (`TC_T3_INT_02`, `TC_T3_INT_04`, `TC_T3_INT_05`, `TC_T3_INT_09`, `TC_T3_INT_14`, `TC_T5_FUZZ_*`). Those are addressed in parallel or by peer workers.
- **Pure net10.0 Test Runner**: The E2E tests run on `net10.0` headlessly. All WPF tests inspect genuine XAML markup and theme resources on disk without requiring an active Revit UI process.

---

## 4. Conclusion

All 13 flagged methods across Tier 1 and Tier 2 have complete, authentic, drop-in C# code implementations specified in `tier1_tier2_remediation.md`.
All proposed methods strictly comply with:
- **Rule C2**: File length $\le 300$ lines (all files remain $\le 185$ lines).
- **Rule M3**: Method length $\le 50$ lines (all methods are $\le 40$ lines).
- **Rule Q-B1**: Zero commented-out or dead code.
- **Rule Q-B2**: Zero unannotated empty catch blocks.
- **Integrity Rule**: Zero local dummy functions, zero local dictionary LINQ assertions, zero BCL math/primitive assertions.

---

## 5. Verification Method

To verify the designs and implementations:
1. Inspect the detailed blueprint:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it3_1\tier1_tier2_remediation.md`
2. Apply the drop-in replacements to:
   - `HPGrasshopper.E2E.Tests/Contracts/GrasshopperE2EModels.cs`
   - `HPGrasshopper.E2E.Tests/Harness/GrasshopperE2EHarness.cs`
   - `HPGrasshopper.E2E.Tests/Tiers/Tier1_NamedPipeAndWpfTests.cs`
   - `HPGrasshopper.E2E.Tests/Tiers/Tier1_RegistryAndSeedsTests.cs`
   - `HPGrasshopper.E2E.Tests/Tiers/Tier1_CanvasAndPlayerToolsTests.cs`
   - `HPGrasshopper.E2E.Tests/Tiers/Tier1_EditAndExportToolsTests.cs`
   - `HPGrasshopper.E2E.Tests/Tiers/Tier2_RegistryAndSeedBoundariesTests.cs`
   - `HPGrasshopper.E2E.Tests/Tiers/Tier2_CanvasAndPlayerBoundariesTests.cs`
   - `HPGrasshopper.E2E.Tests/Tiers/Tier2_EditAndExportBoundariesTests.cs`
3. Verify clean code AST limits:
   ```powershell
   python scratch/verify_clean_code_ast.py "HPGrasshopper/HPGrasshopper.E2E.Tests" 50
   ```
4. Build and test:
   ```powershell
   dotnet build HPGrasshopper/HPGrasshopper.slnx -c Release
   powershell -ExecutionPolicy Bypass -File .\run-e2e-grasshopper-tests.ps1 -Tier Tier1
   powershell -ExecutionPolicy Bypass -File .\run-e2e-grasshopper-tests.ps1 -Tier Tier2
   ```
