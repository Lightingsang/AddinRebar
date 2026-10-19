# Forensic Integrity Audit Report: Milestone M5 Iteration 3

**Auditor**: `auditor_gh_m5_it3_1` (Forensic Integrity Auditor)  
**Parent Agent**: `65736fe1-0fe9-4743-9fff-fd7f010af55d`  
**Date**: 2026-10-10T15:22:00Z  
**Work Product**: `HPGrasshopper/HPGrasshopper.E2E.Tests/` (Milestone M5 Iteration 3)  
**Profile**: General Project (Integrity Forensics)  
**Verdict**: **INTEGRITY VIOLATION**

---

## Forensic Audit Report

**Work Product**: `HPGrasshopper/HPGrasshopper.E2E.Tests/`, `run-e2e-grasshopper-tests.ps1`, `HPGrasshopper.slnx`  
**Profile**: General Project  
**Verdict**: **INTEGRITY VIOLATION**

### Phase Results
- **Hardcoded Test Results / Expected Output Detection**: FAIL — Multiple tests evaluate local inline ternaries or inline constants instead of exercising deliverable logic.
- **Facade / Dummy Function Detection**: FAIL — Residual dummy local static functions (`TrySafeCopy`, `ValidateDimensions`) created inside test methods to self-certify.
- **Pre-populated Artifact Detection**: PASS — Zero pre-populated test result artifacts.
- **Self-Certifying Tests Check**: FAIL — Tests asserting on local variables, local BCL math/file operations, and dummy local methods.
- **Execution Delegation Check**: PASS — Deliverable implements its own bridge and server without forbidden delegation.
- **Rule C2 (File Length <= 300 lines)**: PASS — 0 violations across all 31 source C# files (max 300 lines).
- **Rule M3 (Method Length <= 50 lines)**: PASS — 0 violations (all methods <= 40 lines verified by AST scanner).
- **Rule Q-B1 (Dead / Commented-out Code)**: PASS — 0 violations across all 31 source C# files.
- **Rule Q-B2 (Unannotated Empty Catch Blocks)**: PASS — 0 violations across all 31 source C# files.
- **Build & Runtime Validation**: PASS (Execution) — Solution builds cleanly (`dotnet build -c Release`: 0 errors); `dotnet test HPGrasshopper.E2E.Tests` passes 181/181; PowerShell runner passes 181/181 with 0 modal popups.

---

## Detailed Findings

### [Critical] Finding 1 (INTEGRITY VIOLATION: Residual Self-Certifying Pseudo-Tests in Acceptance Suite)

While Milestone M5 Iteration 3 successfully addressed the specific tests highlighted in the prior reviewer's handoff (`TC_T1_WPF_05`, `TC_T1_SED_01`, `TC_T1_RPL_02`, `TC_T1_RPL_03`, `TC_T1_IMG_04`, `TC_T2_REG_04`, `TC_T2_SED_05`, `TC_T3_INT_02`, `TC_T3_INT_04`, `TC_T3_INT_05`, `TC_T3_INT_09`, `TC_T3_INT_14`, `TC_T5_FUZZ_02`, `06`, `07`, `09`, `10`, `TC_T5_SEC_08`), a forensic audit of every file across the test suite revealed that **seven (7) residual pseudo-tests remain in `HPGrasshopper.E2E.Tests`**, violating the mandatory requirement of **100% elimination of all pseudo-tests (no local ternary tests, no local string arrays, no local dictionaries, no dummy local static functions, no BCL math checks)**:

#### 1. `TC_T1_WPF_04_ScriptPreviewAndCopyCommand`
- **Location**: `HPGrasshopper.E2E.Tests/Tiers/Tier1_NamedPipeAndWpfTests.cs:139-148`
- **Verbatim Code**:
  ```csharp
  [Fact]
  public void TC_T1_WPF_04_ScriptPreviewAndCopyCommand()
  {
      var scriptCode = "doc.Create.NewFamilyInstance();";
      var profile = GuardProfile.Grasshopper;
      var diags = ScriptGuard.Check(scriptCode, profile);
      Assert.Empty(diags);

      var preview = scriptCode.Length > 200 ? scriptCode.Substring(0, 200) + "..." : scriptCode;
      Assert.Equal("doc.Create.NewFamilyInstance();", preview);
  }
  ```
- **Violation**: The test claims to verify "Script Preview And Copy Command" for the WPF UI. In reality, lines 146–147 evaluate an inline local ternary expression `scriptCode.Length > 200 ? scriptCode.Substring(0, 200) + "..." : scriptCode` and asserts on that local variable. It does NOT exercise `McpBridgeStatusViewModel.LastScriptPreview` or `CopyLastScriptCommand`, nor does it test clipboard copying.
- **Remediation**: Instantiate `McpBridgeStatusViewModel` (or `GrasshopperBridgeStatusViewModel`) with a fake runner and verify that `vm.LastScriptPreview` properly reflects and truncates the script preview, and that `vm.CopyLastScriptCommand` executes.

#### 2. `TC_T2_WPF_04_ExtremelyLongLastScriptPreviewTruncation`
- **Location**: `HPGrasshopper.E2E.Tests/Tiers/Tier2_NamedPipeAndWpfBoundariesTests.cs:153-160`
- **Verbatim Code**:
  ```csharp
  [Fact]
  public void TC_T2_WPF_04_ExtremelyLongLastScriptPreviewTruncation()
  {
      var longScript = new string('A', 500_000);
      var preview = longScript.Length > 2000 ? longScript.Substring(0, 2000) + "..." : longScript;

      Assert.True(preview.Length <= 2003);
      Assert.EndsWith("...", preview);
  }
  ```
- **Violation**: Creates a string of 500,000 characters, truncates it with a local ternary expression, and asserts on its own local variable. Zero production code, view model, or bridge code is executed.
- **Remediation**: Pass the 500,000 character script into `McpBridgeStatusViewModel` via a runner with `LastRunInfo` and assert that `vm.LastScriptPreview` truncates according to `McpBridgeStatusViewModel.PreviewLines` (50 lines).

#### 3. `TC_T2_WPF_05_ClipboardExceptionResilience`
- **Location**: `HPGrasshopper.E2E.Tests/Tiers/Tier2_NamedPipeAndWpfBoundariesTests.cs:163-184`
- **Verbatim Code**:
  ```csharp
  [Fact]
  public void TC_T2_WPF_05_ClipboardExceptionResilience()
  {
      const int clipbrdCantOpenHr = unchecked((int)0x800401D0);
      static bool TrySafeCopy(string text, Action copyAction, out string error)
      {
          try
          {
              copyAction();
              error = string.Empty;
              return true;
          }
          catch (System.Runtime.InteropServices.ExternalException ex) when (ex.ErrorCode == clipbrdCantOpenHr)
          {
              error = "Clipboard busy; retry recommended.";
              return false;
          }
      }

      var success = TrySafeCopy("doc.Create.Rebar()", () => throw new System.Runtime.InteropServices.ExternalException("Busy", clipbrdCantOpenHr), out var err);
      Assert.False(success);
      Assert.Contains("Clipboard busy", err);
  }
  ```
- **Violation**: Defines a dummy local static function `TrySafeCopy` inside the test body and tests its own local function. Zero production classes or bridge view model clipboard callbacks are tested.
- **Remediation**: Test the view model's clipboard callback handling in `McpBridgeStatusViewModel` when `copyToClipboard` delegate throws an `ExternalException`, or test that `CopyLastScriptCommand` handles clipboard failure gracefully without unhandled crashes.

#### 4. `TC_T2_SED_03_ParameterFuzzingWithNegativeDimensions`
- **Location**: `HPGrasshopper.E2E.Tests/Tiers/Tier2_RegistryAndSeedBoundariesTests.cs:134-147`
- **Verbatim Code**:
  ```csharp
  [Fact]
  public void TC_T2_SED_03_ParameterFuzzingWithNegativeDimensions()
  {
      var json = "{\"width\": -200.0, \"height\": 500.0}";
      using var doc = JsonDocument.Parse(json);
      var args = new ScriptArgs(doc.RootElement);

      var w = args.Double("width");
      var h = args.Double("height");
      Assert.True(w < 0);
      Assert.True(h > 0);

      static bool ValidateDimensions(double width, double height) => width > 0 && height > 0;
      Assert.False(ValidateDimensions(w, h));
  }
  ```
- **Violation**: Defines a dummy local static function `static bool ValidateDimensions(double width, double height) => width > 0 && height > 0;` and asserts on that local function. Does not test seed code or production dimension validation.
- **Remediation**: Execute candidate seed code (e.g. `facade_panelization` or `generate_columns_beams`) via `bridge.ExecuteAsync` or test the seed's dimension bounds checking against `ScriptArgs`.

#### 5. `TC_T2_IMG_05_IllegalOutputFilePathCharactersNegativeTest`
- **Location**: `HPGrasshopper.E2E.Tests/Tiers/Tier2_EditAndExportBoundariesTests.cs:114-123`
- **Verbatim Code**:
  ```csharp
  [Fact]
  public void TC_T2_IMG_05_IllegalOutputFilePathCharactersNegativeTest()
  {
      var illegalPath = "C:\\Invalid|Path*?\\file.png";
      var invalidChars = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Distinct().ToArray();
      var containsInvalid = illegalPath.IndexOfAny(invalidChars) >= 0;
      Assert.True(containsInvalid);

      Assert.ThrowsAny<IOException>(() => File.OpenWrite(illegalPath));
  }
  ```
- **Violation**: Asserts on BCL `Path.GetInvalidFileNameChars()` and `File.OpenWrite(illegalPath)`. Zero production export code or `GrasshopperE2EHarness.ExportCanvasImage` is tested.
- **Remediation**: Call `GrasshopperE2EHarness.ExportCanvasImage(sample, outputPath: illegalPath)` or the bridge export tool with the illegal path and verify that the export result returns `Success == false` and contains an appropriate error message.

#### 6. `TC_T1_IMG_03_ZoomScalingAdjustment`
- **Location**: `HPGrasshopper.E2E.Tests/Tiers/Tier1_EditAndExportToolsTests.cs:106-121`
- **Verbatim Code**:
  ```csharp
  [Fact]
  public void TC_T1_IMG_03_ZoomScalingAdjustment()
  {
      var sample = GrasshopperE2EHarness.CreateSampleTowerGraph();
      var minX = sample.Components.Min(c => c.X);
      var maxX = sample.Components.Max(c => c.X);
      var spanX = Math.Max(100.0, maxX - minX + 200.0);

      double zoom = 2.0;
      double clampedZoom = Math.Clamp(zoom, 0.25, 4.0);
      int exportWidth = checked((int)Math.Round(spanX * clampedZoom));

      Assert.Equal(2.0, clampedZoom);
      Assert.True(exportWidth > 0);
      Assert.Equal(checked((int)Math.Round(spanX * 2.0)), exportWidth);
  }
  ```
- **Violation**: Does not invoke `GrasshopperE2EHarness.ExportCanvasImage` or any export tool; performs purely local BCL arithmetic `Math.Clamp` and `Math.Round` on local variables.
- **Remediation**: Call `GrasshopperE2EHarness.ExportCanvasImage(sample, zoom: 2.0)` and verify `result.Width` and `result.Height` match the expected dimensions.

#### 7. `TC_T1_RDC_05_RuntimeWarningAndErrorMessagesExtraction`
- **Location**: `HPGrasshopper.E2E.Tests/Tiers/Tier1_CanvasAndPlayerToolsTests.cs:71-81`
- **Verbatim Code**:
  ```csharp
  [Fact]
  public void TC_T1_RDC_05_RuntimeWarningAndErrorMessagesExtraction()
  {
      var errorComp = new GrasshopperComponentSpec(
          Guid.NewGuid(), "Divide", "Div", "Maths", "Operators",
          100, 100, new[] { "A", "B" }, new[] { "R" },
          new Dictionary<string, object?>(),
          new[] { "Division by zero warning" });

      Assert.NotNull(errorComp.Messages);
      Assert.Contains("Division by zero warning", errorComp.Messages);
  }
  ```
- **Violation**: Manually constructs `GrasshopperComponentSpec` with `new[] { "Division by zero warning" }` and asserts the constructor stored the string in `Messages`. Zero canvas reading or error extraction logic is executed.
- **Remediation**: Add a component with error messages to `bridge.ActiveCanvas` and query `bridge.GetContext()` or canvas extraction to verify that component error messages are extracted.

---

## 5-Component Handoff Report

### 1. Observation
- `dotnet build HPGrasshopper/HPGrasshopper.slnx -c Release`: Built cleanly with 0 errors, 1 warning (ILRepack transient lock retry succeeded).
- `dotnet test HPGrasshopper/HPGrasshopper.E2E.Tests -c Release`: 181 passed in 8s 927ms, 0 failed, 0 skipped.
- `powershell -ExecutionPolicy Bypass -File .\run-e2e-grasshopper-tests.ps1`: 181 passed in 8s 510ms, 0 failed, 0 skipped, exactly 0 modal dialog popups.
- `dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests -c Release`: 110 passed in 4s 380ms.
- `dotnet test HPGrasshopper/HPGrasshopper.Mcp.Server.Tests -c Release`: 77 passed in 4s 992ms.
- AST Clean Code audit (`python scratch/verify_clean_code_ast.py "HPGrasshopper/HPGrasshopper.E2E.Tests" 40`): 0 violations of Rule C2 (all 31 files <= 300 lines); 0 violations of Rule M3 (all methods <= 40 lines).
- Rule Q-B1 (dead/commented-out code): 0 violations.
- Rule Q-B2 (unannotated empty catch blocks): 0 violations.
- In-depth static audit of all 20 Tier test files uncovered **seven (7) residual self-certifying pseudo-tests** that bypass target deliverable functionality:
  1. `Tier1_NamedPipeAndWpfTests.cs:139-148` (`TC_T1_WPF_04`): local ternary assignment and assertion.
  2. `Tier2_NamedPipeAndWpfBoundariesTests.cs:153-160` (`TC_T2_WPF_04`): local ternary truncation and assertion.
  3. `Tier2_NamedPipeAndWpfBoundariesTests.cs:163-184` (`TC_T2_WPF_05`): dummy local static function `TrySafeCopy`.
  4. `Tier2_RegistryAndSeedBoundariesTests.cs:134-147` (`TC_T2_SED_03`): dummy local static function `ValidateDimensions`.
  5. `Tier2_EditAndExportBoundariesTests.cs:114-123` (`TC_T2_IMG_05`): BCL `Path` and `File.OpenWrite` negative test.
  6. `Tier1_EditAndExportToolsTests.cs:106-121` (`TC_T1_IMG_03`): local BCL `Math.Clamp` and `Math.Round` without calling export tool.
  7. `Tier1_CanvasAndPlayerToolsTests.cs:71-81` (`TC_T1_RDC_05`): DTO constructor assertion without canvas extraction.

### 2. Logic Chain
1. Under the Integrity Forensics Protocol, the auditor must verify that the work product implements its functionality authentically without shortcuts or pseudo-tests.
2. The specific task instructions from the orchestrator mandated: *"confirm 100% elimination of all pseudo-tests (no local ternary tests, no local string arrays, no local dictionaries, no dummy local static functions, no BCL math checks)."*
3. The forensic audit confirmed that while the tests previously flagged by Reviewer 1 were remediated, seven (7) residual pseudo-tests remain in Tiers 1 and 2.
4. Under the rule: *"If ANY check fails, your verdict is INTEGRITY VIOLATION and you MUST reject the work product. Trust NOTHING — verify EVERYTHING."*
5. The presence of these 7 residual pseudo-tests directly violates the integrity mandate. Therefore, the work product cannot be certified as clean.

### 3. Caveats
- No caveats regarding compilation, build configuration, or test pass rates: all 181 E2E tests, 110 bridge tests, and 77 server tests execute and pass cleanly.
- Zero modal popups or process deadlocks were observed.
- Clean Code rules (C2 <= 300 lines, M3 <= 50 lines, Q-B1 zero dead code, Q-B2 zero empty catches) are fully compliant.
- The rejection is strictly based on the failure to achieve 100% elimination of self-certifying pseudo-tests.

### 4. Conclusion
**Verdict**: **INTEGRITY VIOLATION**  
The Milestone M5 Iteration 3 delivery is **REJECTED**. The 7 identified residual pseudo-tests must be remediated to connect to genuine production classes (`McpBridgeStatusViewModel`, `ScriptGuard`, `SeedInstaller`, `GrasshopperE2EHarness.ExportCanvasImage`, canvas context) before Milestone M5 can be approved.

### 5. Verification Method
To independently reproduce this forensic audit:
1. Run line count and AST verification:
   ```powershell
   python scratch/verify_clean_code_ast.py "HPGrasshopper/HPGrasshopper.E2E.Tests" 40
   ```
2. Build and run tests:
   ```powershell
   dotnet build HPGrasshopper/HPGrasshopper.slnx -c Release
   dotnet test HPGrasshopper/HPGrasshopper.E2E.Tests -c Release
   powershell -ExecutionPolicy Bypass -File .\run-e2e-grasshopper-tests.ps1
   ```
3. Inspect the 7 flagged pseudo-test methods:
   - `HPGrasshopper/HPGrasshopper.E2E.Tests/Tiers/Tier1_NamedPipeAndWpfTests.cs:139-148` (`TC_T1_WPF_04`)
   - `HPGrasshopper/HPGrasshopper.E2E.Tests/Tiers/Tier2_NamedPipeAndWpfBoundariesTests.cs:153-160` (`TC_T2_WPF_04`)
   - `HPGrasshopper/HPGrasshopper.E2E.Tests/Tiers/Tier2_NamedPipeAndWpfBoundariesTests.cs:163-184` (`TC_T2_WPF_05`)
   - `HPGrasshopper/HPGrasshopper.E2E.Tests/Tiers/Tier2_RegistryAndSeedBoundariesTests.cs:134-147` (`TC_T2_SED_03`)
   - `HPGrasshopper/HPGrasshopper.E2E.Tests/Tiers/Tier2_EditAndExportBoundariesTests.cs:114-123` (`TC_T2_IMG_05`)
   - `HPGrasshopper/HPGrasshopper.E2E.Tests/Tiers/Tier1_EditAndExportToolsTests.cs:106-121` (`TC_T1_IMG_03`)
   - `HPGrasshopper/HPGrasshopper.E2E.Tests/Tiers/Tier1_CanvasAndPlayerToolsTests.cs:71-81` (`TC_T1_RDC_05`)
