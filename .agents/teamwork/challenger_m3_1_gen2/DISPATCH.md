# Dispatch Instructions: challenger_m3_1_gen2

## Identity
- Role: Empirical Challenger & Pipe Test Verifier
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m3_1_gen2
- Target Milestone: Milestone 3 (Revit Client & Pipe Engine)

## Scope & Tasks
1. Empirically verify full test execution:
   - Run: `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release`
   - Record test counts (HPBim.Core.Tests: 155, HPBim.Client.Tests: 35, Total: 190).
   - Verify 0 failures, 0 errors, execution time < 3.0s.
2. Script Generation Verification:
   - Verify that all stage generators emit non-empty C# scripts that compile via Roslyn syntax trees without errors.
   - Verify that `CompositeBimGenerator.Generate(model, dryRun: true)` emits a valid script with `TransactionGroup` and `RollBack()`.
3. Model Verification Logic:
   - Verify that `RevitModelVerifier` compares counts correctly and detects discrepancies.
4. Deliverable:
   - Write `handoff.md` with explicit verdict: `APPROVE` or `REQUEST_CHANGES`.
   - Send completion message to orchestrator via `send_message`.


## 2026-10-06T02:16:54Z
You are challenger_m3_1_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m3_1_gen2
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m3_1_gen2\DISPATCH.md
Read ORIGINAL_REQUEST.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md

Empirically test HPBim.Client and test suite:
Run dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release
Verify 190/190 tests pass.
Verify Roslyn script syntax across all 8 stages, Composite, and verifier queries.
Write handoff.md with test counts and verdict APPROVE or REQUEST_CHANGES and report back via send_message.
