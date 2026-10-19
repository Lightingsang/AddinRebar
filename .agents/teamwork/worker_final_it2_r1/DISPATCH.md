# Dispatch Instructions — worker_final_it2_r1

## 2026-10-07T23:58:42Z

You are worker_final_it2_r1, a teamwork_preview_worker agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_final_it2_r1\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
Your detailed dispatch assignment is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_final_it2_r1\DISPATCH.md.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

MANDATORY REPORTS TO READ BEFORE EDITING CODE:
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_tier5\report.md (Forensic Audit Violation Report)
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_final_it2_1\report.md (Refactoring Specification & Drop-in Code)
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_final_it2_2\report.md (Exhaustive Census of All 10 Tier 5 Files)
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_final_it2_3\report.md (Authentic Attestation Blueprint & MTP Commands)

Your Exclusive Write Ownership:
- HPBim/tests/HPBim.Client.Tests/Recovery/ReconcileServiceTier5AdversarialTests.cs
- HPBim/tests/HPBim.Client.Tests/Connection/TimeoutPolicyTier5AdversarialTests.cs

Key Remediation Tasks:
1. Refactor `ReconcileServiceTier5AdversarialTests.cs`:
   - Replace lines 38–96 with the refactored method (34 physical lines) and the 5 private static helper methods (`RecordInitialIntent`, `CreateReceiptElementMap`, `AddReceipt`, `AssertLedgerTarget`, `AssertJournalStatus`) as detailed in `explorer_final_it2_1/report.md` Section 5.
   - Verify that the test method is strictly <= 35 lines, all helpers <= 10 lines, and total file <= 210 lines.
2. Update `TimeoutPolicyTier5AdversarialTests.cs:56` to make the non-throw assertion explicit:
   `var ex = Record.Exception(() => policy.Validate()); Assert.Null(ex);`.
3. Build and Test Verification:
   - `dotnet run --project scratch/RoslynSyntaxAuditor/RoslynSyntaxAuditor.csproj` -> Confirm OVERALL AUDIT VERDICT: CLEAN / 100% PASS (0 M3 violations).
   - `dotnet build HPBim/HPBim.slnx` -> 0 errors, 0 warnings.
   - `dotnet test HPBim/HPBim.slnx` -> 770/770 tests pass 100%.
   - `dotnet test McpShared/McpShared.slnx` -> 908/908 tests pass 100%.
4. Attestation Veracity:
   - Accurately report exact physical file lines and method line spans in `report.md` and `handoff.md`.

Deliver report.md and handoff.md in your working directory, then send a message back to parent.
