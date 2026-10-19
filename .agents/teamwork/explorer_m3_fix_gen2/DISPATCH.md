# Dispatch Instructions: explorer_m3_fix_gen2

## Identity
- Role: Technical Investigator & Fix Strategy Architect
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_fix_gen2
- Target Milestone: Milestone 3 Iteration 2 (Remediation of Reviewer 2 Findings)

## Context & Inputs
1. Reviewer 2 handoff report:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_2_gen2\handoff.md`
2. Original Request:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
3. Current source files:
   - `HPBim/src/HPBim.Client/ScriptGenerators/CompositeBimGenerator.cs`
   - `HPBim/src/HPBim.Client/Connection/RevitPipeClient.cs`
   - `HPBim/src/HPBim.Client/Verification/RevitModelVerifier.cs`
   - `HPBim/src/HPBim.Client/TypeResolver/TypeResolutionRules.cs`

## Issues to Solve
1. **`CompositeBimGenerator.cs` (Critical)**:
   - Must incorporate Stage 7 (MEP) and Stage 8 (Rebars) into the generated C# script inside `TransactionGroup` so atomic dry-run genuinely validates all 8 stages.
   - Design a modular split (e.g. `CompositeBimGenerator.cs` + partial class or script sections) so that EVERY file remains strictly <= 250 lines (well under the 300 line limit).
2. **`RevitPipeClient.cs` Line Limit & Concurrency (Major)**:
   - Current file is 306 lines (> 300 line limit).
   - `ConnectAsync` lacks a synchronization lock (`SemaphoreSlim _connectLock` or equivalent).
   - Design extraction of helper class (e.g., `PipeMessageReader` or `PipeStreamHandler`) to reduce `RevitPipeClient.cs` to < 220 lines.
3. **`CompareCounts` in `RevitModelVerifier.cs` (Major)**:
   - Method is 58 lines (> 50 line limit).
   - Design extraction into table-driven mapping or helper methods so every method is <= 35 lines.
4. **Dead code in `TypeResolutionRules.cs` (Minor)**:
   - `GetDoorSymbolName` and `GetWindowSymbolName` are unused.
   - Determine whether to integrate them into `Stage5OpeningGenerator` or remove them to eliminate dead code.
5. **Fidelity improvements**:
   - Align column top level parameter and opening symbol selection between individual stages and composite generator.

## Deliverables
- Write `handoff.md` with:
  1. Complete code design and file partitioning for the fixes.
  2. Exact class line counts ensuring all <= 250 lines and all methods <= 40 lines.
  3. Step-by-step instructions for `worker_m3_fix_gen2`.

## 2026-10-06T02:31:45Z
[Message] sender=18c3ad8c-1fc9-42cc-83e7-40ab9000587e
Design the complete fix strategy for Milestone 3:
1. CompositeBimGenerator: integrate Stage 7 (MEP) and Stage 8 (Rebars) into script while keeping file/classes <= 250 lines (using partial classes or helper script templates).
2. RevitPipeClient: extract helper (e.g. PipeMessageReader) to reduce RevitPipeClient <= 220 lines, add SemaphoreSlim synchronization to ConnectAsync.
3. CompareCounts in RevitModelVerifier: refactor to <= 35 lines.
4. TypeResolutionRules: eliminate dead code.
5. Column and opening fidelity alignment.
