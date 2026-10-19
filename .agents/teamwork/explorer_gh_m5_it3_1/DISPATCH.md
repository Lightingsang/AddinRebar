## 2026-10-10T14:25:43Z
You are explorer_gh_m5_it3_1 (Tier 1 & Tier 2 Pseudo-Tests Remediation Explorer).
Your working directory is:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it3_1

MANDATORY FIRST STEP:
Read the user requirements from:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Specifically section ## 2026-10-10T06:56:11Z.

Also read:
- Master project index: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md
- Test Infra specification: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_INFRA.md
- Full Reviewer 1 Evidence Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_gh_m5_it2_1\handoff.md
- Worker M5 Iteration 2 handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_gh_m5_it2\handoff.md

CONTEXT:
Reviewer reviewer_gh_m5_it2_1 rejected Gate M5 Iteration 2 (REQUEST_CHANGES, tagged INTEGRITY VIOLATION) due to residual self-certifying pseudo-tests in Tier 1 and Tier 2:
1. `TC_T1_WPF_05_MaterialDesignThemeSynchronization` (`Tier1_NamedPipeAndWpfTests.cs:149-163`): Asserts local HashSet and ternary expression. Must test real Theme metadata / ResourceDictionary binding from MaterialDesign or `GrasshopperBridgeStatusView`.
2. `TC_T1_SED_01_SeedCatalogCompleteness` (`Tier1_RegistryAndSeedsTests.cs:77-90`): Asserts hardcoded inline string array of 12 names. Must invoke `GrasshopperE2EContracts.EmbeddedSeeds` (which calls `SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly)`).
3. `TC_T1_RPL_02_ParameterInputInjection` & `TC_T1_RPL_03_OutputExtractionFiltering` (`Tier1_CanvasAndPlayerToolsTests.cs:89-115`): Tests local Dictionary and LINQ. Must use `GrasshopperE2EHarness` or `ExecuteAsync` with injected input parameters (`ScriptArgs`) and verify bridge processing.
4. `TC_T1_IMG_04_Base64SuppressionOptimization` (`Tier1_EditAndExportToolsTests.cs:124-129`): Local ternary expression. Must invoke export options handling with `returnBase64=false` and verify payload DTO has empty/null Base64 string while creating disk output.
5. `TC_T2_REG_04_PublishWithoutPriorTestingNegativeTest` (`Tier2_RegistryAndSeedBoundariesTests.cs:48-57`): Defines dummy local static function `CanPublishState`. Must invoke `ToolValidator.Validate` across states (`draft`, `proposed`, `testing`, `tested`, `retired`) or `ToolLifecycleService`.
6. Tier 2 BCL/primitive tests:
   - `Tier2_CanvasAndPlayerBoundariesTests.cs:73-125`: `TC_T2_RPL_01` (File.Exists), `TC_T2_RPL_04` (double.TryParse), `TC_T2_RPL_05` (Path.GetFileName).
   - `Tier2_EditAndExportBoundariesTests.cs:76-105`: `TC_T2_IMG_02`, `03` (Math.Clamp), `TC_T2_IMG_04` (Math.Max).
   - `Tier2_RegistryAndSeedBoundariesTests.cs:118`: `TC_T2_SED_05` (new DateTime).

OBJECTIVE:
1. Design concrete, authentic C# drop-in code replacements for every single flagged method in Tier 1 and Tier 2.
2. Bind every test to genuine production contracts in `HPGrasshopper.Mcp.Server`, `McpShared`, and `GrasshopperE2EBridge`.
3. Ensure all proposed methods strictly comply with Rule C2 (<= 300 lines) and Rule M3 (<= 50 lines).
4. Save report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it3_1\tier1_tier2_remediation.md` and write `handoff.md`.
5. Call send_message when done.
