# Dispatch Instructions: challenger_m3_it2_1_gen2

## Identity
- Role: Empirical Challenger & Test Suite Verifier
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m3_it2_1_gen2
- Target Milestone: Milestone 3 Iteration 2 (Remediation Challenge)

## Scope & Tasks
1. Empirically verify full test execution across both Release and Debug:
   - Run: `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release`
   - Run: `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Debug`
   - Verify 216/216 tests pass with 0 errors and 0 warnings.
2. Roslyn Script Syntax Verification on Composite Script:
   - Run `ScriptSyntaxTests.CompositeBimGenerator_ScriptSyntax_HasZeroErrors`.
   - Verify that all 8 stages in `CompositeBimGenerator` compile without errors via `CSharpSyntaxTree`.
3. Model Verifier Logic:
   - Run `RevitModelVerifierTests` and verify discrepancy detection with `ElementCounts`.
4. Deliverable:
   - Write `handoff.md` with explicit verdict: `APPROVE` or `REQUEST_CHANGES`.
   - Send completion message to orchestrator via `send_message`.


## 2026-10-06T02:54:39Z
You are challenger_m3_it2_1_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m3_it2_1_gen2
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m3_it2_1_gen2\DISPATCH.md
Read ORIGINAL_REQUEST.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md

Empirically test HPBim solution:
1. Run dotnet test HPBim.slnx under Release and Debug (verify 216/216 pass).
2. Verify Roslyn syntax parsing of composite script with all 8 stages.
3. Verify RevitModelVerifierTests pass with ElementCounts.
Write handoff.md with test counts and verdict APPROVE or REQUEST_CHANGES and report back via send_message.
