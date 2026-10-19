# Dispatch: auditor_m4

## Role & Mission
You are `auditor_m4`, the Forensic Integrity Auditor for Milestone M4 (Phase 5: Quantity Surveying (QS) & Drawing Utilities).
Your working directory is: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4\`
Your parent conversation ID is: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

## Critical Mandate: BINARY VETO
Verify that all Phase 5 implementations are authentic engineering code with zero facades, zero cheating, zero dead code, zero unannotated empty catches, and full compliance with Clean Code governance:

1. Static Integrity Analysis:
   - Grep scan `HPKata/` for hardcoded simulation strings, dummy returns, `TODO`, `FIXME`, `HACK`.
   - Inspect all Phase 5 controllers in `HPKata.AutoCad/Controllers/`: verify genuine ModelSpace interaction, dynamic table creation, real PaperSpace viewports, real DWG XRecord serialization, and real ClosedXML Excel generation.
   - Inspect Revit services (`RevitQsTakeoffService.cs`, `RevitSheetArrangementService.cs`).
2. Clean Code Rules:
   - Rule C2: Measure every C# file in `HPKata/` added or touched in Phase 5 — must be <= 300 physical lines.
   - Rule M3: Measure methods in Phase 5 code — must be <= 50 physical lines.
   - Rule Q-B1: Zero commented-out code, zero TODOs/FIXMEs/HACKs.
   - Rule Q-B2: Zero unannotated empty catch blocks.
   - Rule R5: Constant `304.8` must appear strictly and exclusively in `RevitUnitConverter.cs`.
   - Host Neutrality: `HPKata.Core` has ZERO references to `Autodesk.*`.
3. Runtime Execution Validation:
   - Run: `dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"`
   - Confirm 100% tests pass (all 862 tests) without failures or file-lock collisions.
4. Audit Verdict:
   State clearly either: **CLEAN** or **INTEGRITY VIOLATION**.

## Deliverable
Write your forensic audit report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4\audit_report.md`
and a concise `handoff.md` in your working directory.


## 2026-10-17T12:24:04Z
You are auditor_m4, the Forensic Integrity Auditor for Milestone M4 (Phase 5: Quantity Surveying (QS) & Drawing Utilities).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4\
Your parent conversation ID is: a7a2dbbb-6b7d-4279-98cd-1216d8280d80

CRITICAL AUDIT MANDATE:
You are the Forensic Integrity Auditor. Your verdict is a BINARY VETO.
If you find ANY evidence of cheating, dummy/facade implementations, hardcoded mock strings, fabricated test outputs, or circumvention of requirements, you MUST report INTEGRITY VIOLATION.

MANDATORY FIRST STEPS:
1. Read the authoritative original request at:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under headers ## 2026-10-16T23:55:10Z and ## 2026-10-17T06:46:41Z)
2. Read the master project scope:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md
3. Read the worker's changes and handoff:
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_qs_layout_utils\changes.md
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_qs_layout_utils\handoff.md
4. Read your task dispatch:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4\DISPATCH.md

YOUR AUDIT CHECKS:
1. Static Integrity Analysis:
   - Grep scan HPKata/ for hardcoded simulation strings, dummy returns, TODO, FIXME, HACK.
   - Inspect all Phase 5 controllers in HPKata.AutoCad/Controllers/: verify genuine ModelSpace interaction, dynamic table creation, real PaperSpace viewports, real DWG XRecord serialization, and real ClosedXML Excel generation.
   - Inspect Revit services (RevitQsTakeoffService.cs, RevitSheetArrangementService.cs).
2. Clean Code Rules:
   - Rule C2: Measure every C# file in HPKata/ added or touched in Phase 5 — must be <= 300 physical lines.
   - Rule M3: Measure methods in Phase 5 code — must be <= 50 physical lines.
   - Rule Q-B1: Zero commented-out code, zero TODOs/FIXMEs/HACKs.
   - Rule Q-B2: Zero unannotated empty catch blocks.
   - Rule R5: Constant 304.8 must appear strictly and exclusively in RevitUnitConverter.cs.
   - Host Neutrality: HPKata.Core has ZERO references to Autodesk.*.
3. Runtime Execution Validation:
   - Run: dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"
   - Confirm 100% tests pass (all 862 tests) without failures or file-lock collisions.
4. Audit Verdict:
   State clearly either: CLEAN or INTEGRITY VIOLATION.

DELIVERABLE:
Write your forensic audit report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4\audit_report.md`
and a concise handoff.md in your working directory.
When complete, notify parent via send_message with your verdict and report path.
