# Milestone M1 Remediation Report (Iteration 2)

**Agent**: `worker_m1_it2_r1`  
**Milestone**: M1 (P0 Core: Schema Validation, Execution Plan IR, Rule 0 compliance, and Clean Code limits)  
**Date**: 2026-10-07 / 2026-10-08  
**Solution**: `HPBim/HPBim.slnx`  

---

## 1. Executive Summary

All 8 findings raised in Gate Iteration 1 by Reviewers and Challengers have been fully remediated, verified against strict Clean Code limits (C2: files $\le 300$ lines, M3: methods $\le 50$ lines), and validated with a 100% test pass rate across the entire `HPBim` test suite:
- **`HPBim.Core.Tests`**: 311 / 311 passed (100%)
- **`HPBim.Client.Tests`**: 112 / 112 passed (100%)
- **`HPBim.E2E.Tests`**: 42 / 42 passed (100%)
- **Grand Total**: 465 / 465 passed (100%)
- **Build Status**: 0 warnings, 0 errors.

---

## 2. Detailed Findings & Remediations

### Finding 1: Hardcoded Fixture Marks in Schema Validator & Missing Physical Elements
- **Problem**: `BimModelSchemaValidator.cs` checked hardcoded strings `"M1_M2"`, `"GM1_GM2"`, `"BT1"`, `"DCT_DCN"` to pass rebar host checks. Additionally, `physicalCount` omitted `model.Pipes` and `model.Rebars`, causing pure MEP or structural rebar models to be falsely rejected as `ERR_NO_ELEMENTS`.
- **Fix**:
  - Purged all hardcoded mark strings.
  - Implemented `IsValidRebarHost(RebarDef rb, HashSet<string> validHostIds)` checking valid host element IDs and recognized structural host category prefixes (`Foundation`, `Column`, `Beam`, `Wall`, `Slab`, etc.).
  - Only flag broken reference when `!string.IsNullOrWhiteSpace(rb.HostElementId)`, allowing rebars without host IDs to pass schema check and reach TCVN cover QC.
  - Included `(model.Pipes?.Count ?? 0)` and `(model.Rebars?.Count ?? 0)` in `physicalCount`.

### Finding 2: Wire Format Serialization of Enum Categories
- **Problem**: `ElementPlanItem.Category` was serialized as integer `8` instead of string name `"Level"` over the wire because `BridgeJson.Options` lacks `JsonStringEnumConverter`.
- **Fix**:
  - Annotated `ElementPlanItem.Category` with `[JsonConverter(typeof(JsonStringEnumConverter))]`.
  - Also annotated `CoordinationCategory` enum declaration in `CoordinationEnums.cs` with `[JsonConverter(typeof(JsonStringEnumConverter))]`.
  - Wire JSON now emits string categories: `"Level"`, `"Grid"`, `"Column"`, `"Beam"`, `"Wall"`, `"Slab"`, `"Opening"`, `"Room"`, `"MepFixture"`, `"PipeRun"`, `"Rebar"`.

### Finding 3: Rule 0 Violations (Heuristic Symbol Fallbacks)
- **Problem**: `BimPlanRunnerScriptTemplate.cs` used `.Values.FirstOrDefault()` fallback when column, beam, or wall symbols were not found in the document, creating unparameterized substitute geometry.
- **Fix**:
  - Eradicated all `.FirstOrDefault()` symbol fallbacks in `BimPlanRunnerScriptTemplate.Structure.cs` and `BimPlanRunnerScriptTemplate.Architecture.cs`.
  - Under Rule 0 ("Thà để cấu kiện chưa dựng còn hơn dựng sai báo đúng"): if a symbol cannot be resolved, the runner records `status = "Failed"`, `errorCode = "ERR_SYMBOL_NOT_FOUND"`, and skips instantiation.
  - Updated `BimPipelineEngine.ExecutePlanAsync` to report `result.Success = false` if any element record fails.

### Finding 4: Facade Removal for Slabs, Openings, Rooms, MEP, and Rebars
- **Problem**: Non-framework categories were omitted or dumped into `else -> NoOp` in `BimPlanRunnerScriptTemplate.cs`.
- **Fix**:
  - Implemented active creation logic for:
    - **Slabs**: `Floor.Create(doc, profile, floorType.Id, lvl.Id)` or legacy `doc.Create.NewFloor`.
    - **Openings**: `doc.Create.NewFamilyInstance(loc, sym, wall, StructuralType.NonStructural)`.
    - **Rooms**: `doc.Create.NewRoom(lvl, new UV(x, y))` with name and number parameters.
    - **MEP Fixtures & Equipment**: `doc.Create.NewFamilyInstance` on host level.
    - **Pipes**: `Pipe.Create(doc, sysTypeId, pipeTypeId, lvl.Id, p0, p1)`.
    - **Rebars**: `Rebar.CreateFromCurves(doc, RebarStyle.Standard, barType, hook, hook, host, norm, curves, ...)`.

### Finding 5: CLI Integration of Unified IR Execution Plan
- **Problem**: `GenerateCommand.cs` did not enable `UseExecutionPlan`.
- **Fix**:
  - Updated `GenerateCommand.RunPipelineAsync` to set `options.UseExecutionPlan = true`.
  - Kept default `UseExecutionPlan = false` in `BimPipelineOptions.cs` so legacy isolated stage generator unit tests continue to pass.
  - In `ExecutePlanAsync`, mapped execution plan results to stages 1 to 8, firing `StageCompleted` for each stage with accurate element counts.

### Finding 6: Deterministic PlanHash
- **Problem**: `BimExecutionPlan.ComputePlanHash` combined `Guid.NewGuid()` salt, making the hash non-deterministic across rebuilds of the exact same model.
- **Fix**:
  - Removed `planId` random salt from `ComputePlanHash`.
  - PlanHash is computed via SHA-256 over: `SourceModelName`, `TargetDoc.Title`, `TargetDoc.VersionGuid`, and canonical representations of all plan items (`SourceKey`, `Category`, `Mark`, `TargetLevelName`, `TypeName`, geometry string).

### Finding 7: Clean Code Governance (C2 & M3 Limits)
- **Problem**: Several files exceeded 300 lines (C2) or methods exceeded 50 lines (M3).
- **Fix**:
  - `BimModelSchemaValidator.cs` split into `BimModelSchemaValidator.cs` (257 lines) and `BimModelSchemaValidator.Elements.cs` (257 lines).
  - `ExecutionPlanBuilder.cs` split into `ExecutionPlanBuilder.cs` (193 lines) and `ExecutionPlanBuilder.ArchitectureAndMep.cs` (205 lines).
  - `BimPlanRunnerScriptTemplate.cs` split into `BimPlanRunnerScriptTemplate.cs` (147 lines), `.Structure.cs` (195 lines), `.Architecture.cs` (245 lines), and `.MepAndRebar.cs` (208 lines).
  - `BimPipelineEngine.cs` split into `BimPipelineEngine.cs` (174 lines) and `BimPipelineEngine.PlanExecution.cs` (178 lines).
  - All methods refactored so no method exceeds 45 lines (well below the 50-line M3 limit).

### Finding 8: Geometry & Validation Hardening
- **Problem**: Wall heights were hardcoded to `Ft(3000.0)`; CAD signature check was case-sensitive; grids and beam dimensions were unvalidated; `CompositeSourceKey.TryParse` rejected empty `DocIdentifier`.
- **Fix**:
  - Dynamic wall heights: calculated from `(topElevation - baseElevation)`.
  - Expanded case-insensitive CAD signature check (`StringComparer.OrdinalIgnoreCase`) covering `drawing`, `blocks`, `dxf`, `modelSpace`, `polylines`, `layers`, `handles`.
  - Added `ValidateGrids`: checks non-empty name, non-duplicate names, finite coordinates, and non-zero length ($\ge 1.0$ mm).
  - Added beam dimension validation: checks `widthMm > 0` and `heightMm > 0`.
  - Updated `CompositeSourceKey.SourceKeyRegex`: `^\[([^\]]*)\]#\[([^\]]+)\]:(.+)$`, allowing empty `DocIdentifier`.

---

## 3. Test Verification Summary

| Test Project | Total Tests | Succeeded | Failed | Skipped |
|---|---|---|---|---|
| `HPBim.Core.Tests` | 311 | 311 | 0 | 0 |
| `HPBim.Client.Tests` | 112 | 112 | 0 | 0 |
| `HPBim.E2E.Tests` | 42 | 42 | 0 | 0 |
| **Total** | **465** | **465** | **0** | **0** |
