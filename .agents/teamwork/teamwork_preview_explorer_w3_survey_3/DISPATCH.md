## 2026-10-09T15:00:37Z
You are Survey Explorer 3 for HPBim Wave 3 Remediation.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_w3_survey_3
Your parent conversation ID is: 8b5286ec-5528-473c-b9fe-c3867bdf1bf8

MANDATORY FIRST STEP: Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (specifically under header ## 2026-10-09T14:54:56Z).
Constraint: You are READ-ONLY. Do NOT modify source code or run destructive commands.

Investigate R5 and Test Suites / Governance in target codebase: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim
1. R5: Standardized Geometric Measurement & Acceptance Verification
   - Locate geometric measurement and verification services/classes in HPBim.
   - Trace how elements are queried by mapped TargetUniqueId.
   - Trace verification of Pile elements: find where cutoff elevation check is missing and how to implement it (returning Inconclusive if cutoff elevation is missing).
   - Trace verification of Profile/Slab elements: outline and thickness checks.
   - Trace category-level verification: find where Generic category is auto-verified without actual measurement and how to eliminate this flaw.
2. Test Suites and Repository Governance
   - Enumerate all test projects in HPBim (e.g. HPBim.Core.Tests, HPBim.Tests, etc.).
   - Identify existing tests covering R1..R5.
   - Check test runners, build commands, and overall test count across repository.
   - Verify Clean Code constraints: file lines <= 300 (C2), method lines <= 50 (M3), zero comments (Q-B1), zero empty catch (Q-B2), HPBim.Core pure netstandard2.0 without Autodesk.Revit.*.

Deliverables:
- Write survey_report.md and handoff.md in your working directory.
- Send a completion message via send_message to parent (8b5286ec-5528-473c-b9fe-c3867bdf1bf8) summarizing your findings.
