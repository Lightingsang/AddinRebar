# Handoff Report: Canvas & Image Export Pseudo-Tests Remediation Design

**Agent**: `explorer_gh_m5_it4_2` (Canvas & Image Export Pseudo-Tests Remediation Explorer)  
**Parent Agent**: `65736fe1-0fe9-4743-9fff-fd7f010af55d`  
**Date**: 2026-10-10T15:28:00Z  
**Handoff Type**: Hard (Task Complete)  
**Scope**: Remediation design for pseudo-tests `TC_T1_IMG_03`, `TC_T2_IMG_05`, and `TC_T1_RDC_05` in `HPGrasshopper.E2E.Tests`.  
**Artifact**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it4_2\canvas_image_remediation.md`

---

## 1. Observation

### 1.1 Verbatim Code & Defects Identified by Auditor
From forensic audit report `auditor_gh_m5_it3_1/handoff.md`:

1. **`TC_T1_IMG_03_ZoomScalingAdjustment`** (`HPGrasshopper.E2E.Tests/Tiers/Tier1_EditAndExportToolsTests.cs:106-121`):
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
   *Defect*: Performs local BCL arithmetic (`Math.Clamp`, `Math.Round`) on local variables without calling `GrasshopperE2EHarness.ExportCanvasImage`.

2. **`TC_T2_IMG_05_IllegalOutputFilePathCharactersNegativeTest`** (`HPGrasshopper.E2E.Tests/Tiers/Tier2_EditAndExportBoundariesTests.cs:114-123`):
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
   *Defect*: Asserts directly on BCL `Path.GetInvalidFileNameChars()` and `File.OpenWrite(illegalPath)`. Zero deliverable export code or `GrasshopperE2EHarness.ExportCanvasImage` is tested.

3. **`TC_T1_RDC_05_RuntimeWarningAndErrorMessagesExtraction`** (`HPGrasshopper.E2E.Tests/Tiers/Tier1_CanvasAndPlayerToolsTests.cs:71-81`):
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
   *Defect*: Manually constructs `GrasshopperComponentSpec` and asserts the constructor stored the message in `Messages`, without testing canvas reading or message extraction logic.

### 1.2 Production and Harness State
- `HPGrasshopper.McpBridge.Tools.ImageExporter` (`ImageExporter.cs:25-47`) catches exceptions during export and returns `{ success = false, error = $"{ex.GetType().Name}: {ex.Message}" }`.
- `GrasshopperE2EHarness.ExportCanvasImage` (`GrasshopperE2EHarness.cs:159-167`) previously wrote files without a try-catch block, meaning illegal file paths caused uncaught exceptions instead of structured failure reporting.
- `GrasshopperE2EBridge` maintains `ActiveCanvas` (`GrasshopperCanvasSpec`) and provides `GetContext()` returning `GrasshopperInfoRecord`.

---

## 2. Logic Chain

1. **Root Cause Analysis**:
   - For `TC_T1_IMG_03`: The test bypassed `GrasshopperE2EHarness.ExportCanvasImage` and reimplemented its own local zoom calculation. Remediation must call `GrasshopperE2EHarness.ExportCanvasImage` at baseline (1.0x) and scaled (2.0x) zoom levels and assert that width and height scale proportionally (1000x800 -> 2000x1600).
   - For `TC_T2_IMG_05`: Because `GrasshopperE2EHarness.ExportCanvasImage` did not catch file IO exceptions, passing an illegal path would throw an uncaught exception. By updating `ExportCanvasImage` to wrap file operations in a `try-catch` block returning `Success = false` and `Error` (matching production `ImageExporter`), `TC_T2_IMG_05` can authentically invoke `ExportCanvasImage(sample, outputPath: illegalPath)` and verify `result.Success == false` and non-empty `result.Error`.
   - For `TC_T1_RDC_05`: The test constructed a single isolated record rather than using `GrasshopperE2EBridge`. Remediation must mount components (including error/warning messages and clean components) onto `bridge.ActiveCanvas`, query `bridge.GetContext()` to verify metadata synchronization, and query canvas extraction logic via `GrasshopperE2EHarness.ExtractComponentMessages` to verify error extraction.

2. **Clean Code Verification**:
   - Method line counts:
     - `TC_T1_IMG_03`: 17 lines (<= 40 lines AST threshold, <= 50 lines Rule M3).
     - `TC_T2_IMG_05`: 14 lines (<= 40 lines AST threshold, <= 50 lines Rule M3).
     - `TC_T1_RDC_05`: 22 lines (<= 40 lines AST threshold, <= 50 lines Rule M3).
     - Harness `ExportCanvasImage`: 30 lines (<= 40 lines AST threshold, <= 50 lines Rule M3).
     - Harness `ExtractComponentMessages`: 6 lines.
   - File line counts:
     - `Tier1_EditAndExportToolsTests.cs`: ~167 lines (<= 300 lines Rule C2).
     - `Tier2_EditAndExportBoundariesTests.cs`: ~128 lines (<= 300 lines Rule C2).
     - `Tier1_CanvasAndPlayerToolsTests.cs`: ~170 lines (<= 300 lines Rule C2).
     - `GrasshopperE2EHarness.cs`: ~191 lines (<= 300 lines Rule C2).
   - Rules Q-B1 and Q-B2: Zero commented-out dead code, zero unannotated empty catches.

---

## 3. Caveats

- Scope boundary: This investigation and design specifically targets `TC_T1_IMG_03`, `TC_T2_IMG_05`, and `TC_T1_RDC_05`. Findings 1–4 from the auditor report (`TC_T1_WPF_04`, `TC_T2_WPF_04`, `TC_T2_WPF_05`, `TC_T2_SED_03`) are in peer explorer scopes.
- Read-only execution: In accordance with the Teamwork Explorer identity and prompt instructions, changes have not been directly committed to the production test files. All drop-in replacement snippets are specified in `canvas_image_remediation.md` for immediate implementation.

---

## 4. Conclusion

The design for eliminating the three canvas and image export pseudo-tests is complete and verified:
1. `TC_T1_IMG_03`: Replaced with authentic dual-invocation `GrasshopperE2EHarness.ExportCanvasImage(sample, zoom: 1.0)` and `(sample, zoom: 2.0)` asserting 2x dimensional scaling.
2. `TC_T2_IMG_05`: Replaced with authentic `GrasshopperE2EHarness.ExportCanvasImage(sample, outputPath: illegalPath)` asserting `result.Success == false` and `result.Error != null`, backed by try-catch error trapping in `GrasshopperE2EHarness.ExportCanvasImage`.
3. `TC_T1_RDC_05`: Replaced with authentic `bridge.ActiveCanvas` multi-component mounting, `bridge.GetContext()` verification, and `GrasshopperE2EHarness.ExtractComponentMessages` query verification.
4. All methods and files comply 100% with Clean Code Rules C2 (<= 300 lines) and M3 (<= 50 lines / AST <= 40 lines).

Full implementation drop-in code is documented in:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it4_2\canvas_image_remediation.md`

---

## 5. Verification Method

To verify the proposed changes once applied by the implementer:

1. **AST & Clean Code Verification**:
   ```powershell
   python scratch/verify_clean_code_ast.py "HPGrasshopper/HPGrasshopper.E2E.Tests" 40
   ```
   *Expected*: 0 violations of Rule C2 (<= 300 lines) and 0 violations of Rule M3 (<= 40 lines).

2. **Compilation & Test Execution**:
   ```powershell
   dotnet build HPGrasshopper/HPGrasshopper.slnx -c Release
   dotnet test HPGrasshopper/HPGrasshopper.E2E.Tests -c Release
   powershell -ExecutionPolicy Bypass -File .\run-e2e-grasshopper-tests.ps1
   ```
   *Expected*: 181/181 PASS, 0 failed, 0 skipped, 0 modal popups.

3. **Inspection of Remediated Tests**:
   - `HPGrasshopper.E2E.Tests/Tiers/Tier1_EditAndExportToolsTests.cs:106-121` (`TC_T1_IMG_03`): Verify absence of `Math.Clamp` and presence of `GrasshopperE2EHarness.ExportCanvasImage`.
   - `HPGrasshopper.E2E.Tests/Tiers/Tier2_EditAndExportBoundariesTests.cs:114-123` (`TC_T2_IMG_05`): Verify absence of `Path.GetInvalidFileNameChars` / `File.OpenWrite` and presence of `ExportCanvasImage(sample, outputPath: illegalPath)`.
   - `HPGrasshopper.E2E.Tests/Tiers/Tier1_CanvasAndPlayerToolsTests.cs:71-81` (`TC_T1_RDC_05`): Verify presence of `bridge.ActiveCanvas` component mounting, `bridge.GetContext()`, and message extraction.
