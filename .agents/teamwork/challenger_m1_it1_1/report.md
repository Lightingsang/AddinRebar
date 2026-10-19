# Challenge Report: Milestone M1 (BimModelSchemaValidator & Execution Plan IR)

**Author**: `challenger_m1_it1_1` (Teamwork Preview Challenger: Critic & Specialist)  
**Date**: 2026-10-07  
**Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m1_it1_1\`  
**Target Milestone**: M1 (P0 Core: Baseline Repair, Schema Validation & Execution Plan IR)  
**Reference Code**:
- `HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.cs`
- `HPBim/src/HPBim.Core/Serialization/BimModelSerializer.cs`
- `HPBim/src/HPBim.Cli/Commands/ValidateCommand.cs`
- `HPBim/src/HPBim.Cli/Commands/GenerateCommand.cs`
- `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.cs`
- `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs`

---

## Challenge Summary

**Overall risk assessment**: **CRITICAL**

The worker implemented a baseline `BimModelSchemaValidator` and Execution Plan IR suite, fixing the CS0535 compilation error. However, adversarial empirical testing proves that **raw CAD payloads and invalid/degenerate BIM objects CAN slip past validation and report PASS**. Furthermore, a critical runtime serialization mismatch in the Execution Plan IR causes the unified C# Revit script to silently mark all elements as `NoOp` (creating zero elements) while reporting success.

---

## Challenges

### [Critical] Challenge 1: Case-Sensitive & Incomplete CAD Signatures Allow Raw CAD Payloads to Slip Past Validation

- **Assumption challenged**: `CheckRawCadSignatures` reliably detects and rejects raw CAD JSON files.
- **Attack scenario**:
  1. `CadRootSignatures` only contains 8 hardcoded lowercase strings: `["cad", "blockName", "entities", "layers", "dwg", "handles", "modelSpace", "polylines"]`.
  2. The code performs case-sensitive matching: `root.TryGetProperty(sig, out _)`. System.Text.Json `TryGetProperty` is strictly ordinal. A CAD export using uppercase or PascalCase properties (`"CAD"`, `"DWG"`, `"Entities"`, `"BlockName"`) is completely ignored by `CheckRawCadSignatures`.
  3. Real CAD export files in the repository (e.g. `scratch/cad_columns_extracted.json`, `scratch/cad_footings_raw.json`) use root properties like `"drawing"`, `"blocks"`, `"totalPolylines"`, `"allPolylines"`, `"blockDefinitionName"`, none of which are in `CadRootSignatures`.
  4. If such a CAD payload includes basic levels and an element or grid (standard in AutoCAD Architectural or Civil 3D exports), `BimModelSchemaValidator.Validate()` returns `IsValid = true` (PASS) with 0 errors.
- **Blast radius**: Raw CAD files bypass pre-flight schema validation in `hpbim validate` and `hpbim generate`, reaching downstream Revit generation and violating the primary user requirement (R1: "từ chối dứt khoát raw CAD JSON hoặc file sai định dạng, không deserialize thành BimModel rỗng rồi báo PASS").
- **Mitigation**:
  1. Perform case-insensitive enumeration of all root properties (`root.EnumerateObject()`) with `StringComparer.OrdinalIgnoreCase`.
  2. Expand CAD signatures to include `"drawing"`, `"blocks"`, `"blockDefinitionName"`, `"totalPolylines"`, `"allPolylines"`, `"dxf"`, `"modelSpace"`, `"paperSpace"`.
  3. Strictly enforce `requireStrictDiscriminant = true` by default (see Challenge 3).

### [Critical] Challenge 2: Execution Plan IR Enum Integer Serialization Silently Breaks Revit Script Execution

- **Assumption challenged**: The Execution Plan IR cleanly unifies Preview and Apply via `BimPlanRunnerScriptTemplate`.
- **Attack scenario**:
  1. In `BimPlanRunnerScriptTemplate.cs`, the Revit script parses item categories using:
     ```csharp
     string category = item.Require("category");
     if (category == "Level") ...
     else if (category == "Grid") ...
     else if (category == "Column") ...
     ```
  2. In `BimPipelineEngine.cs` (line 186), the request arguments are created using:
     `Args: BridgeJson.ToElement(plan)`
  3. `BridgeJson` serializes enums as integer numbers (`CoordinationCategory.Level == 8`).
  4. When `ScriptArgs` evaluates `item.Require("category")`, it reads `"8"`.
  5. In the script, `"8" != "Level"`, `"8" != "Grid"`, `"8" != "Column"`, etc.
  6. **Every single item in the plan falls through to `else { status = "NoOp" }`**.
  7. The script reports success with 0 created elements and 0 errors.
- **Blast radius**: The entire single execution plan pipeline fails to generate any elements in Revit while reporting 100% success.
- **Mitigation**:
  In `ExecutionPlanBuilder` or when passing arguments to `BridgeJson`, ensure `CoordinationCategory` is serialized as its string representation (using `JsonStringEnumConverter`), or serialize `ElementPlanItem` with explicit string category properties.

### [High] Challenge 3: Strict Discriminant Disabled by Default Permits Non-BIM Payloads Without Artifact Metadata

- **Assumption challenged**: Payloads must be authenticated as `HPBim.Model.v1`.
- **Attack scenario**:
  `BimModelSchemaValidator.Validate(json, bool requireStrictDiscriminant = false)` defaults to `false`.
  `ValidateCommand.cs` (line 43), `GenerateCommand.cs` (line 44), and `BimModelSerializer.DeserializeValidated` (line 100) all call `Validate` without enabling `requireStrictDiscriminant`.
  As a result, any JSON payload completely omitting `artifactType` and `schemaVersion` is accepted with a minor warning and reports `IsValid = true`.
- **Blast radius**: Arbitrary non-BIM JSON files can be processed without valid schema provenance.
- **Mitigation**:
  Default `requireStrictDiscriminant` to `true` across `BimModelSchemaValidator`, `BimModelSerializer`, and all CLI commands. Treat missing metadata as a blocking error `ERR_MISSING_ARTIFACT_TYPE`.

### [High] Challenge 4: Grids are Completely Unvalidated (`ValidateGrids` Omitted)

- **Assumption challenged**: Physical elements in `BimModel` undergo geometric sanity checks.
- **Attack scenario**:
  In `BimModelSchemaValidator.ValidateModel`:
  `int physicalCount = (model.Grids?.Count ?? 0) + ...`
  Grids count towards `physicalCount`, but `ValidateModel` NEVER calls any `ValidateGrids`.
  Consequently:
  - Grid with empty name (`Name = ""`) reports PASS.
  - Grid with degenerate zero length (`startPoint == endPoint`, length < 1mm) reports PASS.
  - Multiple grids with identical names (e.g. 5 grids named `"X1"`) report PASS.
  - Grids with `NaN` coordinates report PASS.
- **Blast radius**: Corrupt grid geometries slip into Revit, causing native Revit API exceptions during `Grid.Create`.
- **Mitigation**:
  Add `ValidateGrids(model.Grids, errors)` checking for non-empty names, uniqueness of grid names, finite coordinates, and minimum length $\ge 1.0\text{mm}$.

### [High] Challenge 5: Beams with Negative Width and Zero Height Pass Validation

- **Assumption challenged**: Beam geometry is strictly validated like columns and walls.
- **Attack scenario**:
  `ValidateColumns` checks `c.WidthMm <= 0 || c.DepthMm <= 0`.
  `ValidateWalls` checks `w.ThicknessMm <= 0`.
  `ValidateSlabs` checks `s.ThicknessMm <= 0`.
  `ValidateBeams` only checks:
  ```csharp
  if (string.IsNullOrWhiteSpace(b.Level) || !levels.Contains(b.Level)) ...
  if (b.LengthMm < 1.0) ...
  ```
  It **never checks `WidthMm` or `HeightMm`**. A beam with `widthMm: -300.0, heightMm: 0.0` passes validation.
- **Blast radius**: Invalid structural framing dimensions are dispatched to Revit.
- **Mitigation**:
  In `ValidateBeams`, add:
  ```csharp
  if (b.WidthMm <= 0 || b.HeightMm <= 0)
  {
      errors.Add($"[ERR_INVALID_GEOMETRY] Beam '{b.Id}' has invalid cross-section dimensions: {b.WidthMm}x{b.HeightMm}mm.");
  }
  ```

### [High] Challenge 6: Valid MEP Piping and Rebar Models are Falsely Rejected by `physicalCount`

- **Assumption challenged**: `physicalCount` correctly assesses whether a BIM model contains physical entities.
- **Attack scenario**:
  Lines 222-233 in `BimModelSchemaValidator.cs`:
  ```csharp
  int physicalCount = (model.Grids?.Count ?? 0)
                    + (model.Columns?.Count ?? 0)
                    + (model.Walls?.Count ?? 0)
                    + (model.Beams?.Count ?? 0)
                    + (model.Slabs?.Count ?? 0)
                    + (model.Rooms?.Count ?? 0)
                    + (model.Fixtures?.Count ?? 0);
  ```
  Notice that `model.Pipes` (MEP) and `model.Rebars` (Structural) are **omitted from `physicalCount`**.
  A valid MEP plumbing model with Levels and Pipes, or a structural rebar detailing model with Levels and Rebars, has `physicalCount == 0` and is falsely rejected with:
  `[ERR_NO_ELEMENTS] Model defines levels but contains zero structural, architectural, or MEP elements.`
- **Blast radius**: Legitimate MEP pipe models and structural rebar models cannot be validated or generated.
- **Mitigation**:
  Include `(model.Pipes?.Count ?? 0)` and `(model.Rebars?.Count ?? 0)` in `physicalCount`.

### [Medium] Challenge 7: Slabs with Empty Boundary Polygons Pass Validation

- **Assumption challenged**: Slabs must have valid geometric boundaries.
- **Attack scenario**:
  `ValidateSlabs` checks `Level` and `ThicknessMm <= 0`. It does NOT check `Boundary`. A slab with an empty boundary (`vertices: []`, area 0) passes validation.
- **Blast radius**: Revit `Floor.Create` fails when passed an empty curve loop.
- **Mitigation**:
  In `ValidateSlabs`, verify `s.Boundary != null && s.Boundary.Vertices.Count >= 3 && s.Boundary.Area > 0`.

### [Medium] Challenge 8: Zero-Height and Inverted Columns Pass Validation

- **Assumption challenged**: Columns have valid vertical spans.
- **Attack scenario**:
  `ValidateColumns` checks `levels.Contains(c.BaseLevel)` and `levels.Contains(c.TopLevel)`. It does not verify that `BaseLevel != TopLevel`, nor does it verify that `TopLevel` elevation is strictly greater than `BaseLevel` elevation.
- **Blast radius**: Zero-length or inverted vertical elements cause Revit API failures.
- **Mitigation**:
  Verify `c.BaseLevel != c.TopLevel` and check elevation ordering.

### [Medium] Challenge 9: Duplicate Element IDs Pass Validation

- **Assumption challenged**: Identifiers within a model are unique.
- **Attack scenario**:
  While `model.Levels` enforces uniqueness (`ERR_DUPLICATE_LEVEL`), columns, beams, walls, slabs, and fixtures never check for duplicate IDs. A model containing 10 columns all named `"C1"` passes validation.
- **Blast radius**: Loss of element provenance tracking and non-deterministic composite source key resolution in coordination ledger.
- **Mitigation**:
  Enforce ID uniqueness across all element collections.

---

## Stress Test Results

Executed empirically via `dotnet run --project HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj` (all 311 tests passed, confirming empirical reproduction):

| # | Test Scenario | Expected Behavior | Actual Behavior | Result |
|---|---|---|---|---|
| 1 | Baseline: Empty JSON `{}` & null | Must reject (`ERR_EMPTY_MODEL`) | Rejected (`ERR_EMPTY_MODEL`) | PASS |
| 2 | Baseline: Malformed JSON syntax | Must reject (`ERR_INVALID_JSON`) | Rejected (`ERR_INVALID_JSON`) | PASS |
| 3 | Baseline: Non-object root `[1, 2]` | Must reject (`ERR_INVALID_ROOT`) | Rejected (`ERR_INVALID_ROOT`) | PASS |
| 4 | Baseline: Corrupted version & artifactType | Must reject (`ERR_UNSUPPORTED_VERSION`) | Rejected (`ERR_UNSUPPORTED_VERSION`) | PASS |
| 5 | Baseline: Missing levels & broken reference | Must reject (`ERR_MISSING_LEVELS` / `ERR_BROKEN_REFERENCE`) | Rejected | PASS |
| 6 | **Adversarial**: Raw CAD with uppercase properties (`"CAD"`, `"DWG"`) | **Must reject** | **SLIPPED PAST (Reports PASS)** | **FAIL** (Vulnerability proven) |
| 7 | **Adversarial**: Raw CAD drawing with `"drawing"` & `"blocks"` | **Must reject** | **SLIPPED PAST (Reports PASS)** | **FAIL** (Vulnerability proven) |
| 8 | **Adversarial**: Missing `artifactType` & `schemaVersion` (non-strict) | **Must reject** | **SLIPPED PAST (Reports PASS)** | **FAIL** (Vulnerability proven) |
| 9 | **Adversarial**: Degenerate grid (empty name, 0 length) | **Must reject** | **SLIPPED PAST (Reports PASS)** | **FAIL** (Vulnerability proven) |
| 10 | **Adversarial**: Duplicate grid names (`X1`, `X1`) | **Must reject** | **SLIPPED PAST (Reports PASS)** | **FAIL** (Vulnerability proven) |
| 11 | **Adversarial**: Beam with negative width (-300mm) & zero height | **Must reject** | **SLIPPED PAST (Reports PASS)** | **FAIL** (Vulnerability proven) |
| 12 | **Adversarial**: Slab with empty boundary polygon | **Must reject** | **SLIPPED PAST (Reports PASS)** | **FAIL** (Vulnerability proven) |
| 13 | **Adversarial**: Column with BaseLevel == TopLevel (0 height) | **Must reject** | **SLIPPED PAST (Reports PASS)** | **FAIL** (Vulnerability proven) |
| 14 | **Adversarial**: Duplicate column IDs (`C1`, `C1`, `C1`) | **Must reject** | **SLIPPED PAST (Reports PASS)** | **FAIL** (Vulnerability proven) |
| 15 | **Adversarial**: Valid MEP model with Levels and Pipes | **Must PASS** | **FALSELY REJECTED (`ERR_NO_ELEMENTS`)** | **FAIL** (Bug proven) |
| 16 | **Adversarial**: Valid Structural model with Levels and Rebars | **Must PASS** | **FALSELY REJECTED (`ERR_NO_ELEMENTS`)** | **FAIL** (Bug proven) |
| 17 | **Adversarial**: Execution plan category enum serialization (`HPBim.Client.Tests`) | Must serialize as String | **Serialized as Number (8), causes NoOp** | **FAIL** (Critical runtime bug proven) |

---

## Unchallenged Areas

- **Live Revit 2026 GUI Execution**: Tested headlessly via C# Roslyn AST, mock IPC, and unit testing platforms. Modeless/modal dialog interactions in Revit GUI were out of scope for headless test runner.
