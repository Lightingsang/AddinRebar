## 2026-10-10T15:22:47Z
You are explorer_gh_m5_it4_1 (WPF & Clipboard Pseudo-Tests Remediation Explorer).
Your working directory is:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it4_1

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
1. `TC_T1_WPF_04_ScriptPreviewAndCopyCommand` (`Tier1_NamedPipeAndWpfTests.cs:139-148`):
   Evaluates an inline local ternary `scriptCode.Length > 200 ? scriptCode.Substring(0, 200) + "..." : scriptCode` instead of exercising `McpBridgeStatusViewModel.LastScriptPreview` or `CopyLastScriptCommand`.
2. `TC_T2_WPF_04_ExtremelyLongLastScriptPreviewTruncation` (`Tier2_NamedPipeAndWpfBoundariesTests.cs:153-160`):
   Evaluates an inline local ternary on a 500,000 char string rather than testing view model truncation.
3. `TC_T2_WPF_05_ClipboardExceptionResilience` (`Tier2_NamedPipeAndWpfBoundariesTests.cs:163-184`):
   Defines a dummy local static function `TrySafeCopy` inside the test body and tests its own local function instead of testing production clipboard handling.

TASK OBJECTIVE:
1. Design authentic C# drop-in code replacements for `TC_T1_WPF_04`, `TC_T2_WPF_04`, and `TC_T2_WPF_05`.
   - Exercise genuine `McpBridgeStatusViewModel` (from `HPRebar.McpBridge.Core.UI` / `HPGrasshopper.McpBridge.UI`), `CopyLastScriptCommand`, `LastScriptPreview` truncation properties, and clipboard error handling callbacks.
   - If UI dependencies require mock runners/delegates, use canonical constructors or callbacks from `HPRebar.McpBridge.Core`.
2. Ensure all replacement methods strictly comply with Rule C2 (<= 300 lines) and Rule M3 (<= 50 lines).
3. Save your design to:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it4_1\wpf_clipboard_remediation.md`
   and write a self-contained `handoff.md`. Call send_message when done.
