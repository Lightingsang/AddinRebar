# TIER 5 ADVERSARIAL CHALLENGER DISPATCH — Gate M7 (challenger_m7_1)

## Mission
Perform Tier 5 White-Box Adversarial Coverage Hardening on the HPKata solution, inspecting implementation source and test suites for untested code paths, combinatorial edge cases, or potential failure modes.

## Authoritative Inputs
- **Original User Request**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (read first!)
- **Master Blueprint**: `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`
- **E2E Infrastructure**: `TEST_INFRA.md` & `TEST_READY.md`
- **Test Writer Handoff**: `.agents/teamwork/test_writer_m7/handoff.md`
- **Clean Code Standards**: `docs/clean-code/HP_CLEAN_CODE_CORE.md`

## Mandatory Resource Directive
- The solution has already been compiled in Release mode.
- If executing tests, you MUST use `--no-build --disable-build-servers -m:1`.
- MTP Concurrency Isolation: You MUST pass `--results-directory "$env:TEMP\mtp-chal71-$([guid]::NewGuid())"` to prevent test results locking collisions!

## Challenge Tasks
1. **White-Box Coverage Audit**:
   - Inspect implementation source in `HPKata.Core`, `HPKata.AutoCad`, and `HPKata.Revit`.
   - Probe for unexercised paths in:
     - 1D-CSP solver heuristics under extreme scrap constraints and multi-stock inventories.
     - FilletArcCalculator bulge evaluation for nearly collinear or extreme obtuse angles.
     - RevitParameterBridge parameter extraction under corrupted or missing built-in parameters.
     - ClosedXML spreadsheet formatting under unusual cell numbers.
2. **Execute Full Test Suite**:
   - Run: `dotnet test HPKata/HPKata.slnx -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-chal71-$([guid]::NewGuid())"`.
   - Verify all 437 tests pass 100% green without flaky timeouts or memory leaks.
3. **Report Gap Analysis**:
   - If gaps or bugs are found, document them with reproducible test cases.
   - If 0 remaining gaps are discovered and coverage is saturated, certify completion.
4. **Deliverable**:
   - Deliver `challenge_report.md` and `handoff.md` with explicit verdict: **APPROVE** or **REQUEST_CHANGES**.


## 2026-10-17T06:04:33Z
You are challenger_m7_1, the Tier 5 White-Box Adversarial Stress Challenger for Milestone M7 of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m7_1\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md.
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m7_1\DISPATCH.md.
3. Read TEST_INFRA.md and TEST_READY.md.
4. Read docs/clean-code/HP_CLEAN_CODE_CORE.md.

RESOURCE DIRECTIVE (MANDATORY):
- Solution is already built in Release mode.
- If executing tests, you MUST use `--no-build --disable-build-servers -m:1`.
- MTP Concurrency Isolation: You MUST pass `--results-directory "$env:TEMP\mtp-chal71-$([guid]::NewGuid())"` to prevent test results locking collisions!

YOUR TASK:
Perform Tier 5 White-Box Adversarial Stress Testing across the entire HPKata solution:
1. Perform white-box analysis of core logic (1D cutting stock heuristics, shape calculations, XData serialization, parameter bridge).
2. Run full test suite: `dotnet test HPKata/HPKata.slnx -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-chal71-$([guid]::NewGuid())"`.
3. Verify all 437 tests pass 100% green without flakiness or memory leaks.
4. Check for unexercised paths or edge case gaps.
5. Deliver `challenge_report.md` and `handoff.md` with explicit verdict: **APPROVE** or **REQUEST_CHANGES**.
6. Send message to orchestrator_16 when complete.
