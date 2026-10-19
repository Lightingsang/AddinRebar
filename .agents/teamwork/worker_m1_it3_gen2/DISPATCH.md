# Worker Dispatch — Milestone 1 Iteration 3

## Identity
- Role: `teamwork_preview_worker`
- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_it3_gen2`
- Parent: Orchestrator Gen 2 (`18c3ad8c-1fc9-42cc-83e7-40ab9000587e`)

## Context & Inputs
- Original Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (MUST read first)
- Project Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10_gen2\PROJECT.md`
- Predecessor Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10\handoff.md`
- Forensic Auditor Report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\auditor_m1_it2_1_hpbim\handoff.md`

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Objective
Fix the Forensic Audit violation in `HPBim/src/HPBim.Core/Qc/QcEngine.cs`:
1. In `HPBim/src/HPBim.Core/Qc/QcEngine.cs`:
   - Change `QcOptions` definition from `public readonly record struct QcOptions(...)` to:
     ```csharp
     public sealed record QcOptions(
         bool CheckClashes = true,
         bool CheckClearances = true,
         bool CheckTcvnCompliance = true);
     ```
     (Or add parameterless constructor `public QcOptions() : this(true, true, true) { }` to struct).
   - In `QcEngine.Validate(BimModel model, QcOptions? options = null)`:
     Change `var opts = options ?? new QcOptions();` to:
     `var opts = options ?? new QcOptions(true, true, true);`
   - Ensure that calling `Validate(model)` with default arguments (`options = null`) executes all checks (`TotalChecksRun > 0`).
2. Run build: `dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release`
   Must compile with 0 warnings, 0 errors.
3. Verify clean code rules: method <= 50 lines, class <= 300 lines, 0 empty catches, 0 dead code.
4. Exclusive write ownership: `HPBim/src/HPBim.Core/Qc/QcEngine.cs`.

## Handoff
Write `handoff.md` in `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_it3_gen2\handoff.md` with:
- Observation (what was changed, exact diff)
- Build command & verification results
- Logic Chain & confirmation that `Validate(badModel)` fails
- Report completion via `send_message`.


## 2026-10-06T00:51:42Z
Received dispatch message:
Fix the Forensic Audit violation in `HPBim/src/HPBim.Core/Qc/QcEngine.cs`:
1. Change `QcOptions` in `HPBim/src/HPBim.Core/Qc/QcEngine.cs` from `public readonly record struct QcOptions(...)` to:
   ```csharp
   public sealed record QcOptions(
       bool CheckClashes = true,
       bool CheckClearances = true,
       bool CheckTcvnCompliance = true);
   ```
   (or add parameterless constructor `public QcOptions() : this(true, true, true) { }` to struct).
2. In `QcEngine.Validate(BimModel model, QcOptions? options = null)`:
   Change `var opts = options ?? new QcOptions();` to:
   `var opts = options ?? new QcOptions(true, true, true);`
3. Verify that default call `Validate(model)` executes all checks (`TotalChecksRun > 0`).
4. Run build command:
   `dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release`
   Verify 0 warnings, 0 errors.
5. Adhere to Clean Code rules: method <= 50 lines, class <= 300 lines, 0 empty catches, 0 dead code.
6. Write full handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_it3_gen2\handoff.md` and send message back to orchestrator.
