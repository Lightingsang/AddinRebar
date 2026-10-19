import os
import glob
import re

tier_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.E2E.Tests\Tiers"

# List of 43 flagged method names
with open(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it2_1\audit_files.py", "r", encoding="utf-8") as f:
    audit_script = f.read()

# Let's extract and print the code of each flagged test
flagged_names = [
    # Tier 1
    ("Tier1_BuildAndIntegrityTests.cs", "TC_T1_INT_01_CoreToolsCountInvariant"),
    ("Tier1_CanvasAndPlayerToolsTests.cs", "TC_T1_RPL_05_ExecutionDurationTelemetry"),
    ("Tier1_EditAndExportToolsTests.cs", "TC_T1_IMG_03_ZoomScalingAdjustment"),
    ("Tier1_ExecutionAndSafetyTests.cs", "TC_T1_EXC_05_ScriptArgsParameterParsing"),
    ("Tier1_NamedPipeAndWpfTests.cs", "TC_T1_NPS_01_PipeServerNameResolution"),
    ("Tier1_NamedPipeAndWpfTests.cs", "TC_T1_WPF_04_ScriptPreviewAndCopyCommand"),
    ("Tier1_NamedPipeAndWpfTests.cs", "TC_T1_WPF_05_MaterialDesignThemeSynchronization"),
    ("Tier1_RegistryAndSeedsTests.cs", "TC_T1_REG_01_SearchToolsDiscovery"),
    ("Tier1_RegistryAndSeedsTests.cs", "TC_T1_REG_03_TestToolSandboxExecution"),
    ("Tier1_RegistryAndSeedsTests.cs", "TC_T1_REG_04_PublishToolPromotion"),
    ("Tier1_RegistryAndSeedsTests.cs", "TC_T1_REG_05_ManageToolRetirementLifecycle"),
    ("Tier1_SED_01_SeedCatalogCompleteness" in audit_script and ("Tier1_RegistryAndSeedsTests.cs", "TC_T1_SED_01_SeedCatalogCompleteness")),
    ("Tier1_RegistryAndSeedsTests.cs", "TC_T1_SED_05_StructuralRebarSeedsParameterCoverage"),
    # Tier 2
    ("Tier2_BuildAndIntegrityBoundariesTests.cs", "TC_T2_BLD_01_ZeroDisallowedNativeAssemblyReferences"),
    ("Tier2_BuildAndIntegrityBoundariesTests.cs", "TC_T2_BLD_02_ZeroNativeDependenciesInE2eTestSuite"),
    ("Tier2_BuildAndIntegrityBoundariesTests.cs", "TC_T2_BLD_03_StrictRuleC2ComplianceBoundary"),
    ("Tier2_BuildAndIntegrityBoundariesTests.cs", "TC_T2_BLD_04_StrictRuleM3ComplianceBoundary"),
    ("Tier2_BuildAndIntegrityBoundariesTests.cs", "TC_T2_BLD_05_HostProfileDefaultClampingInvariant"),
    ("Tier2_CanvasAndPlayerBoundariesTests.cs", "TC_T2_RPL_02_CorruptedArchiveFileNegativeTest"),
    ("Tier2_CanvasAndPlayerBoundariesTests.cs", "TC_T2_RPL_03_EmptyInputsObjectBoundaryTest"),
    ("Tier2_EditAndExportBoundariesTests.cs", "TC_T2_IMG_02_ExtremeZoomFactorLowerClamping"),
    ("Tier2_EditAndExportBoundariesTests.cs", "TC_T2_IMG_03_ExtremeZoomFactorUpperClamping"),
    ("Tier2_EditAndExportBoundariesTests.cs", "TC_T2_IMG_05_IllegalOutputFilePathCharactersNegativeTest"),
    ("Tier2_ExecutionAndSafetyBoundariesTests.cs", "TC_T2_DLS_04_ReadOnlyDocumentTransactionPrevention"),
    ("Tier2_NamedPipeAndWpfBoundariesTests.cs", "TC_T2_WPF_05_ClipboardExceptionResilience"),
    ("Tier2_RegistryAndSeedBoundariesTests.cs", "TC_T2_REG_04_PublishWithoutPriorTestingNegativeTest"),
    ("Tier2_RegistryAndSeedBoundariesTests.cs", "TC_T2_SED_02_MissingRequiredSeedParameterNegativeTest"),
    ("Tier2_RegistryAndSeedBoundariesTests.cs", "TC_T2_SED_03_ParameterFuzzingWithNegativeDimensions"),
    ("Tier2_RegistryAndSeedBoundariesTests.cs", "TC_T2_SED_04_NullGeometryListInputNegativeTest"),
    ("Tier2_RegistryAndSeedBoundariesTests.cs", "TC_T2_SED_05_SolarRadiationDateTimeRangeBoundary"),
    # Tier 4
    ("Tier4_RealWorldWorkloadTests.cs", "TC_T4_RW_02_FacadeAdaptivePanelizationAndCladding"),
    # Tier 5
    ("Tier5_CancellationStressTests.cs", "TC_T5_CNC_04_CancelOfCancel_NoDeadlock"),
    ("Tier5_CancellationStressTests.cs", "TC_T5_CNC_05_ThreadRelease_BusyFlagHonesty"),
    ("Tier5_RoslynAstMutationTests.cs", "TC_T5_AST_08_DynamicToolRegistrar_MutatingTool_InjectsDryRun"),
    ("Tier5_RoslynAstMutationTests.cs", "TC_T5_AST_09_DynamicToolRegistrar_ReadOnlyTool_OmitsDryRun"),
    ("Tier5_SecurityOptInAndDenyListTests.cs", "TC_T5_SEC_08_MalformedJsonRpc_SafetyRejection"),
    ("Tier5_SeedParameterFuzzingTests.cs", "TC_T5_FUZZ_01_PlayerFilePathValidation"),
    ("Tier5_SeedParameterFuzzingTests.cs", "TC_T5_FUZZ_02_PlayerInvalidExtensionRejection"),
    ("Tier5_SeedParameterFuzzingTests.cs", "TC_T5_FUZZ_06_SearchToolsOversizedQueryHandling"),
    ("Tier5_SeedParameterFuzzingTests.cs", "TC_T5_FUZZ_08_LevelsGridsEmptyElevationsRejection"),
    ("Tier5_SeedParameterFuzzingTests.cs", "TC_T5_FUZZ_09_ColumnsBeamsZeroLengthRejection"),
    ("Tier5_SeedParameterFuzzingTests.cs", "TC_T5_FUZZ_10_FloorsSlabsNonClosedPolygonRejection"),
    ("Tier5_SeedParameterFuzzingTests.cs", "TC_T5_FUZZ_11_StirrupDistributionNonPositiveSpacingRejection")
]

out = []
for item in flagged_names:
    if not item:
        continue
    file_name, method_name = item
    file_path = os.path.join(tier_dir, file_name)
    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()
    
    # find method
    pattern = rf"(\[(?:Fact|Theory)\][\s\S]*?(?:public void|public async Task)\s+{method_name}\s*\([^)]*\)[\s\S]*?\{{[\s\S]*?\n    \}})"
    match = re.search(pattern, content)
    if match:
        out.append(f"// FILE: {file_name}\n" + match.group(1) + "\n")
    else:
        out.append(f"// NOT FOUND: {file_name} :: {method_name}\n")

with open(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it2_1\flagged_methods_code.cs", "w", encoding="utf-8") as f:
    f.write("\n".join(out))

print(f"Wrote {len(flagged_names)} methods to flagged_methods_code.cs")
