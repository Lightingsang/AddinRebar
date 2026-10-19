# E2E Acceptance Test Suite: Tiers 3, 4 & 5 Test Design
**Project**: HPGrasshopper MCP Subsystem  
**Milestone**: M5 (Final Milestone: E2E Verification & Hardening)  
**Author**: `explorer_gh_m5_3`  
**Date**: 2026-10-10  
**Status**: COMPLETE DESIGN SPECIFICATION  

---

## 1. Executive Summary & Design Principles

### 1.1 Scope & Purpose
This document specifies the end-to-end acceptance test suites for **Tier 3** (Cross-Feature Combinations), **Tier 4** (Real-World Scenarios), and **Tier 5** (Adversarial Hardening) for the `HPGrasshopper` Model Context Protocol (MCP) subsystem. 
These test tiers validate that the 5 Core MCP Tools, 8 Dynamic Registry Meta Tools, 12 Embedded Seeds, Dual-Layer Safety System, and Named Pipe IPC infrastructure function reliably in complex production workflows, withstand deliberate adversarial tampering, and enforce zero-leakage safety guarantees.

### 1.2 Core Design Principles
1. **Opaque-Box Requirement-Driven Testing**: Derived strictly from user requirements in `ORIGINAL_REQUEST.md § 2026-10-10T06:56:11Z` and `TEST_INFRA.md`. Tests exercise the system via standard MCP protocols, wire contracts, and public bridge entry points without depending on unexposed internal methods.
2. **Deterministic & Headless Execution**: All test fixtures and execution runners execute 100% headless in automated CI/CD runners (`net10.0`, xUnit v3) with zero Windows modal error dialogs (`STATUS_HARD_ERROR` suppressed via `SetErrorMode`).
3. **Dual-Layer Safety Verification**: Every mutating workflow verifies two distinct rollback mechanisms:
   - Outer layer: Revit `TransactionGroup` rollback preventing persistent Revit element creation when `dryRun = true`.
   - Inner layer: Grasshopper `CanvasUndoScope` / `GH_UndoRecord` restoring canvas component GUIDs and wire topologies upon rollback or exception.
4. **Clean Code Governance Compliance**: Every planned test file is strictly $\le 300$ lines (Rule C2) and each test method is strictly $\le 50$ lines (Rule M3). Zero dead/commented code (Rule Q-B1) and zero unannotated empty catch blocks (Rule Q-B2).

---

## 2. Test Architecture & File Layout

### 2.1 Solution & Project Layout
The test cases are located within `HPGrasshopper.E2E.Tests` (`net10.0`, xUnit v3):

```
HPGrasshopper/HPGrasshopper.E2E.Tests/
├── HPGrasshopper.E2E.Tests.csproj
├── Contracts/
│   ├── GrasshopperE2EContracts.cs       (<= 150 lines)
│   └── GrasshopperE2EModels.cs          (<= 200 lines)
├── Harness/
│   ├── GrasshopperE2EFakeBridge.cs      (<= 280 lines)
│   └── GrasshopperE2ETestHarness.cs     (<= 280 lines)
├── Tier3_CrossFeature/
│   └── Tier3_CrossFeatureInteractionTests.cs (14 test cases, ~250 lines)
├── Tier4_RealWorldScenarios/
│   └── Tier4_RealWorldWorkloadTests.cs       (6 scenarios, ~260 lines)
└── Tier5_AdversarialHardening/
    ├── Tier5_RoslynAstMutationTests.cs       (10 test cases, ~210 lines)
    ├── Tier5_SeedParameterFuzzingTests.cs    (12 test cases, ~250 lines)
    ├── Tier5_SecurityOptInAndDenyListTests.cs (8 test cases, ~240 lines)
    └── Tier5_CancellationStressTests.cs     (6 test cases, ~200 lines)
```

### 2.2 Test Count Summary
| Tier | Description | Target Minimum | Designed Tests | File Allocation |
|---|---|:---:|:---:|---|
| **Tier 3** | Pairwise Combinatorial & Cross-Feature | $\ge 12$ | **14** | `Tier3_CrossFeatureInteractionTests.cs` |
| **Tier 4** | Real-World Application Scenarios (RW-01..06) | $\ge 6$ | **6** | `Tier4_RealWorldWorkloadTests.cs` |
| **Tier 5** | Adversarial Hardening (AST, Fuzz, Sec, Cancel) | Comprehensive | **36** | 4 modular files ($\le 300$ lines each) |
| **Total** | **Tiers 3, 4, and 5 Combined** | $\ge 18$ | **56** | **6 files** |

---

## 3. Tier 3: Pairwise Combinatorial & Cross-Feature Matrix

Tier 3 exercises pairwise interactions between distinct feature subsystems.

### 3.1 Interaction Matrix
| Interaction ID | Feature A | Feature B | Interaction Workflow Description |
|---|---|---|---|
| **INT-01** | `read_grasshopper_canvas` | `batch_edit_grasshopper_canvas` | Read canvas baseline -> Add components & connect wires -> Re-read canvas to verify node/wire increments. |
| **INT-02** | `batch_edit_grasshopper_canvas` | `run_grasshopper_player` | Update parameter slider value on canvas -> Run Player on definition -> Verify output reflects updated parameter. |
| **INT-03** | `run_grasshopper_player` | Dual Safety Rollback | Run Player with `dryRun = true` -> Verify Revit TransactionGroup rollbacks and canvas retains baseline state. |
| **INT-04** | Dynamic Tool Registry | Roslyn Compiler & AST | Propose custom tool -> Validate AST (line count, no forbidden namespace) -> Test in sandbox -> Publish to tool surface. |
| **INT-05** | Dynamic Tool Registry | Tool Execution & Retirement | Execute published tool -> Retire tool via `manage_tool` -> Verify subsequent execution is rejected (-32001/-32601). |
| **INT-06** | `execute_grasshopper_code` | Cancellation Handling | Dispatch long-running script -> Trigger CancellationToken mid-execution -> Verify clean rollback & zero lock leakage. |
| **INT-07** | `batch_edit_grasshopper_canvas` | `CanvasUndoScope` Rollback | Batch edit multiple nodes with `dryRun = true` -> Verify `CanvasUndoScope` restores initial GUIDs and wires. |
| **INT-08** | `execute_grasshopper_code` | `grasshopper.context` | Execute script creating Revit levels -> Query context -> Verify live workspace and updated element count. |
| **INT-09** | `export_grasshopper_canvas_image`| `batch_edit_grasshopper_canvas` | Modify canvas layout/zoom via batch edit -> Export canvas image -> Validate PNG magic bytes `0x89 0x50 0x4E 0x47`. |
| **INT-10** | Seed Chaining | Dual Safety Rollback | Chain `create_levels_grids` then `generate_columns_beams` under single `dryRun=true` -> Verify 0 committed elements. |
| **INT-11** | Opt-In Security Switch | Named Pipe JSON-RPC | Toggle Opt-In switch OFF -> Attempt execution -> Receive `-32001` -> Toggle ON -> Re-execute succeeds immediately. |
| **INT-12** | Multi-Session Isolation | Correlated Cancellation | Client A (Session 1) and Client B (Session 2) dispatch concurrent work; Cancel Session 1; Session 2 completes intact. |
| **INT-13** | Seed Script Lifecycle | `ScriptQuality` Walker | Re-propose modified seed -> Run production compiler & ADR-0007 quality walker -> Check 0 errors / 0 warnings. |
| **INT-14** | Canvas Wire Replacement | LIFO Rollback Ordering | Replace wires A->T, B->T, C->T on same port -> Rollback restores initial connection A->T (not intermediate B->T). |

---

### 3.2 Tier 3 Test Cases Specification

#### TC-T3-INT-01: Canvas Read + Batch Edit Verification
- **Features**: `read_grasshopper_canvas`, `batch_edit_grasshopper_canvas`
- **Setup**: Initialize fake bridge with empty active canvas.
- **Action**:
  1. Call `read_grasshopper_canvas` to record baseline (0 nodes, 0 wires).
  2. Call `batch_edit_grasshopper_canvas` to add `NumberSlider` (X=100, Y=100) and `Panel` (X=300, Y=100), and wire output port 0 to input port 0.
  3. Call `read_grasshopper_canvas` again.
- **Assertions**:
  - `result.NodesCount == 2`
  - `result.WiresCount == 1`
  - Added node IDs match returned GUIDs from batch edit.

#### TC-T3-INT-02: Batch Edit Parameter + Run Player Solve
- **Features**: `batch_edit_grasshopper_canvas`, `run_grasshopper_player`
- **Setup**: Open sample definition `ParametricTower.gh`.
- **Action**:
  1. Modify slider input `"FloorCount"` from 10 to 25 via `batch_edit_grasshopper_canvas`.
  2. Invoke `run_grasshopper_player` on the active definition with `runEvaluation = true`.
- **Assertions**:
  - `playerResult.Success == true`
  - Output parameter `"TotalHeight"` equals $25 \times 3.8 = 95.0\text{ m}$.

#### TC-T3-INT-03: Run Player with DryRun Dual Safety Rollback
- **Features**: `run_grasshopper_player`, Dual Safety System
- **Setup**: Load definition mutating both canvas parameters and Revit levels.
- **Action**:
  1. Execute `run_grasshopper_player` with `dryRun = true`.
- **Assertions**:
  - `result.Success == true`
  - `result.RolledBack == true`
  - Revit element count delta equals 0.
  - Canvas modified parameter restored to initial value.

#### TC-T3-INT-04: Tool Registry Dynamic Lifecycle (Propose -> Test -> Publish)
- **Features**: Dynamic Registry, Roslyn ScriptCompiler, ToolValidator
- **Setup**: Prepare C# script calculating structural span deflection.
- **Action**:
  1. Submit via `propose_tool`: check AST quality, line count $\le 300$, transaction = "auto".
  2. Submit via `test_tool`: execute in sandbox with `dryRun = true`.
  3. Submit via `publish_tool`: promote status from `proposed` to `published`.
- **Assertions**:
  - Proposed tool passes validation with 0 errors.
  - Sandbox test reports `Success = true`, `RolledBack = true`.
  - Tool catalog contains published tool and responds to invocation.

#### TC-T3-INT-05: Dynamic Registry Tool Retirement Rejection
- **Features**: Dynamic Registry, MCP Tool Surface
- **Setup**: Tool `custom_beam_calc` published and active.
- **Action**:
  1. Invoke tool -> succeeds.
  2. Invoke `manage_tool` with `action = "retire"`.
  3. Attempt invocation of `custom_beam_calc`.
- **Assertions**:
  - Subsequent execution returns error code `-32001` or `-32601` (tool not available).
  - Status in registry reflects `retired`.

#### TC-T3-INT-06: Script Guard AST Validation + Cancellation Mid-Flight
- **Features**: `execute_grasshopper_code`, Cancellation, Transaction Scope
- **Setup**: Bridge connected with execution enabled.
- **Action**:
  1. Submit script performing iterative geometric calculation with `CancellationTokenSource`.
  2. Cancel token 10ms into execution.
- **Assertions**:
  - Execution returns `Success = false` and `RolledBack = true`.
  - No orphan transactions remain open in Revit.
  - Bridge thread releases lock (`_busy == 0`).

#### TC-T3-INT-07: Canvas Batch Edit + Undo Rollback Topology Integrity
- **Features**: `batch_edit_grasshopper_canvas`, `CanvasUndoScope`
- **Setup**: Live canvas with 3 existing nodes and 2 wires.
- **Action**:
  1. Execute batch edit adding 2 nodes, removing 1 node, and creating 2 new wires with `dryRun = true`.
- **Assertions**:
  - `result.RolledBack == true`.
  - Removed node restored with original GUID.
  - Added nodes purged from canvas.
  - Initial 2 wires intact; added wires removed.

#### TC-T3-INT-08: Execute Code with Revit Transaction + Context Refresh
- **Features**: `execute_grasshopper_code`, `grasshopper.context`
- **Setup**: Live session attached to active Revit document.
- **Action**:
  1. Execute script creating 3 levels (`dryRun = false`).
  2. Query `grasshopper.context`.
- **Assertions**:
  - `execResult.Success == true`.
  - Context `DocumentElementCount` increments by 3.
  - Context reports `IsLiveCanvasAttached == true` and active workspace name.

#### TC-T3-INT-09: Export Canvas Image Post Batch Edit
- **Features**: `export_grasshopper_canvas_image`, `batch_edit_grasshopper_canvas`
- **Setup**: Active canvas.
- **Action**:
  1. Batch edit adds cluster of 4 math components at coordinates (500, 500).
  2. Invoke `export_grasshopper_canvas_image` with `zoomToFit = true`.
- **Assertions**:
  - Export returns non-empty byte payload.
  - First 4 bytes match PNG signature `[0x89, 0x50, 0x4E, 0x47]`.

#### TC-T3-INT-10: Seed Pipeline Chaining under DryRun
- **Features**: Seeds `create_levels_grids` and `generate_columns_beams`
- **Setup**: Bridge connected to mock Revit document.
- **Action**:
  1. Invoke `create_levels_grids` (elevations: [0, 4, 8], grids: [6, 6]) with `dryRun = true`.
  2. Invoke `generate_columns_beams` (using grid coordinates) with `dryRun = true`.
- **Assertions**:
  - Both operations succeed with `RolledBack = true`.
  - Committed element count in Revit remains exactly 0.

#### TC-T3-INT-11: Opt-In Security Switch over Named Pipe
- **Features**: Modeless UI Opt-In Gate, Named Pipe Dispatcher
- **Setup**: Pipe listener active on `hpgrasshopper-mcp-2026`.
- **Action**:
  1. Set `ExecutionEnabled = false`.
  2. Send `grasshopper.execute` -> verify error `-32001`.
  3. Set `ExecutionEnabled = true`.
  4. Send `grasshopper.execute` -> verify success.
- **Assertions**:
  - Blocked execution returns exact message referencing "Allow AI Grasshopper execution".
  - Unblocked execution executes immediately without connection reset.

#### TC-T3-INT-12: Multi-Session Isolation with Correlated Cancellation
- **Features**: `ICorrelatedCancelExecutor`, Named Pipe Server
- **Setup**: Single bridge listener, two client sessions (`Session-A`, `Session-B`).
- **Action**:
  1. Client A dispatches long-running task with SessionId="A".
  2. Client B dispatches short task with SessionId="B".
  3. Client A sends `grasshopper.cancel` for task A.
- **Assertions**:
  - Task A terminates with cancellation.
  - Task B executes and completes with `Success = true`.
  - No cancellation leakage between sessions.

#### TC-T3-INT-13: Seed Upgrade & Production Script Quality Compliance
- **Features**: Embedded Seeds, ScriptQuality Walker (ADR-0007)
- **Setup**: Load source of seed `facade_panelization`.
- **Action**:
  1. Compile seed using `GrasshopperCompilerReferences`.
  2. Run `ScriptQuality` walker AST analyzer.
- **Assertions**:
  - 0 compilation errors.
  - 0 quality errors (lines $\le 300$, no empty catch, no commented code).

#### TC-T3-INT-14: Wire Replacement LIFO Ordering Rollback
- **Features**: `batch_edit_grasshopper_canvas`, `CanvasUndoScope`
- **Setup**: Canvas with node N1 (out 0) wired to Target T (in 0).
- **Action**:
  1. In a single batch, wire N2 (out 0) -> T (in 0), then N3 (out 0) -> T (in 0) with `dryRun = true`.
- **Assertions**:
  - Rollback applies actions in LIFO order.
  - Final canvas has N1 (out 0) wired to T (in 0). Intermediate N2 wire is discarded.

---

## 4. Tier 4: Real-World Application Workload Scenarios

Tier 4 implements 6 comprehensive, realistic engineering workflows specified in `TEST_INFRA.md`.

```
+-----------------------------------------------------------------------------------+
|                        TIER 4 REAL-WORLD APPLICATION WORKLOADS                    |
+-------------------+---------------------------------------------------------------+
| RW-01             | Parametric Tower Structure (Grids, Columns, Floors, Solve)   |
| RW-02             | Facade Adaptive Panelization (UV Surface Divide, Adaptive Cmp)|
| RW-03             | Automated Rebar Detailing (Curves, Stirrup Dist, TransGroup)  |
| RW-04             | DryRun Safety Verification (Canvas & Revit 100% Rollback)     |
| RW-05             | Tool Registry Dynamic Lifecycle (Propose, Test, Publish, Run) |
| RW-06             | Solar Radiation Analysis & DirectShape Mesh Bake              |
+-------------------+---------------------------------------------------------------+
```

---

### 4.1 Scenario RW-01: Parametric Tower Structure
- **Objective**: Generate a multi-story parametric concrete tower from structural grid definitions, column bases, beam framing, and slab perimeters.
- **Features Exercised**: Seeds `create_levels_grids`, `generate_columns_beams`, `create_floors_slabs`, and `read_grasshopper_canvas`.
- **Pipeline Workflow**:
  1. **Levels & Axis Grids**:
     - Input elevations: `[0.0, 3.8, 7.6, 11.4, 15.2, 19.0]` (6 levels, 5 stories).
     - Input grid spacings: `[6.0, 6.0, 6.0]` along X (4 grids: X1..X4), `gridLength = 24.0` along Y (grids Y1..Y4 spaced at 6m).
     - Execute `create_levels_grids`: creates 6 Level and 8 Grid elements.
  2. **Columns & Beams Framing**:
     - Column placement at $4 \times 4 = 16$ grid intersections for each of the 5 stories ($16 \times 5 = 80$ columns).
     - Perimeter & interior beams connecting adjacent column nodes ($24 \text{ beams/story} \times 5 = 120$ beams).
     - Execute `generate_columns_beams`: places 80 structural columns and 120 structural framing elements.
  3. **Floor Slabs Boundary**:
     - Boundary polygon: `[(0,0), (18,0), (18,18), (0,18)]` with thickness 200mm.
     - Execute `create_floors_slabs` at each elevation: creates 6 native Revit floor slabs.
  4. **Canvas State & Solver Verification**:
     - Call `read_grasshopper_canvas` to ensure all parametric solver nodes report status `Solved` with zero warning/error flags.
- **Verification Assertions**:
  - Committed element counts: 6 Levels, 8 Grids, 80 Columns, 120 Beams, 6 Slabs.
  - Zero geometry overlap or orphan transaction warnings.

---

### 4.2 Scenario RW-02: Facade Adaptive Panelization
- **Objective**: Subdivide a curved double-curvature facade envelope into a UV grid matrix and instantiate parametric adaptive panels.
- **Features Exercised**: Seed `facade_panelization`, Seed `curtain_grid_layout`, `run_grasshopper_player`.
- **Pipeline Workflow**:
  1. **Surface Geometry Setup**:
     - Envelope dimensions: `surfaceWidthMeters = 36.0`, `surfaceHeightMeters = 18.0`.
     - Subdivisions: `uDivisions = 12`, `vDivisions = 6` ($12 \times 6 = 72$ panel units).
  2. **UV Parametric Discretization**:
     - Invoke `facade_panelization` with family `"Adaptive-Quad-Panel"`.
     - Seed generates 72 quad point arrays ($4 \times 72 = 288$ adaptive placement points).
  3. **Curtain Grid Layout**:
     - Invoke `curtain_grid_layout` to align structural mullion centerlines along U and V iso-curves.
  4. **Player Execution with Overrides**:
     - Invoke `run_grasshopper_player` with parameter override `{"GlazingRatio": 0.65}`.
- **Verification Assertions**:
  - Exact count of 72 adaptive quad panels generated.
  - Panel corner coordinates strictly conform to boundary envelope $[0..36\text{ m}] \times [0..18\text{ m}]$.
  - Glazing area ratio evaluated and verified in solver output.

---

### 4.3 Scenario RW-03: Automated Rebar Detailing Pipeline
- **Objective**: Convert Grasshopper structural reinforcement curves into native 3D Revit Rebar elements with zoned stirrup spacing.
- **Features Exercised**: Seed `generate_rebar_curves`, Seed `generate_stirrup_distribution`, Revit `TransactionGroup`.
- **Pipeline Workflow**:
  1. **Host Beam Selection**:
     - Concrete beam $300 \times 600\text{ mm}$, clear span $L = 6000\text{ mm}$, cover $c = 30\text{ mm}$.
  2. **Longitudinal Rebar Generation**:
     - Top bars: 3 $\phi 20$ continuous with $90^\circ$ hooks into end columns.
     - Bottom bars: 3 $\phi 20$ continuous with standard anchorage.
     - Invoke `generate_rebar_curves` with 6 polyline curve sets.
  3. **Zoned Stirrup Distribution**:
     - Support dense zones: $L/4 = 1500\text{ mm}$ at each end with $\phi 8 @ 100\text{ mm}$ ($15 \text{ stirrups} \times 2 = 30$).
     - Mid-span sparse zone: $L/2 = 3000\text{ mm}$ with $\phi 8 @ 200\text{ mm}$ ($15 \text{ stirrups}$).
     - Invoke `generate_stirrup_distribution` with `denseSpacingMm = 100`, `sparseSpacingMm = 200`.
  4. **Atomic Transaction Commit**:
     - Entire detailing operation executes inside an atomic `TransactionGroup`.
- **Verification Assertions**:
  - Longitudinal rebar count equals 6.
  - Stirrup count equals 45.
  - All rebars hosted inside beam host element with valid `RebarBarType`.

---

### 4.4 Scenario RW-04: DryRun Safety & 100% Rollback Verification
- **Objective**: Prove zero-footprint safety guarantee across both Revit DB and Grasshopper canvas under `dryRun = true`.
- **Features Exercised**: `batch_edit_grasshopper_canvas`, `execute_grasshopper_code`, `CanvasUndoScope`, `RevitTransactionScope`.
- **Pipeline Workflow**:
  1. **Baseline Snapshot**:
     - Record initial Revit element count ($E_0$).
     - Record initial canvas component count ($C_0$) and wire count ($W_0$).
  2. **Multi-Host Mutation under DryRun**:
     - Canvas mutation: add 5 components (Sliders, Math, Panels) and connect 4 wires.
     - Revit mutation: execute script creating 2 Levels, 4 Columns, and 2 Walls.
     - Request parameter: `dryRun = true`.
  3. **Rollback Execution**:
     - Outer Revit `TransactionGroup` executes `RollBack()`.
     - Inner Grasshopper `CanvasUndoScope` executes `PerformRollback()`.
  4. **Post-Rollback Audit**:
     - Inspect Revit database: current element count must equal $E_0$.
     - Inspect Grasshopper canvas: component count must equal $C_0$; wire count must equal $W_0$.
- **Verification Assertions**:
  - `result.Success == true` and `result.RolledBack == true`.
  - $\Delta \text{RevitElements} == 0$.
  - $\Delta \text{CanvasComponents} == 0$ and $\Delta \text{Wires} == 0$.
  - Zero orphaned transaction locks.

---

### 4.5 Scenario RW-05: Tool Registry Dynamic Lifecycle
- **Objective**: Execute the complete dynamic tool authoring lifecycle from code drafting to sandbox testing, publication, execution, and retirement.
- **Features Exercised**: 8 Dynamic Registry meta tools (`propose_tool`, `test_tool`, `publish_tool`, `manage_tool`, `list_tools`, `get_tool`).
- **Pipeline Workflow**:
  1. **Draft Tool Definition**:
     - Name: `calculate_facade_shading_index`.
     - C# code calculates shading coefficient from incident angle and overhang depth.
  2. **Propose Tool**:
     - Submit via `propose_tool`. ToolValidator enforces line count $\le 300$, transaction mode `"none"`, no denied namespaces. Tool saved as `proposed`.
  3. **Sandbox Test Tool**:
     - Invoke `test_tool` with sample arguments. Sandbox runs script with `dryRun = true`.
  4. **Publish Tool**:
     - Invoke `publish_tool`. Status advances to `published`.
     - `list_tools` reflects updated catalog count ($24 \to 25$).
  5. **Execute Published Tool**:
     - Invoke tool via standard JSON-RPC tool call. Receives computed shading index.
  6. **Retire Tool**:
     - Invoke `manage_tool` with action `"retire"`. Status advances to `retired`.
     - Subsequent invocation is rejected with error code.
- **Verification Assertions**:
  - Tool passes each lifecycle gate in strict order.
  - SQLite registry records full audit trail of state transitions.

---

### 4.6 Scenario RW-06: Solar Radiation Analysis & DirectShape Bake
- **Objective**: Perform solar incident angle analysis on facade panels and bake high-radiation shading geometry into native Revit DirectShape elements.
- **Features Exercised**: Seed `evaluate_solar_radiation`, Seed `directshape_from_geometry`, Seed `query_revit_geometry`.
- **Pipeline Workflow**:
  1. **Solar Condition Definition**:
     - Sun vector at summer solstice noon: Azimuth $180^\circ$, Altitude $65^\circ \to$ normalized vector $(0.0, -0.4226, 0.9063)$.
     - Irradiance: $1000\text{ W/m}^2$.
  2. **Solar Radiation Evaluation**:
     - Query facade panel normals from 50 panels.
     - Invoke `evaluate_solar_radiation`: computes $\cos(\theta)$ and incident Watts/$\text{m}^2$ for each panel.
  3. **High-Radiation Filtering**:
     - Filter panels with incident radiation $> 700\text{ W/m}^2$ (e.g. 18 panels).
  4. **DirectShape Shading Fin Geometry Bake**:
     - Generate solid Brep louvre geometry in Grasshopper for the 18 panels.
     - Invoke `directshape_from_geometry` with `category = "GenericModel"`, `name = "Solar_Shading_Fin"`.
- **Verification Assertions**:
  - Solar radiation correctly calculated per panel.
  - Exactly 18 DirectShape elements created in Revit with `BuiltInCategory.OST_GenericModel`.

---

## 5. Tier 5: Adversarial Hardening Suite

Tier 5 applies aggressive stress, boundary violations, syntax mutations, fuzzing, and security bypass attempts.

### 5.1 Architecture & Modular File Allocation
To maintain strict compliance with Rule C2 ($\le 300$ lines per file), Tier 5 is partitioned into 4 specialized suites:

```
Tier 5 Adversarial Hardening Suites:
1. Tier5_RoslynAstMutationTests.cs       (10 tests) -> Syntax & AST rules
2. Tier5_SeedParameterFuzzingTests.cs    (12 tests) -> Extreme inputs & boundaries
3. Tier5_SecurityOptInAndDenyListTests.cs (8 tests)  -> Opt-In & Deny-list evasion
4. Tier5_CancellationStressTests.cs      (6 tests)  -> Concurrency & cancel stress
```

---

### 5.2 Suite 1: Roslyn AST & Compilation Stress (`Tier5_RoslynAstMutationTests.cs`)

| Test ID | Test Name | Attack / Condition Tested | Expected Outcome |
|---|---|---|---|
| **TC-T5-AST-01** | `LineCount_Boundary300And301` | Script with exactly 300 lines vs 301 lines. | 300 lines PASS; 301 lines FAILS with Q-B3 rule error. |
| **TC-T5-AST-02** | `EmptyCatch_WithAndWithoutReasonComment` | `catch { }` vs `catch { /* expected */ }`. | Unannotated fails (Q-B2); annotated with reason passes. |
| **TC-T5-AST-03** | `CommentedOutCode_Detection` | `// int x = 5;` vs `// Calculates beam span`. | Commented C# statements fail (Q-B1); prose comments pass. |
| **TC-T5-AST-04** | `SyntaxMutations_NoCompilerCrash` | Unclosed quotes, missing semicolons, unmatched braces. | Clean Roslyn diagnostic diagnostics, zero crash/panic. |
| **TC-T5-AST-05** | `DeepBinaryExpression_StackOverflowDefense` | 1,000 chained binary additions `1 + 1 + ... + 1`. | Parsed cleanly without `StackOverflowException`. |
| **TC-T5-AST-06** | `ToolValidator_ReservedNames_Rejected` | Candidate named `execute_grasshopper_code` or `propose_tool`. | ToolValidator rejects as reserved system tool. |
| **TC-T5-AST-07** | `ToolValidator_InvalidTransactionMode` | Candidate with transaction `"semi_auto"` or `"destructive"`. | ToolValidator rejects (must be `auto`, `manual`, or `none`). |
| **TC-T5-AST-08** | `DynamicToolRegistrar_MutatingTool_InjectsDryRun` | Dynamic tool with transaction `"auto"` or `"manual"`. | Generated input schema contains `properties.dryRun` (boolean). |
| **TC-T5-AST-09** | `DynamicToolRegistrar_ReadOnlyTool_OmitsDryRun` | Dynamic tool with transaction `"none"`. | Generated input schema omits `properties.dryRun`. |
| **TC-T5-AST-10** | `CompilerDirectives_LoadAndReference_Blocked` | Script containing `#r "foo.dll"` or `#load "bar.cs"`. | Blocked before compilation as unauthorized directive. |

---

### 5.3 Suite 2: Seed Parameter Fuzzing (`Tier5_SeedParameterFuzzingTests.cs`)

| Test ID | Seed Tested | Fuzzed Inputs | Expected Assertion |
|---|---|---|---|
| **TC-T5-FUZZ-01** | `run_grasshopper_player` | `""`, `"   "`, `null`, non-existent file path. | Throws `ArgumentException` with clear validation message. |
| **TC-T5-FUZZ-02** | `run_grasshopper_player` | File with extension `.dyn`, `.dwg`, `.txt`. | Throws `ArgumentException` (requires `.gh` or `.ghx`). |
| **TC-T5-FUZZ-03** | `batch_edit_grasshopper_canvas` | Coordinates `X = NaN`, `Y = Infinity`, `-Infinity`. | Throws `ArgumentException` ("Coordinates must be finite"). |
| **TC-T5-FUZZ-04** | `batch_edit_grasshopper_canvas` | Negative port indices `srcPort = -1`, `tgtPort = -100`. | Throws `IndexOutOfRangeException` ("Non-negative indices"). |
| **TC-T5-FUZZ-05** | `batch_edit_grasshopper_canvas` | Cyclic self-loop connection (same source & target GUID). | Throws `InvalidOperationException` (Self-loop rejected). |
| **TC-T5-FUZZ-06** | `search_tools` | Query string `null`, whitespace, or 10,000 characters. | Empty/whitespace handled gracefully; oversized clamped. |
| **TC-T5-FUZZ-07** | `search_tools` | Limit input `-50`, `0`, `500`, `int.MaxValue`. | Clamped strictly to valid range $[1..200]$. |
| **TC-T5-FUZZ-08** | `create_levels_grids` | Empty elevations array `[]`, negative spacings `[-5.0]`. | Schema validation fails or throws `ArgumentException`. |
| **TC-T5-FUZZ-09** | `generate_columns_beams` | Mismatched endpoints (`startX == endX` and `startY == endY`). | Zero-length beam rejected before Revit API call. |
| **TC-T5-FUZZ-10** | `create_floors_slabs` | Self-intersecting or non-closed polygon loop. | Geometric validator catches non-closed loop. |
| **TC-T5-FUZZ-11** | `generate_stirrup_distribution` | `denseSpacingMm = 0.0`, `sparseSpacingMm = -10.0`. | Throws `ArgumentOutOfRangeException` (spacing must be $> 0$). |
| **TC-T5-FUZZ-12** | JSON Serialization | Payload containing embedded null characters `\0` or BOM. | Handled cleanly by `BridgeJson` without corrupting parser. |

---

### 5.4 Suite 3: Security Opt-In & Deny-List Evasion (`Tier5_SecurityOptInAndDenyListTests.cs`)

| Test ID | Test Name | Attack Vector / Scenario | Expected Outcome |
|---|---|---|---|
| **TC-T5-SEC-01** | `ExecutionDisabled_ReturnsMinus32001` | Dispatch `grasshopper.execute` when Opt-In is unchecked. | Returns error code `-32001` with explicit user instruction. |
| **TC-T5-SEC-02** | `NonExecutionMethods_PermittedWhenDisabled` | Call `ping`, `context`, `cancel` when Opt-In is unchecked. | Allowed to execute (returns success, does not require opt-in). |
| **TC-T5-SEC-03** | `RapidOptInStateFlipping_ZeroLeakage` | Toggle `ExecutionEnabled` every 5ms across 50 concurrent calls. | 100% deterministic decision per call; zero unauthorized execution. |
| **TC-T5-SEC-04** | `ForbiddenNamespace_SystemIO_Blocked` | `System.IO.File.Delete("revit.rvt")`. | `ScriptGuard` detects `System.IO` and rejects script. |
| **TC-T5-SEC-05** | `ForbiddenNamespace_SystemDiagnostics_Blocked` | `System.Diagnostics.Process.Start("calc.exe")`. | `ScriptGuard` detects `System.Diagnostics` and rejects. |
| **TC-T5-SEC-06** | `DenyListEvasion_GlobalAlias_Blocked` | `global::System.IO.File.ReadAllText(...)`. | AST walker resolves global alias and blocks namespace. |
| **TC-T5-SEC-07** | `DenyListEvasion_ReflectionEmit_Blocked` | `System.Reflection.Emit.AssemblyBuilder`. | Blocked under reflection code generation deny-list. |
| **TC-T5-SEC-08** | `MalformedJsonRpc_SafetyRejection` | Truncated JSON, missing version, unescaped characters. | Handled safely by JSON-RPC reader without process termination. |

---

### 5.5 Suite 4: Stress Cancellation & Concurrency (`Tier5_CancellationStressTests.cs`)

| Test ID | Test Name | Concurrency / Cancellation Stress Condition | Expected Outcome |
|---|---|---|---|
| **TC-T5-CNC-01** | `ImmediateCancellation_BeforeExecution` | Cancel token immediately before dispatch. | Work item discarded before thread acquisition; 0 side-effects. |
| **TC-T5-CNC-02** | `MidFlightCancellation_TransactionRollback` | Cancel long solver execution midway. | TransactionGroup rolls back cleanly; no lock remains. |
| **TC-T5-CNC-03** | `CorrelatedCancellation_SessionIsolation` | Cancel Session 1 task; verify Session 2 task unaffected. | Session 1 aborts; Session 2 completes successfully. |
| **TC-T5-CNC-04** | `CancelOfCancel_NoDeadlock` | Send cancel request targeting another cancel request. | RequestDispatcher detects control method and safely ignores. |
| **TC-T5-CNC-05** | `ThreadRelease_BusyFlagHonesty` | Verify `_busy` flag remains 1 until scheduler fully halts. | Thread is never released prematurely; no overlapping executions. |
| **TC-T5-CNC-06** | `HighThroughputBurst_NoConnectionDrop` | Dispatch 100 ping/context requests in rapid succession. | All 100 requests handled cleanly without pipe buffer overflow. |

---

## 6. Clean Code Governance Compliance Matrix

Every file in the planned test suites is designed to satisfy the repository's clean code rules:

| Target File | Planned Lines | Limit | Planned Max Method Lines | Limit | Catch / Dead Code Status | Compliance |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| `Tier3_CrossFeatureInteractionTests.cs` | ~250 | $\le 300$ | 35 | $\le 50$ | 0 dead code, 0 unannotated catch | **PASS** |
| `Tier4_RealWorldWorkloadTests.cs` | ~260 | $\le 300$ | 45 | $\le 50$ | 0 dead code, 0 unannotated catch | **PASS** |
| `Tier5_RoslynAstMutationTests.cs` | ~210 | $\le 300$ | 30 | $\le 50$ | 0 dead code, 0 unannotated catch | **PASS** |
| `Tier5_SeedParameterFuzzingTests.cs` | ~250 | $\le 300$ | 25 | $\le 50$ | 0 dead code, 0 unannotated catch | **PASS** |
| `Tier5_SecurityOptInAndDenyListTests.cs` | ~240 | $\le 300$ | 40 | $\le 50$ | 0 dead code, 0 unannotated catch | **PASS** |
| `Tier5_CancellationStressTests.cs` | ~200 | $\le 300$ | 35 | $\le 50$ | 0 dead code, 0 unannotated catch | **PASS** |

---

## 7. Implementation Blueprint for Test Writers

When the test writer agents implement these designed test cases:
1. **Target Framework & Runner**: `net10.0`, `xunit.v3` (v3.1.0) with Microsoft Testing Platform runner (`global.json` pinned).
2. **References**:
   - `HPRebar.Mcp.Contracts`
   - `HPRebar.McpBridge.Core`
   - `HPRebar.Mcp.Server.Core`
   - Zero direct references to native `RhinoCommon.dll` or `Grasshopper.dll` in the test project to ensure 100% portable, headless execution.
3. **Execution Command**:
   ```bash
   dotnet test HPGrasshopper/HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj
   ```
4. **Target Filter Execution**:
   ```bash
   # Tier 3 only
   dotnet test HPGrasshopper/HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj --filter "FullyQualifiedName~Tier3"

   # Tier 4 only
   dotnet test HPGrasshopper/HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj --filter "FullyQualifiedName~Tier4"

   # Tier 5 only
   dotnet test HPGrasshopper/HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj --filter "FullyQualifiedName~Tier5"
   ```
