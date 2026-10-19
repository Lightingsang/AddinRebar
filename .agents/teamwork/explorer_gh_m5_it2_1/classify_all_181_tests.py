import os
import glob
import re

tier_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.E2E.Tests\Tiers"

categories = {
    "A_Trivial_Local_Arithmetic_Or_Constant": [],
    "B_Self_Certifying_Or_Hardcoded_Logic": [],
    "C_Shadow_Constant_Testing": [],
    "D_Genuine": []
}

all_test_details = []

for filepath in sorted(glob.glob(os.path.join(tier_dir, "*.cs"))):
    fname = os.path.basename(filepath)
    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()
    
    pattern = r"(\[(?:Fact|Theory)\][\s\S]*?(?:public void|public async Task)\s+(\w+)\s*\([^)]*\)[\s\S]*?\{([\s\S]*?\n    \}))"
    matches = re.findall(pattern, content)
    
    for full_decl, name, body in matches:
        body_text = body.strip()
        lines = [l.strip() for l in body_text.split("\n") if l.strip() and not l.strip().startswith("//")]
        
        assigned_cat = None
        notes = []
        
        # Check specific known violations
        if "TC_T2_DLS_04_ReadOnlyDocumentTransactionPrevention" in name:
            assigned_cat = "A_Trivial_Local_Arithmetic_Or_Constant"
            notes.append("Assert.True(isReadOnly) where bool isReadOnly = true")
        elif "TC_T1_WPF_04_ScriptPreviewAndCopyCommand" in name:
            assigned_cat = "B_Self_Certifying_Or_Hardcoded_Logic"
            notes.append("Assert.Contains('doc.Create', preview) where preview is local literal")
        elif "TC_T1_WPF_05_MaterialDesignThemeSynchronization" in name:
            assigned_cat = "B_Self_Certifying_Or_Hardcoded_Logic"
            notes.append("theme == 'Light' || theme == 'Dark' loop over local array ['Light', 'Dark']")
        elif "TC_T1_RPL_05_ExecutionDurationTelemetry" in name:
            assigned_cat = "A_Trivial_Local_Arithmetic_Or_Constant"
            notes.append("Assert.True(durationMs > 0) where var durationMs = 42L")
        elif "TC_T1_EXC_05_ScriptArgsParameterParsing" in name:
            assigned_cat = "B_Self_Certifying_Or_Hardcoded_Logic"
            notes.append("Assert.Contains('2.5', argsJson) without JSON parsing or DTO serialization")
        elif "TC_T1_IMG_03_ZoomScalingAdjustment" in name:
            assigned_cat = "A_Trivial_Local_Arithmetic_Or_Constant"
            notes.append("Assert.Equal(2000, widthScaled) where widthScaled is local arithmetic (1000 * 2.0 / 1.0)")
        elif "TC_T2_BLD_03_StrictRuleC2ComplianceBoundary" in name:
            assigned_cat = "A_Trivial_Local_Arithmetic_Or_Constant"
            notes.append("Assert.True(targetMaxLines < maxAllowedLines) comparing local literals 230 < 300")
        elif "TC_T2_BLD_04_StrictRuleM3ComplianceBoundary" in name:
            assigned_cat = "A_Trivial_Local_Arithmetic_Or_Constant"
            notes.append("Assert.True(targetMaxMethodLines < maxAllowedMethodLines) comparing local literals 40 < 50")
        elif "TC_T2_BLD_05_HostProfileDefaultClampingInvariant" in name:
            assigned_cat = "A_Trivial_Local_Arithmetic_Or_Constant"
            notes.append("Assert.True(timeout <= 600) comparing local literal 600 <= 600")
        elif "TC_T4_RW_02_FacadeAdaptivePanelizationAndCladding" in name:
            assigned_cat = "A_Trivial_Local_Arithmetic_Or_Constant"
            notes.append("Assert.Equal(72, expectedPanels) and 288 corner points from local 12 * 6 arithmetic")
        elif "TC_T5_CNC_05_ThreadRelease_BusyFlagHonesty" in name:
            assigned_cat = "A_Trivial_Local_Arithmetic_Or_Constant"
            notes.append("Interlocked.Exchange on local int busy variable")
        elif "TC_T5_AST_08_DynamicToolRegistrar_MutatingTool_InjectsDryRun" in name:
            assigned_cat = "B_Self_Certifying_Or_Hardcoded_Logic"
            notes.append("transaction is 'auto' or 'manual' on local string literal 'auto'")
        elif "TC_T5_AST_09_DynamicToolRegistrar_ReadOnlyTool_OmitsDryRun" in name:
            assigned_cat = "B_Self_Certifying_Or_Hardcoded_Logic"
            notes.append("transaction is 'auto' or 'manual' on local string literal 'none'")
        elif "TC_T1_BEC_05_TempIdInternalResolution" in name:
            assigned_cat = "A_Trivial_Local_Arithmetic_Or_Constant"
            notes.append("Assert.Equal(1000, 1000) or local temp ID arithmetic")
        elif "TC_T2_IMG_02_ExtremeZoomFactorLowerClamping" in name:
            assigned_cat = "A_Trivial_Local_Arithmetic_Or_Constant"
            notes.append("Assert.Equal(0.25, Math.Clamp(-10.0, 0.25, 4.0)) purely local math")
        elif "TC_T2_IMG_03_ExtremeZoomFactorUpperClamping" in name:
            assigned_cat = "A_Trivial_Local_Arithmetic_Or_Constant"
            notes.append("Assert.Equal(4.0, Math.Clamp(100.0, 0.25, 4.0)) purely local math")
        elif "TC_T2_IMG_05_IllegalOutputFilePathCharactersNegativeTest" in name:
            assigned_cat = "B_Self_Certifying_Or_Hardcoded_Logic"
            notes.append("Assert.True(hasInvalidChars) on local string with pipe character")
        elif "TC_T2_RPL_02_CorruptedArchiveFileNegativeTest" in name:
            assigned_cat = "B_Self_Certifying_Or_Hardcoded_Logic"
            notes.append("Assert.False(isGhHeader) on local byte array {0,1,2,3}")
        elif "TC_T2_RPL_03_EmptyInputsObjectBoundaryTest" in name:
            assigned_cat = "A_Trivial_Local_Arithmetic_Or_Constant"
            notes.append("Assert.Empty(new Dictionary<string, object>()) asserts local empty dict")
        elif "TC_T2_REG_04_PublishWithoutPriorTestingNegativeTest" in name:
            assigned_cat = "B_Self_Certifying_Or_Hardcoded_Logic"
            notes.append("Assert.False(canPublish) where canPublish = (toolState == 'tested') with local toolState='draft'")
        elif "TC_T2_SED_02_MissingRequiredSeedParameterNegativeTest" in name:
            assigned_cat = "B_Self_Certifying_Or_Hardcoded_Logic"
            notes.append("Assert.False(argsJson.Contains('elevations')) on local string '{}'")
        elif "TC_T2_SED_04_NullGeometryListInputNegativeTest" in name:
            assigned_cat = "A_Trivial_Local_Arithmetic_Or_Constant"
            notes.append("Assert.False(hasElements) on local string[]? geom = null")
        elif "TC_T2_WPF_05_ClipboardExceptionResilience" in name:
            assigned_cat = "B_Self_Certifying_Or_Hardcoded_Logic"
            notes.append("Assert.True(clipboardHandled) where clipboardHandled is set in local try/catch")
        elif "TC_T5_CNC_04_CancelOfCancel_NoDeadlock" in name:
            assigned_cat = "B_Self_Certifying_Or_Hardcoded_Logic"
            notes.append("Assert.True(cancelHandled) where cancelHandled is set in local try/catch")
        elif "TC_T5_SEC_08_MalformedJsonRpc_SafetyRejection" in name:
            assigned_cat = "B_Self_Certifying_Or_Hardcoded_Logic"
            notes.append("Assert.True(parseThrew) in local JsonDocument.Parse try/catch without calling RPC handler")
        elif "TC_T5_FUZZ_06_SearchToolsOversizedQueryHandling" in name:
            assigned_cat = "A_Trivial_Local_Arithmetic_Or_Constant"
            notes.append("Assert.Equal(200, truncated.Length) where truncated is local substring on string of 10000 'q'")
        elif "TC_T5_FUZZ_08_LevelsGridsEmptyElevationsRejection" in name:
            assigned_cat = "A_Trivial_Local_Arithmetic_Or_Constant"
            notes.append("Assert.False(valid) where elevations = Array.Empty<double>()")
        elif "TC_T1_REG_03_TestToolSandboxExecution" in name:
            assigned_cat = "A_Trivial_Local_Arithmetic_Or_Constant"
            notes.append("Assert.True(req.DryRun) where req = new GrasshopperExecutionRequest('return 42;', DryRun: true)")
        elif "TC_T1_REG_04_PublishToolPromotion" in name:
            assigned_cat = "C_Shadow_Constant_Testing"
            notes.append("Assert.Contains('publish_tool', GrasshopperE2EContracts.RegistryMetaTools)")
        elif "TC_T1_REG_05_ManageToolRetirementLifecycle" in name:
            assigned_cat = "C_Shadow_Constant_Testing"
            notes.append("Assert.Contains('manage_tool', GrasshopperE2EContracts.RegistryMetaTools)")
        elif "TC_T2_BLD_01_ZeroDisallowedNativeAssemblyReferences" in name:
            assigned_cat = "C_Shadow_Constant_Testing"
            notes.append("Assert.Contains('System.IO', GrasshopperE2EContracts.DeniedNamespaces)")
        elif "TC_T2_BLD_02_ZeroNativeDependenciesInE2eTestSuite" in name:
            assigned_cat = "C_Shadow_Constant_Testing"
            notes.append("Assert.Equal('grasshopper', GrasshopperE2EContracts.HostKey)")
        elif "TC_T1_NPS_01_PipeResolutionStandard" in name:
            assigned_cat = "C_Shadow_Constant_Testing"
            notes.append("Assert.Equal('hpgrasshopper-mcp-2026', GrasshopperE2EContracts.ResolvePipeName('grasshopper', 2026))")
        else:
            # Let's inspect remaining tests
            # Check if any other test asserts on local arithmetic or literal
            has_lit_assert = False
            for l in lines:
                if re.search(r'Assert\.True\(\s*\w+\s*(?:<=|>=|<|>|==|!=)\s*\d+\s*\)', l):
                    has_lit_assert = True
                if re.search(r'Assert\.\w+\([^,)]*\s*[\+\*\-\/]\s*[^,)]*\)', l):
                    has_lit_assert = True
            if has_lit_assert:
                notes.append(f"Suspicious line in body: {[l for l in lines if 'Assert.' in l]}")
                assigned_cat = "A_Trivial_Local_Arithmetic_Or_Constant"
            else:
                assigned_cat = "D_Genuine"
                
        all_test_details.append({
            "file": fname,
            "name": name,
            "category": assigned_cat,
            "notes": notes,
            "lines": lines
        })
        categories[assigned_cat].append({
            "file": fname,
            "name": name,
            "notes": notes
        })

print(f"Summary:")
print(f"  Category A (Trivial Arithmetic / Constants): {len(categories['A_Trivial_Local_Arithmetic_Or_Constant'])}")
print(f"  Category B (Self-Certifying / Hardcoded String/Bool): {len(categories['B_Self_Certifying_Or_Hardcoded_Logic'])}")
print(f"  Category C (Shadow Constant Only): {len(categories['C_Shadow_Constant_Testing'])}")
print(f"  Category D (Genuine / Substantive): {len(categories['D_Genuine'])}")
print(f"Total tests: {len(all_test_details)}")

print("\n--- Flagged Tests List ---")
for cat_key in ["A_Trivial_Local_Arithmetic_Or_Constant", "B_Self_Certifying_Or_Hardcoded_Logic", "C_Shadow_Constant_Testing"]:
    print(f"\n[{cat_key}] ({len(categories[cat_key])} tests):")
    for item in categories[cat_key]:
        print(f"  * {item['file']} :: {item['name']}")
        for n in item['notes']:
            print(f"      -> {n}")
