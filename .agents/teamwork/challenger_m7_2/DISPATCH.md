# TIER 5 DUAL-HOST CHALLENGER DISPATCH — Gate M7 (challenger_m7_2)

## Mission
Perform Tier 5 Dual-Host Cross-Platform Adversarial Stress Testing on AutoCAD 2026 and Revit 2026 interop boundaries in HPKata.

## Authoritative Inputs
- **Original User Request**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (read first!)
- **Master Blueprint**: `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`
- **E2E Infrastructure**: `TEST_INFRA.md` & `TEST_READY.md`
- **Clean Code Standards**: `docs/clean-code/HP_CLEAN_CODE_CORE.md`

## Mandatory Resource Directive
- The solution has already been compiled in Release mode.
- If executing tests, you MUST use `--no-build --disable-build-servers -m:1`.
- MTP Concurrency Isolation: You MUST pass `--results-directory "$env:TEMP\mtp-chal72-$([guid]::NewGuid())"` to prevent test results locking collisions!

## Challenge Tasks
1. **CAD & Revit Interop Stress**:
   - Stress-test `E2ECadContext` and `E2ERevitContext` under rapid repeated transactions.
   - Stress-test `RevitUnitConverter`: check sub-millimeter precision roundtrip (1e-6 mm tolerance).
   - Stress-test `XDataService` and `HandleLinkService`: check handle link re-binding on deep cloning.
   - Stress-test `RevitShapeAdapter`: verify asymmetric vs symmetric U-bar threshold ($|A - C| < 1.0\text{ mm}$).
2. **Execute E2E & Full Solution Test Suite**:
   - Run: `dotnet test HPKata/HPKata.E2E.Tests/HPKata.E2E.Tests.csproj -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-chal72-$([guid]::NewGuid())"`.
   - Run: `dotnet test HPKata/HPKata.slnx -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-chal72all-$([guid]::NewGuid())"`.
   - Verify all 437 tests pass 100% green.
3. **Deliverable**:

## 2026-10-17T06:04:33Z
From: orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6)
Content:
You are challenger_m7_2, the Tier 5 Dual-Host Cross-Platform Stress Challenger for Milestone M7 of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m7_2\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md.
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m7_2\DISPATCH.md.
3. Read TEST_INFRA.md and TEST_READY.md.
4. Read docs/clean-code/HP_CLEAN_CODE_CORE.md.

RESOURCE DIRECTIVE (MANDATORY):
- Solution is already built in Release mode.
- If executing tests, you MUST use `--no-build --disable-build-servers -m:1`.
- MTP Concurrency Isolation: You MUST pass `--results-directory "$env:TEMP\mtp-chal72-$([guid]::NewGuid())"` to prevent test results locking collisions!

YOUR TASK:
Perform Tier 5 Dual-Host Cross-Platform Stress Testing:
1. Stress-test CAD XData <-> Revit parameter mapping and round-trip data fidelity.
2. Stress-test Unit Quarantine (Rule R5): check sub-millimeter precision.
3. Run E2E test suite: `dotnet test HPKata/HPKata.E2E.Tests/HPKata.E2E.Tests.csproj -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-chal72-$([guid]::NewGuid())"`.
4. Run full solution test suite: `dotnet test HPKata/HPKata.slnx -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-chal72all-$([guid]::NewGuid())"`.
5. Verify all 437 tests pass 100% green.
6. Deliver `challenge_report.md` and `handoff.md` with explicit verdict: **APPROVE** or **REQUEST_CHANGES**.
7. Send message to orchestrator_16 when complete.

