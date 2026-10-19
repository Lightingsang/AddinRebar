# Task Dispatch: worker_m5_integration

## Identity
- **Agent Name**: worker_m5_integration
- **Role**: Milestone M5 Full Solution Integration & Quality Auditor
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5_integration\`
- **Parent Conversation ID**: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

## MANDATORY INTEGRITY WARNING
> DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Authoritative Context Documents (MANDATORY TO READ FIRST)
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Headers `## 2026-10-16T23:55:10Z` and `## 2026-10-17T06:46:41Z`)
2. Master Project Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`
3. Gate Status History: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\GATE_STATUS.md`

## Your Mission: Solution-Wide Integration & Clean Code Audit
Perform a comprehensive, solution-wide integration build, static analysis, and verification across ALL projects in `HPKata/HPKata.slnx`:

1. **Full Solution Clean Build**:
   - Run: `dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1`
   - Must build with **0 Warning(s) and 0 Error(s)** across all projects (`HPKata.Core`, `HPKata.UI`, `HPKata.AutoCad`, `HPKata.Revit`, `HPKata.Core.Tests`, `HPKata.UI.Tests`, `HPKata.AutoCad.Tests`, `HPKata.Revit.Tests`, `HPKata.E2E.Tests`).

2. **Full Solution Test Suite Execution**:
   - Run: `dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"`
   - Must pass **100% green** across all 5 test projects ($\ge 900+$ tests, 0 failed, 0 skipped).

3. **Solution-Wide Clean Code Governance Audit**:
   - **Rule C2 (File Length <= 300 physical lines)**:
     Scan every `.cs` file in `HPKata/`. Verify 100% of files are $\le 300$ physical lines. If any file exceeds 300 lines, refactor by extracting helper classes or partial classes.
   - **Rule M3 (Method Length <= 50 physical lines)**:
     Scan every method in `HPKata/`. Verify 100% of methods are $\le 50$ physical lines. If any method exceeds 50 lines, refactor by extracting helper methods.
   - **Rule Q-B1 (Zero Dead Code / Unaddressed Tasks)**:
     Scan for commented-out code, TODO, FIXME, HACK, and `NotImplementedException`. Must be 0 occurrences.
   - **Rule Q-B2 (Annotated Catch Blocks)**:
     Scan all `catch` blocks. Verify every catch block contains an explicit annotation comment explaining why it is safe or logging the error.
   - **Rule R5 (Revit Unit Constant Isolation)**:
     Verify the constant `304.8` appears strictly and exclusively in `RevitUnitConverter.cs` (zero occurrences elsewhere in production code).
   - **Host Neutrality (D1, D4)**:
     Verify `HPKata.Core` has ZERO references to `Autodesk.AutoCAD.*` or `Autodesk.Revit.*`.

4. **Integration Deliverables Table**:
   Produce a comprehensive summary table covering all 5 Milestones (M1 Beam, M2 Column & Wall, M3 Foundation/Slab/Stair, M4 QS & Layout, M5 Full Solution Integration) documenting:
   - Module count, file count, total test count.
   - AutoCAD command list (all 52+ commands verified).
   - Revit service list (all generators verified).
   - Excel export sheets (`Dam`, `Cot`, `Vach`, `Mong`, `San`, `Thang`, `Kata_QS`, `Thong_ke_QS`).

## Deliverable
Write your full integration report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5_integration\integration_report.md`
and a structured `handoff.md` in your working directory.
When complete, notify parent via `send_message` with build/test results and report path.


## 2026-10-17T13:12:48Z
You are worker_m5_integration, the Full Solution Integration & Quality Auditor for Milestone M5.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5_integration\
Your parent conversation ID is: a7a2dbbb-6b7d-4279-98cd-1216d8280d80

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

MANDATORY FIRST STEPS:
1. Read the authoritative original request at:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under headers ## 2026-10-16T23:55:10Z and ## 2026-10-17T06:46:41Z)
2. Read the master project scope:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md
3. Read the Gate Status history:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\GATE_STATUS.md
4. Read your task dispatch:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5_integration\DISPATCH.md

YOUR MISSION:
Perform a comprehensive, solution-wide integration build, static analysis, and verification across ALL projects in HPKata/HPKata.slnx:
1. Full Solution Clean Build:
   - Run: dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1 (must pass with 0 errors, 0 warnings).
2. Full Solution Test Suite Execution:
   - Run: dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())" (must pass 100% green, >= 900+ tests).
3. Solution-Wide Clean Code Audit:
   - Rule C2: Measure every C# file in HPKata/ (must be <= 300 physical lines).
   - Rule M3: Measure every method in HPKata/ (must be <= 50 physical lines).
   - Rule Q-B1: Zero dead/commented-out code, zero TODOs/FIXMEs/HACKs/NotImplementedException.
   - Rule Q-B2: Zero unannotated empty catch blocks.
   - Rule R5: Constant 304.8 strictly and exclusively in RevitUnitConverter.cs.
   - Host Neutrality: HPKata.Core has ZERO references to Autodesk.*.
4. Comprehensive Feature Matrix:
   - Document all completed features across Milestones M1 to M5 in an executive summary table.

DELIVERABLE:
Write your integration report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5_integration\integration_report.md`
and a structured handoff.md in your working directory.
When complete, notify parent via send_message with build/test results and report path.
