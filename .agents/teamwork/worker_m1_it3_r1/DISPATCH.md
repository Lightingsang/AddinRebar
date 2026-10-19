# Dispatch: worker_m1_it3_r1 (Milestone M1 Remediation - Iteration 3)

## Mission
You are `worker_m1_it3_r1`, a `teamwork_preview_worker` agent.
Your working directory is: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_it3_r1\`
The authoritative user request is at: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`.
The master project plan is at: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.
The gate failure report is at: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_1\GATE_STATUS.md`.

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Detailed Explorer Findings & Ready-To-Apply Refactoring Blueprints:
- `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_it3_1\report.md` (Detailed method breakdown & blueprints)
- `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_it3_1\proposed_BimModelSchemaValidator.cs` (Ready replacement file $\le 292$ lines, all methods $\le 31$ lines)
- `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_it3_1\proposed_BimModelSchemaValidator.Elements.cs` (Ready replacement file $\le 283$ lines, all methods $\le 34$ lines)
- `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_it3_2\report.md` (Scope analysis on CLI files & C2 whole-tree audit)
- `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_it3_3\report.md` (Refactoring for `ExecutePlanAsync` [53 lines] and automated verification script)

## Your Exclusive Write Ownership:
- `HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.cs`
- `HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.Elements.cs`
- `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs`
- `HPBim/src/HPBim.Client/Pipeline/ExecutionPlanBuilder.cs`

## Implementation Tasks:
1. **Refactor `BimModelSchemaValidator.cs` and `BimModelSchemaValidator.Elements.cs`**:
   - Apply the verified decomposition from `explorer_m1_it3_1`:
     - `Validate`: split into `Validate` (30 lines), `TryParseJsonDocument` (31 lines), and `CheckCadAndEmpty` (19 lines).
     - `ValidateModelStructure`: split into `ValidateModelStructure` (19 lines), `ValidateLevels` (28 lines), and `ValidateElementPresence` (17 lines).
     - `ValidateRebars`: split into `ValidateRebars` (17 lines) and `CollectValidHostIds` (34 lines).
   - Ensure EVERY method in both files is strictly $\le 35$ lines, and files are $\le 300$ lines (C2).
2. **Refactor `ExecutePlanAsync` in `BimPipelineEngine.PlanExecution.cs`**:
   - Currently 53 lines (lines 18–70). Refactor into helper methods (e.g. `ValidatePlanPreconditions`, `DispatchPlanScript`) so `ExecutePlanAsync` and all sub-methods are strictly $\le 40$ lines.
3. **Check `ExecutionPlanBuilder.Build`**:
   - Ensure `Build` is comfortably $\le 40$ lines.
4. **Empirical Method Line Count Verification**:
   - Run the automated line measurement script across all touched files.
   - In your `report.md` and `handoff.md`, document the exact empirical line counts for EVERY method in touched files. DO NOT make unverified claims.
5. **Build & Test Verification**:
   - Run `dotnet build HPBim/HPBim.slnx` -> 0 warnings, 0 errors.
   - Run all test suites (`dotnet test HPBim/HPBim.slnx`) -> 100% tests pass (473/473).
6. **Deliver**:
   - Deliver report in `report.md` and completion handoff in `handoff.md`, then send message to orchestrator.


## 2026-10-07T18:34:59Z
You are worker_m1_it3_r1, a teamwork_preview_worker agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_it3_r1\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
Your detailed dispatch assignment is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_it3_r1\DISPATCH.md.
The gate status and auditor evidence is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_1\GATE_STATUS.md.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

MANDATORY EXPLORER REPORTS & STAGED FILES TO APPLY:
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_it3_1\report.md
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_it3_1\proposed_BimModelSchemaValidator.cs
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_it3_1\proposed_BimModelSchemaValidator.Elements.cs
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_it3_3\report.md (Refactoring for ExecutePlanAsync and automated method line counting script)

Your Exclusive Write Ownership:
- HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.cs
- HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.Elements.cs
- HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs
- HPBim/src/HPBim.Client/Pipeline/ExecutionPlanBuilder.cs

Implementation Tasks:
1. Apply the verified refactoring from explorer_m1_it3_1 to `HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.cs` and `BimModelSchemaValidator.Elements.cs`. Confirm that Validate is <= 30 lines, ValidateModelStructure is <= 19 lines, and EVERY method in both files is strictly <= 34 lines. Confirm both files are <= 292 lines (C2 compliant).
2. Refactor `ExecutePlanAsync` in `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs` (lines 18-70, currently 53 lines): extract helpers so ExecutePlanAsync and all sub-methods are strictly <= 35 lines.
3. Check `ExecutionPlanBuilder.Build` in `HPBim/src/HPBim.Client/Pipeline/ExecutionPlanBuilder.cs` (currently 50 lines): ensure it is comfortably <= 40 lines.
4. Run the automated PowerShell method-line counter script across all modified files. Document the exact line count of EVERY method in touched files in your report.md and handoff.md.
5. Run `dotnet build HPBim/HPBim.slnx` (0 warnings, 0 errors).
6. Run `dotnet test HPBim/HPBim.slnx` (100% tests pass, 473/473).
7. Deliver report.md and handoff.md in your working directory, then send message to orchestrator.
