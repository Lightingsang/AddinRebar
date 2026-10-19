# RE-REVIEWER DISPATCH SPECIFICATION — Gate M6 Iteration 11 (reviewer_m6_2_iter2)

## Mission
Perform an objective re-review of the remediated `RevitParameterBridge.cs`, the relocated `MockRevitDocumentContext.cs`, and the headless test suite.

## Authoritative Inputs
- **Original User Request**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (read first!)
- **Remediation Worker Handoff**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m6_iter2\handoff.md`
- **Previous Review Handoff**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m6_2\handoff.md`
- **Clean Code Standards**: `docs/clean-code/HP_CLEAN_CODE_CORE.md` and `docs/clean-code/REVITADDINAI_CLEAN_CODE_STANDARD.md`

## Mandatory Resource Directive
- The worker has already built the solution in Release mode.
- If executing tests, you MUST use `--no-build --disable-build-servers -m:1`.
- MTP Concurrency Isolation: You MUST pass `--results-directory "$env:TEMP\mtp-rev62iter2-$([guid]::NewGuid())"` to prevent test results locking collisions!

## Review Tasks
1. **Parameter Bridge Inspection**:
   - Inspect `HPKata/HPKata.Revit/Bridge/RevitParameterBridge.cs`.
   - Verify `ExtractFromRevitRebar` body length $\le 30$ lines, and `ResolveShapeAndParameters` cleanly decouples shape family lookup from curve fallback.
   - Verify that single-bar length `REBAR_ELEM_LENGTH` is still mapped to `CuttingLengthMm` and `rebar.TotalLength` is NEVER used as single-bar length.
2. **Mock Relocation**:
   - Verify that `MockRevitDocumentContext.cs` is now in `HPKata.Revit.Tests/Mocks/` and that `HPKata.Revit` contains zero test mock classes.
3. **Clean Code Audit**:
   - Verify Rule C2 (all files $\le 300$ lines), Rule M3 (all methods $\le 50$ lines), Rule Q-B1 (0 dead code), Rule Q-B2 (0 unannotated catch).
4. **Test Suite Verification**:
   - Run: `dotnet test HPKata/HPKata.Revit.Tests/HPKata.Revit.Tests.csproj -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-rev62iter2-$([guid]::NewGuid())"`.
   - Verify all 54 tests pass 100% green.
5. **Deliverable**:
   - Deliver `review.md` and `handoff.md` in `.agents/teamwork/reviewer_m6_2_iter2/` with an explicit verdict: **APPROVE** or **REQUEST_CHANGES**.


## 2026-10-17T05:23:51Z
You are reviewer_m6_2_iter2, the Revit Interop & Clean Code Re-Reviewer for Gate M6 Iteration 11 of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m6_2_iter2\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md.
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m6_2_iter2\DISPATCH.md.
3. Read your previous review report at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m6_2\handoff.md.
4. Read the remediation worker handoff at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m6_iter2\handoff.md.
5. Read docs/clean-code/HP_CLEAN_CODE_CORE.md.

RESOURCE DIRECTIVE (MANDATORY):
- Worker has already built the solution in Release mode.
- If executing tests, you MUST use `--no-build --disable-build-servers -m:1`.
- MTP Concurrency Isolation: You MUST pass `--results-directory "$env:TEMP\mtp-rev62iter2-$([guid]::NewGuid())"` to prevent log locking collisions!

YOUR TASK:
Re-review architecture and remediated defects:
1. Re-verify Parameter Bridge: Inspect `RevitParameterBridge.cs`. Verify `ExtractFromRevitRebar` body length <= 30 lines, `ResolveShapeAndParameters` cleanly decouples logic, and single-bar length `REBAR_ELEM_LENGTH` is preserved.
2. Re-verify Mock Relocation: Verify `MockRevitDocumentContext.cs` is now in `HPKata.Revit.Tests/Mocks/` and production assembly `HPKata.Revit` contains 0 mocks.
3. Check Clean Code compliance: Rule C2 (<= 300 lines), Rule M3 (<= 50 lines), Rule Q-B1, Rule Q-B2 across all files.
4. Run: `dotnet test HPKata/HPKata.Revit.Tests/HPKata.Revit.Tests.csproj -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-rev62iter2-$([guid]::NewGuid())"`.
5. Record findings in `review.md` and deliver `handoff.md` with explicit verdict: **APPROVE** or **REQUEST_CHANGES**.
6. Send message to orchestrator_16 when complete.
