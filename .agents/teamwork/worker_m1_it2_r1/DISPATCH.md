# Dispatch: worker_m1_it2_r1 (Milestone M1 Remediation - Iteration 2)

## Mission
You are `worker_m1_it2_r1`, a `teamwork_preview_worker` agent.
Your working directory is: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_it2_r1\`
The authoritative user request is at: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`.
The master project plan is at: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.
The gate failure report is at: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_1\GATE_STATUS.md`.

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Detailed Review & Challenger Feedback (MANDATORY TO READ):
- `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m1_it1_1\handoff.md`
- `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m1_it1_2\handoff.md`
- `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m1_it1_1\handoff.md`
- `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m1_it1_2\handoff.md`

## Your Exclusive Write Ownership:
- `HPBim/src/HPBim.Core/Validation/*`
- `HPBim/src/HPBim.Core/Execution/*`
- `HPBim/src/HPBim.Core/Coordination/CompositeSourceKey.cs`
- `HPBim/src/HPBim.Client/Pipeline/*`
- `HPBim/src/HPBim.Cli/Commands/*`
- `HPBim/tests/HPBim.Core.Tests/Validation/*`
- `HPBim/tests/HPBim.Core.Tests/Execution/*`
- `HPBim/tests/HPBim.Client.Tests/Pipeline/*`

## Required Remediations (Iteration 2):
1. **[Critical - Integrity] Remove Hardcoded Fixture Marks**:
   - In `HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.cs` (lines 379–385), delete the hardcoded strings `"M1_M2"`, `"GM1_GM2"`, `"BT1"`, `"DCT_DCN"`, etc.
   - Replace with generic host verification (check if rebar references a valid host category or existing element).
   - In `physicalCount`, include `(model.Pipes?.Count ?? 0)` and `(model.Rebars?.Count ?? 0)` so valid MEP and rebar models are not rejected as `ERR_NO_ELEMENTS`.

2. **[Critical - Wire Format Serialization Bug]**:
   - In `HPBim/src/HPBim.Core/Execution/ElementPlanItem.cs`:
     Add `[JsonConverter(typeof(JsonStringEnumConverter))]` from `System.Text.Json.Serialization` on `public CoordinationCategory Category { get; init; }`.
   - Ensure `category` serializes to string names (`"Column"`, `"Beam"`, etc.) in JSON, NOT integer numbers (`8`).
   - Fix `Verify_BridgeJson_Category_Serialization` test in `EmpiricalChallengerM1Tests.cs`.

3. **[Critical - Rule 0 Heuristic Fallback Removal]**:
   - In `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs`:
     - Delete all `.Values.FirstOrDefault()` symbol/type fallbacks (lines 212, 269, 329).
     - Under Rule 0 ("Thà để cấu kiện chưa dựng còn hơn dựng sai báo đúng"): If a symbol/type cannot be found in the document, FAIL FAST for that element: record `status = "Failed"`, `errorReason = "ERR_SYMBOL_NOT_FOUND: Symbol '{symName}' not found in document"`. Do NOT create unparameterized duplicates!

4. **[Critical - Facade Removal for All Categories]**:
   - In `BimPlanRunnerScriptTemplate.cs`:
     Provide genuine execution logic for Slabs (`Floor.Create`/`NewFloor`), Openings (`FamilyInstance` on Wall), Rooms (`doc.Create.NewRoom`), MEP Fixtures, Pipe Runs (`Pipe.Create`), and Rebars (`Rebar.CreateFromCurves`), or handle them via integrated modular sub-runners.
     NEVER dump them into `else -> NoOp`.

5. **[Major - CLI Integration]**:
   - In `HPBim/src/HPBim.Cli/Commands/GenerateCommand.cs`:
     Set `options.UseExecutionPlan = true` so CLI commands execute via the unified `BimExecutionPlan` IR and `BimPipelineEngine.ExecutePlanAsync`.

6. **[Major - PlanHash Determinism]**:
   - In `ExecutionPlanBuilder.cs` and `BimExecutionPlan.cs`:
     Do NOT include random `Guid.NewGuid()` in `PlanHash`. Derive `PlanHash` deterministically from the model items and target doc snapshot (SHA-256).

7. **[Major - Clean Code Limits (C2 & M3)]**:
   - Split `BimModelSchemaValidator.cs` into partial classes or separate classes so each file is <= 300 lines (C2).
   - Split `ExecutionPlanBuilder.cs` into partial classes or helper builders so each file is <= 300 lines (C2).
   - Refactor methods exceeding 50 lines (M3): `Validate(...)`, `ExecutePlanAsync(...)`, `ParseElementRecords(...)`.

8. **[Major - Geometry & Validation Hardening]**:
   - Compute wall heights dynamically from level elevations (`targetTopLevel.Elevation - baseLevel.Elevation`) instead of hardcoding `Ft(3000.0)`.
   - Make CAD signature checking case-insensitive (`StringComparer.OrdinalIgnoreCase`) and expand signatures (`drawing`, `blocks`, `dxf`, `modelSpace`, etc.).
   - Implement `ValidateGrids` (non-empty name, non-zero length, finite coordinates).
   - Implement beam dimension checks (`WidthMm > 0 && HeightMm > 0`).
   - Allow empty `DocIdentifier` in `CompositeSourceKey.TryParse` (`^\[([^\]]*)\]#\[([^\]]+)\]:(.+)$`).

9. **[Verification]**:
   - Run `dotnet build HPBim/HPBim.slnx` -> 0 errors, 0 warnings.
   - Run `dotnet test HPBim/HPBim.slnx` -> 100% tests pass (Core, Client, E2E, Challenger tests).
   - Document build and test outputs in `handoff.md`.

## 2026-10-07T18:00:31Z
**From**: parent (44938268-52fa-4afa-af1c-06dc2c4f2512)
**Context**: Milestone M1 Remediation (Iteration 2)
**Content**: Checking in on status. Please report your current progress on Tasks 1-10 and update your progress.md timestamp.
**Action**: Please reply with your current status and progress update.

