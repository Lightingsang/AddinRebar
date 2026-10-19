# Exploration & Refactoring Strategy Report: CLI & Whole-Tree Clean Code Audit

**Agent**: `explorer_m1_it3_2` (teamwork_preview_explorer)  
**Date**: 2026-10-07 / 2026-10-08  
**Scope**: `HPBim/src/` (`HPBim.Core`, `HPBim.Client`, `HPBim.Cli`)  
**Mission**:
1. Investigate the 3 flagged CLI files (`Story1CadBeamDetector.cs`, `Story1SecondaryBeamService.cs`, `Story1SpanAuditService.cs`): determine origin, M1 vs legacy status, test dependencies, and design a refactoring strategy to split them $\le 300$ lines (C2) and methods $\le 50$ lines (M3).
2. Execute a full-tree audit across `HPBim/src/` for all Clean Code C2 (>300 lines) and M3 (>50 lines) violations.
3. Formulate an actionable remediation blueprint for the engineering team.

---

## 1. Executive Summary

Following the forensic audit rejection by `auditor_m1_it2`, this investigation conducted an exhaustive static analysis of the entire `HPBim/src/` source tree. The key findings are:

1. **Origin & Status of the 3 Flagged CLI Files**:
   - `Story1CadBeamDetector.cs` (343 lines), `Story1SecondaryBeamService.cs` (309 lines), and `Story1SpanAuditService.cs` (648 lines) are **LEGACY / FOUNDATION-SPECIFIC UTILITIES** created on 2026-10-07 during Milestone M3 foundation tasks (specifically for Story 1 / Da Kieng CAD beam detection in the TanDongHiep project model).
   - They are **NOT active in Milestone M1**. M1 is strictly bounded to P0 Core capabilities (Features F1–F4: Schema Validation, Single Execution Plan IR, Element Execution Records, Zero Heuristic Fallbacks).
   - Zero tests in `HPBim/tests/` reference or depend on any of these three files.
   - However, because they reside in `HPBim/src/HPBim.Cli/Commands/` and are registered in `Program.cs`, any repo-wide automated linter or whole-tree forensic audit will flag them as C2 violations.
2. **Whole-Tree C2 Violations**:
   - Across the entire `HPBim/src/` tree, **EXACTLY THREE FILES** exceed 300 lines. They are precisely the 3 Story 1 CLI files flagged above.
   - All files in `HPBim.Core/` (0 violations) and `HPBim.Client/` (0 violations) are strictly $\le 300$ lines.
3. **Whole-Tree M3 Violations**:
   - In **Milestone M1 active deliverables**, **three methods** violate Rule M3 ($\le 50$ lines):
     - `HPBim.Core/Validation/BimModelSchemaValidator.cs`:
       - `Validate`: **54 lines** (L36–L89)
       - `ValidateModelStructure`: **51 lines** (L145–L195)
     - `HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs`:
       - `ExecutePlanAsync`: **53 lines** (L18–L70)
   - In **pre-existing domain geometry**:
     - `HPBim.Core/Geometry/TriangularFootingGenerator.cs`: `Generate` spans **58 lines** (L18–L75).
   - In **HPBim.Cli**:
     - 58 command methods across 38 files exceed 50 lines (standard CLI `ExecuteAsync` orchestration and CAD script string builders).

---

## 2. Deep Dive: Investigation of the 3 Flagged CLI Files

### 2.1 File Catalog & Metrics

| File Path | Raw Lines | Purpose | Referencing Callers | Test Coverage | M1 Active? |
|---|:---:|---|---|---|:---:|
| `HPBim/src/HPBim.Cli/Commands/Story1CadBeamDetector.cs` | 343 | Detects parallel CAD line pairs to eliminate phantom beams and identify real beams | `Program.cs` (line 93, verb `"detect-cad-beams"`) | None (0 tests) | **No** (Legacy M3) |
| `HPBim/src/HPBim.Cli/Commands/Story1SecondaryBeamService.cs` | 309 | Merges off-grid collinear lines and creates secondary / perimeter / diagonal beams | `PrepareStory1GradeBeamsModelCommand.cs` (line 80) | None (0 tests) | **No** (Legacy M3) |
| `HPBim/src/HPBim.Cli/Commands/Story1SpanAuditService.cs` | 648 | Audits grid-to-grid spans between columns against CAD lines, classifies into main vs open-bay | `Program.cs` (line 94, verb `"audit-spans"`) | None (0 tests) | **No** (Legacy M3) |

### 2.2 Forensic Origin & Git History
- **Creation Timestamps**:
  - `Story1CadBeamDetector.cs`: 2026-10-07 21:15:21 UTC
  - `Story1SecondaryBeamService.cs`: 2026-10-07 20:51:48 UTC
  - `Story1SpanAuditService.cs`: 2026-10-07 21:16:22 UTC
- **Context of Introduction**:
  These files were authored by `worker_m3_foundation` and `explorer_m3_beams_fix` in response to the user follow-up prompt on 2026-10-07T02:05:11Z and 2026-10-07T06:10:58Z ("Thực thi dứt điểm phân đoạn Phần Móng... Trích xuất 22 loại giằng móng GM-1..22"). They operated as specialized offline processing tools parsing `scratch/cad_da_kieng_survey_dossier.json` to generate `scratch/cad_detected_real_beams.json` and `scratch/story1_grade_beams_model.json`.
- **Milestone Scope Alignment**:
  In `PROJECT.md`, Milestone M1 is explicitly defined as:
  > `M1: P0 Core: Baseline Repair, Schema Validation & Execution Plan IR` (Features F1–F4: `BimModelSchemaValidator`, `BimExecutionPlan`, `ElementExecutionRecord`, `FakeRevitPipeClient`).
  The master plan CLI layout specifies: `ValidateCommand.cs`, `GenerateCommand.cs`, `ReconcileCommand.cs`, `AuditCommand.cs`. The three `Story1*.cs` files are ad-hoc scratch-processing utilities outside the scope of M1.

---

## 3. Refactoring Strategy for the 3 CLI Files (C2 & M3 Compliance)

To satisfy repository Clean Code standards (Rule C2: class/file $\le 300$ lines; Rule M3: method $\le 50$ lines), the following refactoring blueprint is designed for immediate execution by the implementation worker.

### 3.1 Refactoring `Story1CadBeamDetector.cs` (343 lines $\rightarrow$ 3 files $\le 120$ lines)

#### Current Anatomy:
- Lines 18–322: Class `Story1CadBeamDetector` (305 lines)
  - `ExecuteAsync`: 72 lines (L28–L99) $\rightarrow$ **violates M3**
  - `MergeCollinear`: 31 lines (L101–L131)
  - `FindParallelPairs`: 31 lines (L133–L163)
  - `AssembleBeams`: 78 lines (L165–L242) $\rightarrow$ **violates M3**
  - `MatchText`: 39 lines (L244–L282)
  - `ParseText`: 7 lines (L284–L290)
  - `LoadGrids`: 10 lines (L292–L301)
  - `WriteDetailedReportAsync`: 19 lines (L303–L321)
- Lines 324–343: Declarations of records `Seg1D`, `SegDiag`, `BeamPair` and class `DetectedBeamModel` (20 lines).

#### Target Refactoring Structure:
1. **`HPBim/src/HPBim.Cli/Commands/Story1CadBeamModels.cs`** (~30 lines):
   - Move `Seg1D`, `SegDiag`, `BeamPair`, and `DetectedBeamModel`.
2. **`HPBim/src/HPBim.Cli/Commands/Story1CadBeamReportWriter.cs`** (~35 lines):
   - Move `WriteDetailedReportAsync` and its markdown string generation.
3. **`HPBim/src/HPBim.Cli/Commands/Story1CadBeamAssembler.cs`** (~95 lines):
   - Move `AssembleBeams` (split into `AssembleAxisBeams` $\le 35$ lines and `AssembleDiagonalBeams` $\le 25$ lines) and `MatchText`.
4. **`HPBim/src/HPBim.Cli/Commands/Story1CadBeamDetector.cs`** (~105 lines):
   - Retain `ExecuteAsync` (refactor pipeline steps into helper methods $\le 35$ lines), `MergeCollinear`, `FindParallelPairs`, `ParseText`, `LoadGrids`.

**Metrics Outcome**:
- All 4 files are $\le 105$ lines (100% C2 compliant).
- All methods are $\le 40$ lines (100% M3 compliant).

---

### 3.2 Refactoring `Story1SecondaryBeamService.cs` (309 lines $\rightarrow$ 2 files $\le 140$ lines)

#### Current Anatomy:
- Lines 11–309: Static class `Story1SecondaryBeamService` (299 lines)
  - `GenerateAllSecondaryBeams`: 22 lines (L13–L34)
  - `ProcessOffGridXBeams`: 57 lines (L36–L92) $\rightarrow$ **violates M3**
  - `ProcessOffGridYBeams`: 57 lines (L94–L150) $\rightarrow$ **violates M3**
  - `MergeCollinear`: 39 lines (L152–L190)
  - `ProcessDiagonalBeams`: 37 lines (L192–L228)
  - `SnapXEndpoints` & `SnapYEndpoints`: 16 lines (L230–L246)
  - `ResolveSecBeamInfo`: 33 lines (L248–L280)
  - `CreateSecBeam`: 27 lines (L282–L308)

#### Root Cause of Bloat & Duplication:
`ProcessOffGridXBeams` and `ProcessOffGridYBeams` share identical $O(N^2)$ greedy line-matching loops (lines 44–71 and 102–129) differing only by whether they process the X coordinate or the Y coordinate.

#### Target Refactoring Structure:
1. **Deduplicate Line Matching**:
   Extract common helper `FindBestParallelPair(List<CadLineItem> merged, int i, HashSet<int> used, out int bestJ, out double bestDist, out double s, out double e, out double overlap)` (~25 lines).
   This shortens both `ProcessOffGridXBeams` and `ProcessOffGridYBeams` from 57 lines to ~35 lines each (fixing M3).
2. **Split into Partial Classes**:
   - **`HPBim/src/HPBim.Cli/Commands/Story1SecondaryBeamService.cs`** (~120 lines):
     - `GenerateAllSecondaryBeams`
     - `MergeCollinear`
     - `FindBestParallelPair`
     - `SnapXEndpoints`, `SnapYEndpoints`, `ResolveSecBeamInfo`, `CreateSecBeam`
   - **`HPBim/src/HPBim.Cli/Commands/Story1SecondaryBeamService.Processing.cs`** (~110 lines):
     - `ProcessOffGridXBeams` (35 lines)
     - `ProcessOffGridYBeams` (35 lines)
     - `ProcessDiagonalBeams` (37 lines)

**Metrics Outcome**:
- Both partial class files are $\le 120$ lines (100% C2 compliant).
- All methods are $\le 40$ lines (100% M3 compliant).

---

### 3.3 Refactoring `Story1SpanAuditService.cs` (648 lines $\rightarrow$ 3 files $\le 180$ lines)

#### Current Anatomy:
- Lines 20–631: Class `Story1SpanAuditService` (612 lines)
  - `ExecuteAsync`: 73 lines (L30–L102) $\rightarrow$ **violates M3**
  - `AuditXGridSpans`: 55 lines (L104–L158) $\rightarrow$ **violates M3**
  - `AuditYGridSpans`: 55 lines (L160–L214) $\rightarrow$ **violates M3**
  - `CheckParallelCoverage`: 27 lines (L216–L242)
  - `DiscoverOffGridBeams`: 161 lines (L244–L404) $\rightarrow$ **violates M3**
  - `AssembleFinalVerifiedBeams`: 75 lines (L406–L480) $\rightarrow$ **violates M3**
  - `MatchText`: 39 lines (L482–L520)
  - `MergeCollinear`: 31 lines (L522–L552)
  - `ParseText` & `LoadGrids`: 17 lines (L554–L571)
  - `GenerateReportAsync`: 58 lines (L573–L630) $\rightarrow$ **violates M3**
- Lines 633–648: Class `SpanAuditItem` (16 lines).

#### Target Refactoring Structure:
1. **`HPBim/src/HPBim.Cli/Commands/Story1SpanAuditModels.cs`** (~25 lines):
   - Move `SpanAuditItem`.
2. **`HPBim/src/HPBim.Cli/Commands/Story1SpanAuditReportWriter.cs`** (~70 lines):
   - Move `GenerateReportAsync` and markdown table generation.
3. **Split Core Service into Partial Classes**:
   - **`HPBim/src/HPBim.Cli/Commands/Story1SpanAuditService.cs`** (~150 lines):
     - `ExecuteAsync`: refactored into sequential stages $\le 40$ lines.
     - `AuditXGridSpans`: extract inner span evaluation into `EvaluateSingleXSpan` $\rightarrow$ method is ~35 lines.
     - `AuditYGridSpans`: extract inner span evaluation into `EvaluateSingleYSpan` $\rightarrow$ method is ~35 lines.
     - `CheckParallelCoverage` (27 lines), `MergeCollinear` (31 lines), `ParseText`, `LoadGrids`.
   - **`HPBim/src/HPBim.Cli/Commands/Story1SpanAuditService.Discovery.cs`** (~180 lines):
     - Split `DiscoverOffGridBeams` into:
       - `DiscoverOffGridXBeams` (~45 lines)
       - `DiscoverOffGridYBeams` (~45 lines)
       - `DiscoverOffGridDiagonalBeams` (~30 lines)
     - Split `AssembleFinalVerifiedBeams` into helper methods (~40 lines).
     - `MatchText` (~39 lines).

**Metrics Outcome**:
- All 4 files are $\le 180$ lines (100% C2 compliant).
- All methods are $\le 45$ lines (100% M3 compliant).

---

## 4. Whole-Tree Clean Code Audit Findings

An automated scan of all C# source files across `HPBim/src/` (excluding `bin/` and `obj/`) was performed.

### 4.1 Whole-Tree C2 File Length Scan ($\le 300$ lines)

| Category / Subsystem | Total Files Scanned | Files Violating C2 (>300 lines) | Details |
|---|:---:|:---:|---|
| **`HPBim.Core/`** | 35 | **0** | Clean! Longest file: `FoundationQcEngine.cs` (296 lines) |
| **`HPBim.Client/`** | 22 | **0** | Clean! Longest file: `BimPlanRunnerScriptTemplate.Architecture.cs` (264 lines) |
| **`HPBim.Cli/`** | 76 | **3** | Violations: `Story1CadBeamDetector.cs` (343), `Story1SecondaryBeamService.cs` (309), `Story1SpanAuditService.cs` (648) |
| **Total `HPBim/src/`** | **133** | **3** | **3 out of 133 files violate C2 (2.2%)** |

#### Top 10 Near-Limit Files in `HPBim/src/` (Safely Under 300 lines):
1. `HPBim.Cli/Commands/PrepareStory1GradeBeamsModelCommand.cs`: 298 lines
2. `HPBim.Cli/Commands/AuditStory1GradeBeamsCommand.cs`: 297 lines
3. `HPBim.Core/Qc/FoundationQcEngine.cs`: 296 lines
4. `HPBim.Cli/Commands/InspectRevitModelCommand.cs`: 293 lines
5. `HPBim.Cli/Commands/AuditColumnsCommand.cs`: 293 lines
6. `HPBim.Core/Coordination/BimCoordinationLedgerService.cs`: 290 lines
7. `HPBim.Cli/Commands/RevitFoundationQueryService.cs`: 290 lines
8. `HPBim.Cli/Commands/BuildGradeBeamNetworkCommand.cs`: 283 lines
9. `HPBim.Core/Validation/BimModelSchemaValidator.Elements.cs`: 278 lines
10. `HPBim.Cli/Commands/FoundationRemediationBuilder.cs`: 277 lines

---

### 4.2 Whole-Tree M3 Method Length Scan ($\le 50$ lines)

A static parser evaluated all method declarations and their closing brace spans across `HPBim/src/`.

#### Group A: Active Milestone M1 Deliverables (MUST FIX FOR M1 CERTIFICATION)

| Project | File | Method Name | Line Range | Span | Limit | Action Required |
|---|---|---|---|:---:|:---:|---|
| `HPBim.Core` | `Validation/BimModelSchemaValidator.cs` | `Validate` | L36–L89 | **54 lines** | 50 | Extract root element parsing into helper |
| `HPBim.Core` | `Validation/BimModelSchemaValidator.cs` | `ValidateModelStructure` | L145–L195 | **51 lines** | 50 | Extract level validation loop into helper |
| `HPBim.Client` | `Pipeline/BimPipelineEngine.PlanExecution.cs` | `ExecutePlanAsync` | L18–L70 | **53 lines** | 50 | Extract stage event dispatch into helper |

*Note on `ExecutePlanAsync`*: In `worker_m1_it2_r1/report.md`, the worker attested that all methods in `BimPipelineEngine.PlanExecution.cs` were refactored to $\le 45$ lines. In fact, `ExecutePlanAsync` is 53 lines and represents an unflagged M3 violation in M1 scope.

#### Group B: Pre-existing / Foundation Domain Logic (Non-M1)

| Project | File | Method Name | Line Range | Span | Limit | Notes |
|---|---|---|---|:---:|:---:|---|
| `HPBim.Core` | `Geometry/TriangularFootingGenerator.cs` | `Generate` | L18–L75 | **58 lines** | 50 | 3-pile footing mathematical generator from foundation milestone |

#### Group C: CLI Commands (`HPBim.Cli`)
Across `HPBim.Cli`, 58 methods exceed 50 lines. The majority of these are command dispatch routines (`ExecuteAsync`) that coordinate file I/O, console logging, and multi-step pipeline actions.
Specifically, in the 3 Story 1 files:
- `Story1CadBeamDetector.cs`: `ExecuteAsync` (72 lines), `AssembleBeams` (78 lines)
- `Story1SecondaryBeamService.cs`: `ProcessOffGridXBeams` (57 lines), `ProcessOffGridYBeams` (57 lines)
- `Story1SpanAuditService.cs`: `ExecuteAsync` (73 lines), `AuditXGridSpans` (55 lines), `AuditYGridSpans` (55 lines), `DiscoverOffGridBeams` (161 lines), `AssembleFinalVerifiedBeams` (75 lines), `GenerateReportAsync` (58 lines)

---

## 5. Remediation Roadmap for Milestone M1 & Beyond

To resolve the auditor's rejection cleanly without cross-milestone scope contamination, the work should be sequenced into two distinct phases:

### Phase 1: Direct M1 Remediation (Immediate Gate Unblock)
The implementation worker assigned to M1 must make minimal, surgical edits to bring all touched M1 deliverables strictly under the 50-line M3 limit:
1. In `HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.cs`:
   - Refactor `Validate`: extract payload parsing into `TryParseJsonDocument(string json, out JsonDocument? doc, out SchemaValidationResult? failure)` (~15 lines). `Validate` becomes ~38 lines.
   - Refactor `ValidateModelStructure`: extract the level iteration into `ValidateLevels(IReadOnlyList<LevelDef> levels, HashSet<string> levelNames, List<string> errors)` (~20 lines). `ValidateModelStructure` becomes ~32 lines.
2. In `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs`:
   - Refactor `ExecutePlanAsync`: extract error message derivation and stage event dispatch into `DispatchPlanCompletion(BimExecutionPlan plan, ...)` (~15 lines). `ExecutePlanAsync` becomes ~38 lines.

### Phase 2: Whole-Tree CLI C2 Remediation (If Enforced by Orchestrator)
If the project orchestrator mandates zero C2 violations across the entire repo tree before declaring M1 or Foundation complete:
1. Apply the split strategy in Section 3:
   - Split `Story1CadBeamDetector.cs` into `Story1CadBeamDetector.cs`, `Story1CadBeamModels.cs`, `Story1CadBeamReportWriter.cs`, and `Story1CadBeamAssembler.cs`.
   - Split `Story1SecondaryBeamService.cs` into `Story1SecondaryBeamService.cs` and `Story1SecondaryBeamService.Processing.cs`.
   - Split `Story1SpanAuditService.cs` into `Story1SpanAuditService.cs`, `Story1SpanAuditService.Discovery.cs`, `Story1SpanAuditModels.cs`, and `Story1SpanAuditReportWriter.cs`.
2. Verify with `dotnet build HPBim/HPBim.slnx` and automated line check.
