# EMPIRICAL RE-CHALLENGER DISPATCH SPECIFICATION — Gate M6 Iteration 11 (challenger_m6_2_iter2)

## Mission
Empirically stress-test the decoupled mock context, command runners, and full solution integration following the relocation of `MockRevitDocumentContext.cs`.

## Authoritative Inputs
- **Original User Request**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (read first!)
- **Remediation Worker Handoff**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m6_iter2\handoff.md`
- **Previous Challenge Handoff**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m6_2\handoff.md`
- **Clean Code Standards**: `docs/clean-code/HP_CLEAN_CODE_CORE.md`

## Mandatory Resource Directive
- The worker has already built the solution in Release mode.
- If executing tests, you MUST use `--no-build --disable-build-servers -m:1`.
- MTP Concurrency Isolation: You MUST pass `--results-directory "$env:TEMP\mtp-chal62iter2-$([guid]::NewGuid())"` to prevent test results locking collisions!

## Challenge Tasks
1. **Mock Relocation & Headless Runner Verification**:
   - Verify that `HPKata.Revit.Tests` cleanly resolves `MockRevitDocumentContext` from `HPKata.Revit.Tests.Mocks`.
   - Run: `dotnet test HPKata/HPKata.Revit.Tests/HPKata.Revit.Tests.csproj -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-chal62iter2-$([guid]::NewGuid())"`.
   - Verify all 54 tests in `HPKata.Revit.Tests` pass.
2. **Full Solution Test Suite Execution**:
   - Run: `dotnet test HPKata/HPKata.slnx -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-chal62all-$([guid]::NewGuid())"`.
   - Verify all 287 tests across all 4 test projects pass 100% green.
3. **Empirical Sanity Checks**:
   - Verify that schedule aggregation, 1D-CSP cutting stock, and ClosedXML export run with 0 memory leaks, 0 file locking bugs, and 0 crashes.
4. **Deliverable**:
   - Deliver `challenge_report.md` and `handoff.md` in `.agents/teamwork/challenger_m6_2_iter2/` with an explicit verdict: **APPROVE** or **REQUEST_CHANGES**.


## 2026-10-17T05:23:51Z
You are challenger_m6_2_iter2, the Empirical Re-Challenger for Gate M6 Iteration 11 of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m6_2_iter2\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md.
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m6_2_iter2\DISPATCH.md.
3. Read the remediation worker handoff at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m6_iter2\handoff.md.
4. Read docs/clean-code/HP_CLEAN_CODE_CORE.md.

RESOURCE DIRECTIVE (MANDATORY):
- Worker has already built the solution in Release mode.
- If executing tests, you MUST use `--no-build --disable-build-servers -m:1`.
- MTP Concurrency Isolation: You MUST pass `--results-directory "$env:TEMP\mtp-chal62iter2-$([guid]::NewGuid())"` to prevent log locking collisions!

YOUR TASK:
Empirically challenge and stress-test remediated subsystems:
1. Empirically verify headless mock context & runner execution:
   - Run: `dotnet test HPKata/HPKata.Revit.Tests/HPKata.Revit.Tests.csproj -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-chal62iter2-$([guid]::NewGuid())"`.
   - Verify all 54 tests in `HPKata.Revit.Tests` pass.
2. Run full solution test suite:
   - `dotnet test HPKata/HPKata.slnx -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-chal62all-$([guid]::NewGuid())"`.
   - Verify all 287 tests pass 100% green.
3. Record empirical evidence in `challenge_report.md` and deliver `handoff.md` with explicit verdict: **APPROVE** or **REQUEST_CHANGES**.
4. Send message to orchestrator_16 when complete.
