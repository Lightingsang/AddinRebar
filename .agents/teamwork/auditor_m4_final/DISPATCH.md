# Task Dispatch: auditor_m4_final

## Identity
- **Agent Name**: auditor_m4_final
- **Role**: Final Forensic Integrity Auditor for Milestone M4
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4_final\`
- **Parent Conversation ID**: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

## CRITICAL AUDIT MANDATE
You are the Forensic Integrity Auditor. Your verdict is a **BINARY VETO**.
If you find ANY evidence of cheating, dummy/facade implementations, hardcoded mock strings, fabricated test outputs, or circumvention of requirements, you MUST report **INTEGRITY VIOLATION**.

## Authoritative Context Documents (MANDATORY TO READ FIRST)
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Headers `## 2026-10-16T23:55:10Z` and `## 2026-10-17T06:46:41Z`)
2. Master Project Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`
3. Previous Audit Report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4\audit_report.md`
4. Worker Changes and Handoff:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_remediation\changes.md`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_remediation\handoff.md`

## Your Forensic Integrity Audit Checks:
1. **Static Integrity Analysis**:
   - Verify `dnoi` / `dim_noi_tiep` in `DimensionUtilityCommands.cs`: Confirm genuine delegation to `ContinueDimensionInteractive()`, with ZERO routing to `TrimDimensionWitnessLinesInteractive()`.
   - Verify `DimensionUtilityController.ContinueDimensionInteractive()`: Confirm genuine continuous dimensioning logic creating `AlignedDimension` or `RotatedDimension` entities in ModelSpace.
   - Verify `LayoutCommands.cs`: Confirm `[CommandMethod("ve_grid")]` is registered on `DrawGrid()`.
   - Verify `QsTakeoffController.cs`: Confirm hardcoded `[C1, D1, S1]` elements are completely removed. Confirm genuine entity querying from ModelSpace/selection. Confirm `FindElement` (`timqs`) performs real ModelSpace entity searching and highlighting via `CadHighlightService`.
   - Verify `SheetArrangerCalculator.cs`: Confirm vertical bounds clamping, rejection of oversized blocks, and pruning of empty phantom sheets.
   - Verify `QsTakeoffCalculator.cs`: Confirm negative dimension sanitization via `Math.Max(0.0, ...)`.
   - Verify `QsFormworkCalculator.cs`: Confirm `EvaluateContactPair` decomposition into helper methods.
2. **Clean Code Governance Limits**:
   - Rule C2: Measure every C# file in `HPKata/` added or touched in Phase 5 — must be $\le 300$ physical lines.
   - Rule M3: Measure all methods in Phase 5 code — must be $\le 50$ physical lines.
   - Rule Q-B1: Zero commented-out dead code, zero TODOs/FIXMEs/HACKs/NotImplementedException.
   - Rule Q-B2: Zero unannotated empty catch blocks.
   - Rule R5: Constant 304.8 strictly and exclusively in `RevitUnitConverter.cs`.
   - Host Neutrality: `HPKata.Core` has ZERO references to `Autodesk.*`.
3. **Runtime Execution Validation**:
   - Run: `dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"`
   - Confirm 100% tests pass (all 889 tests) without any test failures or file-lock collisions.
4. **Audit Verdict**:
   State clearly either: **CLEAN** or **INTEGRITY VIOLATION**.

## Deliverable
Write your report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4_final\audit_report.md`
and a concise `handoff.md` in your working directory.
When done, notify parent via `send_message`.
