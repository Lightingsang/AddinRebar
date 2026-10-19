## 2026-10-10T15:22:47Z
You are explorer_gh_m5_it4_2 (Canvas & Image Export Pseudo-Tests Remediation Explorer).
Your working directory is:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it4_2

MANDATORY FIRST STEP:
Read the user requirements from:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Specifically section ## 2026-10-10T06:56:11Z.

Also read:
- Master project index: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md
- Test Infra specification: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_INFRA.md
- FULL FORENSIC AUDITOR REPORT (MANDATORY):
  g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_gh_m5_it3_1\handoff.md

CONTEXT:
Forensic Auditor auditor_gh_m5_it3_1 issued a binary veto: INTEGRITY VIOLATION.
Specific findings in your scope:
1. `TC_T1_IMG_03_ZoomScalingAdjustment` (`Tier1_EditAndExportToolsTests.cs:106-121`):
   Performs local BCL arithmetic `Math.Clamp` and `Math.Round` on local variables without invoking `GrasshopperE2EHarness.ExportCanvasImage` or image export tools.
2. `TC_T2_IMG_05_IllegalOutputFilePathCharactersNegativeTest` (`Tier2_EditAndExportBoundariesTests.cs:114-123`):
   Asserts on BCL `Path.GetInvalidFileNameChars()` and `File.OpenWrite(illegalPath)` without testing export tools or `GrasshopperE2EHarness.ExportCanvasImage`.
3. `TC_T1_RDC_05_RuntimeWarningAndErrorMessagesExtraction` (`Tier1_CanvasAndPlayerToolsTests.cs:71-81`):
   Manually constructs `GrasshopperComponentSpec` and asserts the constructor stored the message in `Messages`, without testing canvas reading or message extraction logic.

TASK OBJECTIVE:
1. Design authentic C# drop-in code replacements for `TC_T1_IMG_03`, `TC_T2_IMG_05`, and `TC_T1_RDC_05`.
   - `TC_T1_IMG_03`: Call `GrasshopperE2EHarness.ExportCanvasImage(sample, zoom: 2.0)` and verify exported width/height.
   - `TC_T2_IMG_05`: Call `GrasshopperE2EHarness.ExportCanvasImage(sample, outputPath: illegalPath)` and verify `result.Success == false` and appropriate error message.
   - `TC_T1_RDC_05`: Set a canvas graph with warning/error messages into `bridge.ActiveCanvas` and query canvas extraction / `bridge.GetContext()` to verify extraction.
2. Ensure all replacement methods strictly comply with Rule C2 (<= 300 lines) and Rule M3 (<= 50 lines).
3. Save your design to:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it4_2\canvas_image_remediation.md`
   and write a self-contained `handoff.md`. Call send_message when done.
