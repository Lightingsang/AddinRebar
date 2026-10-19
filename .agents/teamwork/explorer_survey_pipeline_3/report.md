# Comprehensive Survey Report: Test Baseline, Acceptance Criteria Specifications, Clean Code Governance & R3 Architectural Proposal

**Date**: 2026-10-07  
**Agent**: `explorer_survey_pipeline_3` (Archetype: `teamwork_preview_spec_miner`)  
**Workspace**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim`  
**Reference Document**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Update: `## 2026-10-07T13:49:28Z`)

---

## 1. Executive Summary

This report delivers a complete, read-only architectural survey and test specification for the **HPBim CAD-to-BIM Pipeline** and **BIM Coordination Ledger**, supporting the transition to P0 (Truthful Results & Single Execution Plan IR) and P1 (Recovery, Cross-Session Idempotency & Extensible Storage Receipts).

### Key Findings
1. **Existing Test Baseline (370 Tests)**:
   - `HPBim.Core.Tests`: **268 tests** running on .NET 10 via Microsoft.Testing.Platform and xUnit v3 (execution time ~694ms). 100% green.
   - `HPBim.Client.Tests`: **102 tests** running on .NET 10 via Microsoft.Testing.Platform and xUnit v3 (execution time ~4.6s). Includes Roslyn syntax analysis and Named Pipe simulation via `FakeRevitPipeClient`.
   - *Active Observation*: An external modification to `IRevitPipeClient.cs` (adding `CancelAsync`) at 2026-10-07T21:00:43+07:00 introduced a compilation error (`CS0535`) in `FakeRevitPipeClient.cs` because `FakeRevitPipeClient` was not updated to implement `CancelAsync`. This needs immediate synchronization in the implementation phase.
2. **Critical Implementation Gaps in Current Codebase**:
   - **Schema Bypass**: `BimModelSerializer.cs:49` deserializes raw CAD JSON or `{}` into an empty `BimModel`, which causes `ValidateCommand.cs:62` to falsely claim "TRẠNG THÁI: SẠCH 100%".
   - **Dual Execution Divergence**: `BimPipelineEngine.cs:59-150` executes dry-run via `CompositeBimGenerator` but standard apply via sequential stage generators, lacking a unified `BimExecutionPlan` Intermediate Representation (IR).
   - **Unverified Dry-Run**: Pipeline checks `!execRes.IsError` without strictly failing when `RolledBack == false` during a dry-run.
   - **Zero Idempotency Tracking**: No composite source keys (`[DocIdentifier]#[Category]:[PrimaryHandle]`) exist, causing re-runs to duplicate elements.
   - **No Extensible Storage Receipts**: Disconnects/timeouts cause untracked failures (`RevitPipeException`) without `OutcomeUnknown` handling or automatic reconciliation upon reconnection.
   - **Count-Only Verification**: `RevitModelVerifier.cs:116-137` only checks integer element counts, ignoring geometrical dimensions, elevations, and `CoordinationTolerances`.
3. **Clean Code Governance & Architectural Compliance**:
   - Strict adherence to `HP_CLEAN_CODE_CORE.md`, `REVITADDINAI_CLEAN_CODE_STANDARD.md`, and `DEPENDENCY_RULES.md`:
     - Method lines: ≤ 20 (ideal), ≤ 50 (review trigger), > 100 (mandatory split).
     - Class lines: ≤ 300 lines (triggers cohesion check; blocking for seeds/runtime tools Q-B3).
     - Dependency rule: `HPBim` → `McpShared/` only. Zero circular dependencies. `HPBim.Core` targets `netstandard2.0` with zero `Autodesk.*` references.

---

## 2. Existing Test Baseline Survey (370 Tests)

### 2.1 Project Infrastructure & Runners

| Metric / Property | `HPBim.Core.Tests` | `HPBim.Client.Tests` |
|---|---|---|
| **Project File** | `tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj` | `tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj` |
| **Target Framework** | `net10.0` | `net10.0` |
| **Output Type** | `Exe` | `Exe` |
| **Test Runner** | Microsoft.Testing.Platform (`<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>`) | Microsoft.Testing.Platform (`<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>`) |
| **Frameworks** | `xunit.v3` (3.1.0), `xunit.runner.visualstudio` (3.1.5) | `xunit.v3` (3.1.0), `xunit.runner.visualstudio` (3.1.5) |
| **Special Libraries** | `Microsoft.Bcl.AsyncInterfaces` (10.0.12) | `Microsoft.CodeAnalysis.CSharp` (4.12.0) |
| **Project References** | `HPBim.Core` | `HPBim.Client`, `HPBim.Core`, `HPBim.Cli`, `HPBim.Core.Tests`, `HPRebar.Mcp.Contracts` |
| **Test Count** | **268 tests** | **102 tests** |
| **Execution Duration** | ~694 ms | ~4,630 ms |
| **Revit Host Dependency** | 0 (Host-free, runs completely in-memory) | 0 (Host-free, uses `FakeRevitPipeClient` and Roslyn syntax trees) |

---

### 2.2 `HPBim.Core.Tests` Breakdown (268 Tests)

`HPBim.Core.Tests` validates mathematical models, structural TCVN design rules, geometry algorithms, clash detection, benchmark fixtures, and the Fail-Fast coordination gate:

| # | Test Class File | Test Count | Scope & Coverage |
|---|---|---|---|
| 1 | `AdvancedAdversarialAndTcvnStressTests.cs` | 23 | TCVN deflection limits, beam span-to-depth ratios, high-stress concrete cover, crack width checks, and adversarial zero/negative load cases. |
| 2 | `CollinearWallTests.cs` | 12 | Merging collinear wall segments, direction normalization, overlapping segment resolution, T-junction detection. |
| 3 | `FoundationQcEngineTests.cs` | 5 | Foundation QC rules: pile spacing, pile edge distances, pile-to-footing alignment, elevation consistency. |
| 4 | `GeometryTests.cs` | 13 | Vector2D/3D operations, line intersections, polygon area calculation, orientation, bounding box containment. |
| 5 | `GradeBeamGridSnapperTests.cs` | 5 | Snapping grade beam centerlines to nearest structural grid intersections and orthogonal axis alignment. |
| 6 | `JsonSerializationStressTests.cs` | 5 | Extreme JSON inputs, missing optional fields, unicode escaping, roundtrip fidelity. |
| 7 | `MultiLevelClashIsolationTests.cs` | 9 | Multi-level clash detection (columns vs beams, pipes vs beams, walls vs doors), tolerance bounding box hierarchies. |
| 8 | `NhaPho5x18mAdversarialStressTests.cs` | 12 | Adversarial stress testing of the 5x18m benchmark under coordinate corruption, missing types, and degenerate dimensions. |
| 9 | `NhaPho5x18mTests.cs` | 12 | Benchmark townhouse integrity: 5 levels, grids X1-X5, YA-YB, 10 RC columns 220x220, beams, slabs, 220/110 walls. |
| 10 | `PolygonAndClippingStressTests.cs` | 17 | Sutherland-Hodgman polygon clipping, concave polygons, self-intersecting hulls, degenerate zero-area polylines. |
| 11 | `QcEngineTests.cs` | 8 | Core QC checks: clear cover, minimum bar spacing, minimum wall thickness, clear door width, headroom clearances. |
| 12 | `QcOptionsBoundaryTests.cs` | 21 | Boundary testing of QC options, tolerance parameters, warning vs error thresholds. |
| 13 | `TcvnRulesTests.cs` | 23 | TCVN 5574:2018 structural rules: concrete grades (B20, B25, B30), rebar grades (CB240-T, CB300-V, CB400-V), min/max reinforcement ratios, lap lengths. |
| 14 | `TriangularFootingGeneratorTests.cs` | 4 | 3-pile triangular footing geometry, centroid calculation, corner offset determination. |
| 15 | `Coordination/BimCoordinationLedgerSerializationTests.cs` | 3 | JSON serialization and deserialization of `BimCoordinationLedger`, schema fields, versioning, element record preservation. |
| 16 | `Coordination/BimCoordinationLedgerServiceTests.cs` | 6 | Ledger service lifecycle transitions across 7 steps, freeze zone enforcement, status updates. |
| 17 | `Coordination/CoordinationLifecycleStepTests.cs` | 5 | Validation of the 7 lifecycle steps, allowed transitions, rejection of illegal backwards state jumps. |
| 18 | `Coordination/EmpiricalChallengerM2Tests.cs` | 34 | State machine history tracking, immutable audit log, ledger roundtrip, summary calculations, unverified halt verification. |
| 19 | `Coordination/FailFastGateAdversarialProbeTests.cs` | 40 | Pathological inputs: NaN/Infinity elevations, negative/zero dimensions, degenerate pile diameters, inverted elevations, null/whitespace supports. |
| 20 | `Coordination/FailFastGateTests.cs` | 11 | Fail-fast violations FF1–FF6 (missing handle, missing/inverted elevation, missing dimensions, insufficient supports, cross-check conflicts), `AssertReadyForRevit`. |
| **Total** | | **268** | **All 268 tests pass** |

---

### 2.3 `HPBim.Client.Tests` Breakdown (102 Tests)

`HPBim.Client.Tests` validates client-side Named Pipe communications, Roslyn C# script compilation, stage-by-stage model generation, CLI command parsing and execution, and model verification:

| # | Test Class File | Test Count | Scope & Coverage |
|---|---|---|---|
| 1 | `Cli/CliParserTests.cs` | 7 | Command parsing, options (`--input`, `--output`), flags (`--dry-run`), positional arguments, fallback values. |
| 2 | `Cli/ConsoleFormatterTests.cs` | 4 | Bordered banner generation, badge formatting, aligned table rendering, empty header handling. |
| 3 | `Cli/EstimateCommandTests.cs` | 6 | BOQ estimation: 7,966.5 kg benchmark rebar weight, concrete and formwork volume, MEP fixture counts, exit codes 0 and 2. |
| 4 | `Cli/FailFastCliCommandsAdversarialTests.cs` | 6 | Adversarial probe: rejects unverified lines, unverified specs, missing types, speculative clustering, free-radius clustering without bypass. |
| 5 | `Cli/GenerateCommandTests.cs` | 5 | Generation dispatch: `--dry-run` flag propagation, full stage execution, invalid model abort, pipe failure exit code 1, missing input exit code 2. |
| 6 | `Cli/ValidateCommandTests.cs` | 4 | Validation CLI: benchmark model exit code 0, cover defect exit code 1, missing file exit code 2, missing argument exit code 2. |
| 7 | `Cli/VerifyCommandTests.cs` | 4 | Verification CLI: exact match exit code 0, element count mismatch exit code 1, missing file exit code 2, missing argument exit code 2. |
| 8 | `Connection/RevitPipeClientAdversarialTests.cs` | 11 | Non-existent pipe timeout, cancellation tokens, disposed client handling, JSON-RPC errors, abrupt server disconnects, request timeouts (`IsTimeout = true`), concurrency safety. |
| 9 | `Connection/RevitPipeClientTests.cs` | 7 | Default connection options, pipe name validation, timeout range checking, not-connected exception, ping behavior, round-trip with in-memory pipe server, idempotent dispose. |
| 10 | `Pipeline/BimPipelineEngineAdversarialTests.cs` | 11 | Downstream stage halting on failure, arbitrary stage subsets, atomic dry-run execution, non-atomic dry-run with `dryRun=true`, auto-connect failure, `StageCompleted` events. |
| 11 | `Pipeline/BimPipelineEngineTests.cs` | 5 | Sequential 8-stage execution, atomic dry-run composite call, selected stage execution, failure abort, auto-connect when disconnected. |
| 12 | `Pipeline/ChallengerM3It2AdversarialTests.cs` | 5 | 20 concurrent connect calls synchronization, composite generator empty model handling, argument count parity between composite and individual generators, atomic rollback verification, cancellation cleanup. |
| 13 | `ScriptGenerators/ScriptSyntaxTests.cs` | 11 | Roslyn C# syntax compilation check (0 syntax errors) for Stages 1–8 scripts, CompositeBimGenerator script, DynamicTypeScriptHelper, and RevitModelVerifier query script. |
| 14 | `ScriptGenerators/StageGeneratorTests.cs` | 10 | Script payload generation for Stages 1–8 (Levels, Grids, Structure, Walls, Openings, Rooms, MEP, Rebar) and Composite BIM generator. |
| 15 | `TypeResolver/TypeResolutionRulesTests.cs` | 4 | Family and Type naming rules for walls, floors, columns, beams, doors, windows, and standard constants. |
| 16 | `Verification/RevitModelVerifierTests.cs` | 3 | Exact count match verification, mismatch discrepancy reporting, script failure exception handling. |
| **Total** | | **102** | **All 102 tests verified** |

---

## 3. Required Acceptance Criteria Test Suites Mapping

To satisfy the authoritative specification in `ORIGINAL_REQUEST.md ## 2026-10-07T13:49:28Z`, six dedicated test suites must be mapped and implemented.

### 3.1 Suite 1: Schema Validation Test Suite

#### Problem Statement & Codebase Gap
In `BimModelSerializer.cs:49`:
```csharp
var model = JsonSerializer.Deserialize<BimModel>(json, DefaultOptions);
return model ?? throw new JsonException("Deserialization returned null BimModel.");
```
If an input JSON is raw CAD JSON (`{"entities": [...]}`) or `{}`:
1. `BimModel` is instantiated with empty lists (`Levels = []`, `Columns = []`, etc.).
2. In `ValidateCommand.cs:40`: `qcEngine.Validate(model)` runs 0 checks because all collections are empty.
3. `report.IsValid` evaluates to `true`.
4. `ValidateCommand` writes `"TRẠNG THÁI: SẠCH 100%. Mô hình sẵn sàng dispatch tới Revit."` and returns exit code 0.
This completely bypasses schema validation.

#### Target Test File
`tests/HPBim.Core.Tests/Serialization/SchemaValidationTests.cs` and `tests/HPBim.Client.Tests/Cli/SchemaValidationCliTests.cs`

#### Detailed Test Case Specifications
| Test ID | Test Name | Input / Precondition | Expected Behavior / Assertion |
|---|---|---|---|
| **TC-SCH-01** | `Deserialize_RawCadJson_ThrowsSchemaValidationException` | Raw CAD export JSON containing entity lists (e.g. `{"layers": [], "blocks": []}`) without BIM schema metadata. | Throws `SchemaValidationException` stating that input is a CAD artifact, not a valid BimModel schema. |
| **TC-SCH-02** | `Deserialize_EmptyObjectJson_ThrowsSchemaValidationException` | Empty JSON object `{}`. | Throws `SchemaValidationException` citing missing mandatory schema root properties (`$schema`, `version`, `modelId`, `levels`, `units`). |
| **TC-SCH-03** | `Deserialize_MissingRequiredLevelsAndGrids_ThrowsSchemaValidationException` | JSON containing valid structural elements but missing `levels` or `grids`. | Throws `SchemaValidationException` identifying missing dependency infrastructure (`levels` are prerequisite for hosting). |
| **TC-SCH-04** | `Deserialize_UnsupportedSchemaVersion_ThrowsSchemaValidationException` | JSON with `"version": "0.1"` or `"version": "99.0"` where current supported version is `"1.0"`. | Throws `SchemaValidationException` citing unsupported schema version. |
| **TC-SCH-05** | `ValidateCommand_WhenGivenRawCadJson_ReturnsExitCode1Or2WithClearError` | CLI invocation: `hpbim validate --input raw_cad.json`. | Exit code `2` or `1`, stderr/console displays `[ERROR] Schema Validation Failed: Detected raw CAD JSON`. Zero PASS message. |
| **TC-SCH-06** | `ValidateCommand_WhenGivenEmptyModelJson_ReturnsExitCode1Or2WithClearError` | CLI invocation: `hpbim validate --input empty.json`. | Exit code `2` or `1`, displays `[ERROR] Model contains zero structural definitions`. |

---

### 3.2 Suite 2: Single Execution Plan Test Suite

#### Problem Statement & Codebase Gap
Currently, `BimPipelineEngine.cs` contains diverging execution paths:
- **Dry-Run (Atomic)**: Calls `_compositeGenerator.Generate(model)` which merges all 8 stages into one monolithic C# script and payload.
- **Apply (Sequential)**: Iterates `_generators` calling individual stage generators (`Stage1LevelGenerator`, `Stage2GridGenerator`, etc.) producing 8 independent scripts.
Furthermore, resource resolution (resolving Revit FamilySymbols, matching Levels, computing coordinate transforms) is duplicated inside in-memory string-interpolated C# scripts. There is no unified Intermediate Representation (IR) shared between Preview and Apply.

#### Target Test File
`tests/HPBim.Client.Tests/Pipeline/SingleExecutionPlanTests.cs` and `tests/HPBim.Core.Tests/Pipeline/BimExecutionPlanBuilderTests.cs`

#### Detailed Test Case Specifications
| Test ID | Test Name | Input / Precondition | Expected Behavior / Assertion |
|---|---|---|---|
| **TC-SEP-01** | `ExecutionPlanBuilder_Build_ProducesIdenticalPlanIRForPreviewAndApply` | Benchmark `NhaPho5x18mFixture.CreateModel()`. | `BimExecutionPlanBuilder.Build(model)` outputs a `BimExecutionPlan` IR containing an immutable list of planned operations with resolved SourceKeys, category types, coordinate transforms, and transaction boundaries. Hash of the plan IR is identical regardless of execution mode. |
| **TC-SEP-02** | `PipelineEngine_PreviewAndApply_ExecuteIdenticalPlanPayload` | Pre-built `BimExecutionPlan`. | Compare the payload dispatched to `IRevitPipeClient.ExecuteAsync`: the script, parameters, and operation sequences must be byte-for-byte identical; only `request.DryRun` differs (`true` for Preview, `false` for Apply). |
| **TC-SEP-03** | `ExecutionPlan_ResourceResolution_ResolvesSymbolsBeforeExecution` | BimModel with custom Column 220x220 and Wall 110. | The Plan IR explicitly defines family creation/resolution directives ahead of placement directives, eliminating ad-hoc fallback scripts. |
| **TC-SEP-04** | `ExecutionPlan_TransactionBoundaries_AreExplicitAndAtomic` | Multi-category model (Columns, Beams, Walls). | The Plan IR defines explicit Transaction and TransactionGroup scopes (`MCP: BimPipelineApply` vs `MCP: BimPipelinePreview`), guaranteeing atomic containment. |

---

### 3.3 Suite 3: Dry-Run Verification Test Suite

#### Problem Statement & Codebase Gap
In `BimPipelineEngine.cs:78-86`:
```csharp
return new BimPipelineResult
{
    Success = stageRes.Success,
    RolledBack = execRes.RolledBack,
    ...
};
```
If a dry-run script runs without throwing a runtime error (`!execRes.IsError`), but the Revit Bridge fails to roll back or returns `RolledBack == false`, the engine currently treats `Success = true`. The requirement mandates: "Dry-run chỉ được báo thành công khi mã thực thi không có lỗi và Revit bridge xác nhận transaction đã được rollback 100%. Phản ánh trung thực partial commit và outcome unknown; thu thập warning từ Revit."

#### Target Test File
`tests/HPBim.Client.Tests/Pipeline/DryRunVerificationTests.cs`

#### Detailed Test Case Specifications
| Test ID | Test Name | Input / Precondition | Expected Behavior / Assertion |
|---|---|---|---|
| **TC-DRY-01** | `ExecuteAsync_DryRun_WhenRollbackConfirmed_ReturnsSuccessTrue` | `FakeRevitPipeClient` returning `IsError: false`, `RolledBack: true`, `Changed: {0, 0, 0}`. | `BimPipelineResult.Success` is `true`, `RolledBack` is `true`, `ErrorMessage` is null. |
| **TC-DRY-02** | `ExecuteAsync_DryRun_WhenRollbackUnconfirmed_ReturnsFailureWithUnconfirmedRollbackDefect` | `FakeRevitPipeClient` returning `IsError: false`, `RolledBack: false` (bridge unconfirmed rollback). | `BimPipelineResult.Success` MUST be `false`; `ErrorMessage` contains `"UnconfirmedRollbackDefect: Dry-run transaction was not confirmed rolled back by Revit bridge."` |
| **TC-DRY-03** | `ExecuteAsync_DryRun_WhenScriptThrows_ReturnsFailureAndCapturesDiagnostics` | `FakeRevitPipeClient` returning `IsError: true`, `Message: "Revit API Exception"`, `Diagnostics: [L12: CS0246]`. | `BimPipelineResult.Success` is `false`, diagnostics are formatted and reported; status is NOT reported as successful rollback. |
| **TC-DRY-04** | `ExecuteAsync_DryRun_CollectsRevitWarnings` | Bridge execution returning non-fatal warnings (e.g. "Room separation line overlaps with wall"). | `BimPipelineResult.Warnings` contains all collected Revit warning messages; warnings are surfaced to user without masking. |

---

### 3.4 Suite 4: Idempotency Test Suite

#### Problem Statement & Codebase Gap
Currently, HPBim does not implement the Composite Source Key specification `[DocIdentifier]#[Category]:[PrimaryHandle]` or provenance tracking. Generating elements repeatedly executes creation scripts (`doc.Create.NewFamilyInstance`, `Wall.Create`) without checking if an element with the corresponding SourceKey already exists in the document. This results in duplicate elements in Revit.

#### Target Test File
`tests/HPBim.Client.Tests/Pipeline/IdempotencyTests.cs` and `tests/HPBim.Core.Tests/Coordination/CompositeSourceKeyTests.cs`

#### Detailed Test Case Specifications
| Test ID | Test Name | Input / Precondition | Expected Behavior / Assertion |
|---|---|---|---|
| **TC-IDM-01** | `CompositeSourceKey_Format_MatchesSpecification` | Column element with primary CAD handle `3F2A`, category `StructuralColumns`, doc `TANDONGHIEP_KC`. | Formatted key equals `TANDONGHIEP_KC#StructuralColumns:3F2A`. Parsing key roundtrips cleanly. |
| **TC-IDM-02** | `Pipeline_FirstRun_CreatesElementsAndRecordsMapping` | Plan containing 10 columns; clean Revit document. | Run 1 executes creation; returns status `Created` for all 10 elements; maps each `SourceKey` to its generated Revit `UniqueId`. |
| **TC-IDM-03** | `Pipeline_SecondRunIdenticalPlan_ReturnsNoOpWithoutDuplicates` | Run 2 with the exact same plan against the document from TC-IDM-02. | Run 2 queries existing elements via SourceKey mapping; identifies that geometry and parameters are identical; returns status `NoOp` for all 10 elements; 0 elements created, 0 duplicated. |
| **TC-IDM-04** | `Pipeline_SecondRunModifiedPlan_UpdatesElements` | Run 2 where Column `3F2A` height is changed from 3.6m to 3.8m. | Run 2 identifies existing `UniqueId`; updates height parameter in place; returns status `Updated` for Column `3F2A`, `NoOp` for the other 9 elements. |
| **TC-IDM-05** | `Pipeline_PerElementGranularity_ReportsIndividualElementStatus` | Plan execution with mixed outcomes (1 Created, 2 Updated, 3 NoOp, 1 Skipped, 1 Failed). | Result object contains `IReadOnlyList<ElementExecutionOutcome>` where every element explicitly reports its exact state, ID, and detailed diagnostic message. No implicit fallback to default type. |

---

### 3.5 Suite 5: Reconcile & Recovery Test Suite

#### Problem Statement & Codebase Gap
In `RevitPipeClient.cs`, if a transport timeout or process disconnect occurs after a request is sent, the client throws `RevitPipeException`. The pipeline has no mechanism to know whether Revit executed the transaction before the pipe disconnected.
The requirement specifies:
- Operation Intent written to local Intent Journal before pipe transmission.
- Operation ID and element mapping stored in Revit Extensible Storage (hidden `DataStorage` element) in the same transaction as geometry creation.
- Timeout separation: Transport timeout vs Queue deadline vs Execution budget.
- When timeout occurs, mark `OutcomeUnknown`.
- Upon reconnection, query Extensible Storage to reconcile state without sending blind re-execution commands.

#### Target Test File
`tests/HPBim.Client.Tests/Pipeline/ReconcileAndRecoveryTests.cs` and `tests/HPBim.Core.Tests/Journal/IntentJournalTests.cs`

#### Detailed Test Case Specifications
| Test ID | Test Name | Input / Precondition | Expected Behavior / Assertion |
|---|---|---|---|
| **TC-REC-01** | `IntentJournal_LogIntentBeforeTransmission_PersistsOperationIdAndPlanHash` | Before dispatching execute request over Named Pipe. | `IntentJournal` writes a journal entry to local disk with unique `OperationId`, `PlanHash`, timestamp, and target document path before pipe I/O begins. |
| **TC-REC-02** | `PipeClient_TransportTimeout_MarksOperationOutcomeUnknown` | Client sends execution request; pipe server disconnects or transport timeout expires before response is received. | Client catches timeout and flags operation status as `OutcomeUnknown` (NOT as Failed, NOT as Succeeded). Avoids re-dispatching. |
| **TC-REC-03** | `ReconcileService_WhenReconnecting_ReadsExtensibleStorageReceipt` | Reconnection established after an `OutcomeUnknown` event. | `ReconcileService` queries Revit via read-only script inspecting `DataStorage` elements for an Extensible Storage entity matching `OperationId`. |
| **TC-REC-04** | `ReconcileService_WhenReceiptFound_UpdatesLedgerAndConfirmsCommit` | `DataStorage` receipt found in Revit matching `OperationId` with list of committed `(SourceKey, UniqueId)` pairs. | `ReconcileService` updates the local ledger, marks the operation as `Committed`, records the element mappings, and updates status to `Reconciled`. |
| **TC-REC-05** | `ReconcileService_WhenReceiptNotFound_ReportsTransactionRolledBackOrUncommitted` | Reconnection established; `DataStorage` receipt does not exist in Revit. | `ReconcileService` concludes the transaction was aborted/rolled back in Revit; safely marks status as `Uncommitted`, allowing an idempotent re-run. |

---

### 3.6 Suite 6: Measured Geometric Verification Test Suite

#### Problem Statement & Codebase Gap
In `RevitModelVerifier.cs:116-137`:
The current verification implementation queries Revit only for integer counts:
```csharp
var items = new (string Category, int Exp, int Act)[]
{
    ("Levels", exp.Levels, act.Levels),
    ("Columns", exp.Columns, act.Columns),
    ...
};
```
If a model has 10 columns planned and 10 columns created in Revit, `RevitModelVerifier` reports `IsMatch = true`, even if every column has the wrong section size (e.g. 200x200 instead of 220x220) or an incorrect elevation (e.g. +3.000m instead of +3.800m).
The requirement mandates: "Kiểm toán hình học độc lập (Measured Geometric Verification): Kiểm toán sau commit bằng cách đo trực tiếp từ Revit API (tâm, cao độ đỉnh/đáy, kích thước tiết diện từ Solid/Curve/Parameters) so sánh với BimExecutionPlan trong phạm vi CoordinationTolerances; không dùng số lượng cấu kiện (counts) để nghiệm thu."

#### Target Test File
`tests/HPBim.Client.Tests/Verification/MeasuredGeometricVerificationTests.cs` and `tests/HPBim.Core.Tests/Coordination/GeometricAuditEngineTests.cs`

#### Detailed Test Case Specifications
| Test ID | Test Name | Input / Precondition | Expected Behavior / Assertion |
|---|---|---|---|
| **TC-GEO-01** | `VerifyGeometry_WhenElevationDiffersByMoreThan1mm_MarksDefectEvenIfCountsMatch` | Plan: 10 columns. Actual Revit: 10 columns present (100% count match). Column C1 Top Elevation is +3798.5mm vs Expected +3800.0mm (error = 1.5mm > 1.0mm tolerance). | `VerificationReport.IsMatch` is `false`; Column C1 is flagged as `Failed`/`Defect` with reason `"Elevation error 1.5mm exceeds tolerance 1.0mm"`. Overall status is Defect. |
| **TC-GEO-02** | `VerifyGeometry_WhenSectionDimensionDiffersByNonZero_MarksDefectEvenIfCountsMatch` | Plan: Column 220x220mm. Actual Revit: Column 200x200mm (error = 20mm > 0.0mm tolerance). | `VerificationReport.IsMatch` is `false`; Column flagged as `Defect` with reason `"Section dimension error 20mm exceeds tolerance 0.0mm"`. |
| **TC-GEO-03** | `VerifyGeometry_WhenPileCenterDiffersByMoreThan2mm_MarksDefect` | Plan: Pile center (1000.0, 2000.0). Actual Revit: Pile center (1002.5, 2000.0) (offset = 2.5mm > 2.0mm tolerance). | `VerificationReport.IsMatch` is `false`; Pile flagged as `Defect`. |
| **TC-GEO-04** | `VerifyGeometry_WhenMeasurementDataMissing_ReturnsInconclusive` | Actual Revit element query returns element presence but bounding box / solid geometry cannot be retrieved. | Result status MUST be `Inconclusive` ("Chưa đủ cơ sở để kết luận"); strictly forbids automatic PASS/acceptance. |
| **TC-GEO-05** | `VerifyGeometry_WhenAllWithinTolerances_ReturnsPass` | All 10 columns, beams, footings measured via Revit API: section error = 0.0mm, elevation error ≤ 1.0mm, center offset ≤ 1.0mm. | `VerificationReport.IsMatch` is `true`; status is `Verified` with full empirical measurement log attached. |

---

## 4. Features Discovered & Edge Cases Tables

### 4.1 Features Discovered
| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|---|---|---|---|---|---|---|
| 1 | Baseline Tests | Core Unit Test Suite | 268 host-free unit tests covering TCVN, geometry, clash detection, benchmark fixtures, and Fail-Fast gate | Command: `dotnet test HPBim.Core.Tests.csproj` | Test execution results, pass/fail counts | Fails if assertion or tolerance violated | `tests/HPBim.Core.Tests/` |
| 2 | Baseline Tests | Client Integration Test Suite | 102 host-free integration tests verifying Named Pipe RPC, Roslyn script compilation, stage generators, CLI commands | Command: `dotnet test HPBim.Client.Tests.csproj` | Test execution results, pass/fail counts | Fails if RPC, parsing, or syntax invalid | `tests/HPBim.Client.Tests/` |
| 3 | Core Coordination | Fail-Fast Gate | Rejects incomplete or ambiguous CAD structural specifications before Revit dispatch | `CadSourceSpec` | `GateEvaluationResult` (Valid/Invalid + Violations FF1-FF6) | Throws `FailFastCoordinationException` | `HPBim.Core/Coordination/FailFastGate.cs` |
| 4 | Core Coordination | Coordination Tolerances | Strict tolerance limits: Section 0.0mm, Elevation 1.0mm, Plan 1.0mm, Pile Center 2.0mm | Structural measurements | Evaluation boolean | Flags out-of-tolerance elements | `HPBim.Core/Coordination/CoordinationTolerances.cs` |
| 5 | Client Pipeline | Stage Script Generators | 8 generators producing C# scripts for Levels, Grids, Columns/Beams/Floors, Walls, Doors/Windows, Rooms, MEP, Rebar | `BimModel` | `StageScriptPayload` (Script string, Argument dictionary) | Produces syntactically valid C# scripts | `HPBim.Client/ScriptGenerators/` |
| 6 | Client Pipeline | Pipeline Engine | Orchestrates sequential execution or atomic dry-run rollback over Named Pipe | `BimModel`, `BimPipelineOptions` | `BimPipelineResult` with stage results | Returns failure on script error | `HPBim.Client/Pipeline/BimPipelineEngine.cs` |
| 7 | Client CLI | CLI Commands | Executable console commands: `validate`, `generate`, `verify`, `estimate` | Command-line arguments (`--input`, `--dry-run`) | Console tables, exit codes (0 = Success, 1 = Invalid, 2 = Missing Arg) | Prints error banner, returns non-zero exit code | `HPBim.Cli/Commands/` |
| 8 | Client Verification | Revit Model Verifier | Queries active Revit document element counts via read-only script | `BimModel`, Revit Pipe | `ModelVerificationReport` (Counts comparison) | Throws `RevitPipeException` on script failure | `HPBim.Client/Verification/RevitModelVerifier.cs` |
| 9 | Core Serialization | BimModel Serializer | JSON serializer/deserializer for `BimModel` via System.Text.Json | JSON string or file path | `BimModel` instance | Throws `JsonException` on invalid JSON syntax | `HPBim.Core/Serialization/BimModelSerializer.cs` |

### 4.2 Edge Cases Discovered
| # | Feature | Input | Observed Behavior |
|---|---|---|---|
| 1 | `BimModelSerializer.Deserialize` | Empty JSON `{}` | Silently deserializes into an empty `BimModel` with empty lists instead of throwing a validation error. |
| 2 | `ValidateCommand.ExecuteAsync` | Raw CAD export JSON or `{}` | `QcEngine` evaluates 0 elements, marks report as valid, writes "TRẠNG THÁI: SẠCH 100%" and returns exit code 0. |
| 3 | `BimPipelineEngine.ExecuteAsync` | Dry-run where bridge returns `RolledBack == false` | Pipeline reports success if no exception was thrown, failing to detect unconfirmed rollback. |
| 4 | `RevitModelVerifier.VerifyAsync` | Model with correct element counts but wrong column dimensions (e.g. 200x200 vs 220x220) | Reports `IsMatch == true` because it only compares integer counts of elements. |
| 5 | `RevitPipeClient.ExecuteAsync` | Transport timeout after packet sent to Revit | Throws `RevitPipeException` with `IsTimeout = true`; does not record `OutcomeUnknown` or support recovery. |
| 6 | `FakeRevitPipeClient` | `IRevitPipeClient` updated with `CancelAsync` | Fails compilation with `CS0535` because fake mock was not updated concurrently with interface. |
| 7 | `FailFastGate.EvaluateCadSource` | Top elevation = `double.NaN` or `double.PositiveInfinity` | Correctly caught and rejected with violation FF3 by FailFastGate. |
| 8 | `GradeBeamGridSnapper` | Beam centerline 45mm away from grid intersection | Snaps cleanly to grid intersection within tolerance; halts if distance exceeds threshold. |

---

## 5. Clean Code Governance Compliance

All code in `HPBim` is governed by the repo's Clean Code Standards: `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `host-appendix/revit.md`, and `docs/architecture/DEPENDENCY_RULES.md`.

### 5.1 Method and Class Length Triggers
- **Method Length Limits (Rule M3)**:
  - Body > ~20 lines: Review trigger (look again).
  - Body > 50 lines: Must be formally justified in review.
  - Body > 100 lines: Mandatory split unless it is a flat, cohesive table/switch mapping.
  - Runtime tools/embedded seeds: Block or local method > 50 lines triggers warning **Q-W1**.
- **Class and File Length Limits (Rule C2)**:
  - File > 300 lines: Triggers cohesion review. A class with a single, cohesive purpose may remain whole.
  - ViewModel > 250 lines: Review trigger.
  - Runtime scripts and embedded seeds: Script text > 300 lines is a **blocking compilation error Q-B3**.
- **Nesting and Parameters**:
  - Control flow nesting > 3 levels: Review warning **Q-W2**.
  - Method parameters > 4: Design question (introduce parameter object) (Rule M4).
  - Boolean parameter switching behavior: Strictly forbidden (Rule M5 / **Q-W4**). Use separate named methods or meaningful enums.

### 5.2 Dependency Rules & Solution Isolation
- **MCP Folder Isolation (Rule K6 / DEPENDENCY_RULES.md)**:
  - `HPBim/` depends **ONLY on `McpShared/`** (`HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Server.Core`).
  - `HPBim` must **NEVER reference other host folders**: no references to `HPAutoCad/`, `HPRebar/`, `HPCivil3d/`, `HPNavis/`, etc.
  - No circular dependencies permitted.
- **Core Purity (Rule D1)**:
  - `HPBim.Core` targets `netstandard2.0`.
  - Zero references to `Autodesk.Revit.*`, `System.Windows.*`, `System.Drawing.*`, `Serilog.*`, or native COM wrappers.
  - Only BCL + `Polyfill` + `System.Text.Json` allowed.
- **Additive Wire Contracts (Rule P9)**:
  - Any contract modifications in `McpShared/HPRebar.Mcp.Contracts` must be strictly additive so older bridges/servers continue working without breaking changes.

### 5.3 Error Handling & Code Quality Rules
- **Rule 0 — Golden Rule of Multi-Agent Coordination**:
  - *"Khi chưa đủ cơ sở để kết luận, hãy dừng lại và báo chưa đủ thông tin. Không được suy đoán để hoàn thành công việc. Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng."*
  - Rejection of heuristic guessing, line pairing, or silent default fallbacks.
- **Commented-Out Code (Rule CM3 / Q-B1)**:
  - Zero commented-out code allowed. Any occurrence is a **blocking error**.
- **Exception Handling (Rule Q-B2 / Q-W5)**:
  - No empty `catch` blocks without an explicit comment explaining the rationale (Q-B2 blocking error).
  - No swallowing `catch (Exception)` that neither rethrows, returns, nor records the caught exception (Q-W5 warning).
- **Exception Types (Rule RV5)**:
  - Caller mistakes must throw `ArgumentException` or `ArgumentOutOfRangeException`.
  - Host/bridge/pipe failures must throw `RevitPipeException` or `InvalidOperationException`.

---

## 6. R3 (P2 Architectural Proposal) Outline

The user specification mandates drafting a high-level Design Proposal for future advanced capabilities (P2):
1. **Two-Way Change Tracking (Revit ↔ CAD/Ledger)**
2. **Background Queue Architecture**
3. **Large-Scale Batching & Memory Safety**

Below is the structured technical outline for the R3 Design Proposal:

```markdown
# Architectural Design Proposal: Advanced BIM Synchronization, Background Queue & Large-Scale Batching (P2)

## 1. Executive Summary & Vision
- Seamless, bidirectional synchronization between CAD structural drawings, coordination ledger, and live Revit 2026 models.
- Enterprise-grade resilience: handling models with 50,000+ elements without UI freezing, transport timeouts, or memory exhaustion.

## 2. Two-Way Change Tracking Architecture (Revit ↔ CAD/Ledger)
### 2.1 Revit-Side Mutation Interception
- Integration with Revit Dynamic Model Update (DMU) via `IUpdater` and `DocumentChangedEventArgs`.
- Intercepting user edits: element movement, deletion, cross-section resizing, level shifts.
- Capturing changed parameter IDs and comparing against original Composite Source Key metadata.
### 2.2 Delta Engine & Three-Way Merging
- Model representations: CAD Source Spec (Base) vs Coordination Ledger (Target) vs Revit Live Model (Working).
- Delta calculation algorithm: Identifying Additions, Modifications, Deletions, and External Renaming.
- Conflict detection matrix: CAD revision vs Revit manual edit.
### 2.3 Automated RFI & Ticket Generation
- When a conflict occurs (e.g. beam resized in Revit while CAD drawing remains unchanged):
  - Mark element state as `Conflicted` in `bim_coordination_ledger.json`.
  - Automatically emit an RFI (Request For Information) record in the ledger (`RfiRecord`).
  - Freeze element from automated overwrites until resolved by the Lead Coordinator.

## 3. Background Queue & Asynchronous Worker Architecture
### 3.1 Pipe Transport Decoupling
- Elimination of synchronous blocking CLI calls over the Named Pipe.
- Asynchronous Job Submission: CLI pushes an `ExecutionJob` to a local SQLite/file-backed persistent queue.
- Job states: `Queued` -> `InFlight` -> `Verifying` -> `Committed` -> `Reconciled` / `Failed`.
### 3.2 Revit ExternalEvent Worker Loop
- Implementation of a dedicated background daemon in `HPRebar.McpBridge` utilizing `IExternalEventHandler`.
- Scheduled execution during Revit application idle cycles without starving the UI thread.
- Configurable execution budgets (e.g. max 500ms per idle tick) to keep Revit responsive.
### 3.3 Telemetry, Progress Streaming & Cancellation
- Bidirectional progress streaming over Named Pipe using JSON-RPC notifications.
- Immediate cancellation via cooperative `CancellationToken` and `revit.cancel` RPC calls.
- Dead-letter queue (DLQ) for poisoned payloads and fatal schema corruptions.

## 4. Large-Scale Batching & Memory Safety
### 4.1 Zone & Category Partitioning Strategy
- Spatial decomposition: Dividing massive projects by Level and Structural Grid Bounding Boxes (e.g. Zone A, Zone B).
- Category sequencing: Levels -> Grids -> Columns -> Beams -> Slabs -> Walls -> Openings -> Rooms -> MEP -> Rebar.
### 4.2 Transaction Chunking & Undo Buffer Throttling
- Revit transactions open for too long accumulate large undo stacks, consuming gigabytes of RAM.
- Chunking algorithm: Grouping operations into batches of 100–250 elements per atomic transaction.
- Explicit `doc.Regenerate()` throttling at chunk boundaries to prevent geometric cache bloat.
### 4.3 Checkpointed Execution & Resume Capability
- After each chunk commits, record a checkpoint receipt in Revit Extensible Storage (`DataStorage`).
- If execution is aborted (power outage, crash, network disconnect):
  - Next execution inspects latest checkpoint receipt.
  - Automatically skips previously committed chunks (`NoOp`).
  - Resumes execution seamlessly from chunk N+1.

## 5. Security, Concurrency & Data Integrity
- Lockfile mechanism on `bim_coordination_ledger.json` preventing simultaneous agent write collisions.
- Audit trail: Every mutation signed with Agent ID, UTC timestamp, and git commit SHA.
```

---

## 7. Actionable Next Steps for Implementation Agents

1. **Fix `FakeRevitPipeClient.cs`**: Implement `CancelAsync` to restore 100% green build for `HPBim.Client.Tests`.
2. **Implement Schema Validation**: Add `SchemaValidator` in `HPBim.Core.Serialization` and integrate it into `ValidateCommand` and `BimModelSerializer`.
3. **Unify Execution Plan (IR)**: Introduce `BimExecutionPlan` and `BimExecutionPlanBuilder` shared by Preview and Apply.
4. **Implement Rollback Verification**: Enforce `RolledBack == true` checks in `BimPipelineEngine` for all dry-runs.
5. **Implement Idempotency & Extensible Storage**: Add `CompositeSourceKey` and `RevitExtensibleStorageReceipt` services.
6. **Implement Measured Geometric Verification**: Replace count-only verification in `RevitModelVerifier` with direct measurement against `CoordinationTolerances`.
