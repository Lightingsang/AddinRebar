# Dispatch: reviewer_m3_rem

## Role & Mission
You are `reviewer_m3_rem`, the Remediation Reviewer for Milestone M3 (Phase 4: Foundation, Slab, Stair & Lintel Detailing).

## Task
1. Verify that both Rule M3 violations flagged by `reviewer_m3_1` are resolved:
   - `PileCoordinateGenerator.cs`: all methods <= 50 physical lines. File <= 300 lines.
   - `StairRebarCalculatorTests.cs`: all methods <= 50 physical lines. File <= 300 lines.
2. Verify clean build:
   - `dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1` (0 warnings, 0 errors).
3. Verify test suite:
   - `dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"` (all 743 tests pass 100% green).
4. Provide explicit verdict: APPROVE or REQUEST_CHANGES.

## 2026-10-17T11:28:52Z
You are reviewer_m3_rem, reviewing Milestone M3 (Phase 4: Foundation, Slab, Stair & Lintel Detailing) Iteration 2 Remediation.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_rem\
Your parent conversation ID is: a7a2dbbb-6b7d-4279-98cd-1216d8280d80

MANDATORY FIRST STEPS:
1. Read the authoritative original request at:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under headers ## 2026-10-16T23:55:10Z and ## 2026-10-17T06:46:41Z)
2. Read the master project scope:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md
3. Read the worker's changes and handoff:
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_remediation\changes.md
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_remediation\handoff.md
4. Read your task dispatch:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_rem\DISPATCH.md

YOUR MISSION:
Review the Milestone M3 remediation focusing on Rule M3 compliance and build/test verification:
1. Clean Code Rule M3 & C2 Verification:
   - Measure `HPKata/HPKata.Core/Foundations/Calculators/PileCoordinateGenerator.cs`: confirm all methods <= 50 physical lines and total file <= 300 physical lines.
   - Measure `HPKata/HPKata.Core.Tests/Stairs/StairRebarCalculatorTests.cs`: confirm all methods <= 50 physical lines and total file <= 300 physical lines.
   - Confirm zero regressions across other Clean Code rules (C2, Q-B1, Q-B2, R5, host neutrality).
2. Build & Test Verification:
   - Run: dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1 (must pass with 0 errors, 0 warnings).
   - Run: dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())" (all 743 tests must pass 100% green).
3. Verdict:
   Provide an explicit verdict: APPROVE or REQUEST_CHANGES.

DELIVERABLE:
Write your review report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_rem\review.md`
and a concise handoff.md in your working directory.
When complete, notify parent via send_message with your verdict and report path.
