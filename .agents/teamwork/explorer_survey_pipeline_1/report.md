# Comprehensive Survey & Architectural Analysis Report: HPBim Pipeline & Coordination Ledger (R1 / P0 Truthful Results)

**Agent**: `explorer_survey_pipeline_1` (Teamwork Explorer)  
**Date**: 2026-10-07  
**Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_pipeline_1`  
**Reference Task**: Milestone R1 (P0 — Truthful Results & Preview/Apply Equivalence) from `ORIGINAL_REQUEST.md` (## 2026-10-07T13:49:28Z)  

---

## 1. Executive Summary

This survey report provides a deep-dive, read-only architectural investigation into the `HPBim` and `McpShared` codebases, focusing on the strict requirements of **R1 (P0 — Truthful Results & Preview/Apply Equivalence)**:
1. **Artifact & Schema Validation**: Raw CAD JSON and malformed JSON files currently deserialize into empty `BimModel` instances without error, passing `QcEngine.Validate` with 0 errors and reporting `[PASS] TRẠNG THÁI: SẠCH 100%` and exit code `0`.
2. **Single Source of Truth Execution Plan**: The codebase has two disconnected architectures: an 8-stage pipeline in `HPBim.Client` and over 20 fragmented CLI commands in `HPBim.Cli` embedding bespoke inline Roslyn scripts. Preview (Dry-Run) and Apply (Commit) diverge in behavior, structure, and view generation.
3. **Per-Element Granularity & Implicit Fallbacks**: Script results report coarse aggregate numbers (`createdCount = 10`), completely omitting per-element tracking (`Created`, `Updated`, `NoOp`, `Skipped`, `Failed`). Severe implicit fallbacks exist across multiple files (picking the first level in the document, first column/framing symbol, first plumbing fixture, arbitrary host elements, and silently skipping short or unresolvable entities).
4. **Rollback Verification & Bridge Status Honesty**: The Revit bridge (`ScriptRunner`) initiates a `TransactionGroup` for scripts, but `AutoDismissFailurePreprocessor` silently deletes all Revit warnings, discarding valuable diagnostics. In addition, `DocumentChangeCounter` records additions during inner transaction commits before rollback, and client-side timeouts throw without tracking `OutcomeUnknown`.
5. **Measured QA/QC Metrics & Hardcoded Literals**: In `FoundationQcEngine`, Step 8 is hardcoded to `true` (literal PASS). Position and span checks return `passed = true` with summary claims of "100% khớp CAD" when expected ground-truth dictionaries are `null`. In `RevitModelVerifier`, verification relies entirely on document-wide `GetElementCount()` rather than measured geometric verification.
6. **Current Build Baseline**: `HPBim.Core.Tests` runs 268/268 tests green. However, `HPBim.Client.Tests` currently fails to compile due to a missing interface member in `FakeRevitPipeClient.cs(14,43)` (`IRevitPipeClient.CancelAsync(CancellationToken)`), which must be addressed before integration test suites can run.

---

## 2. Solution Build Baseline & Compilation Findings

### 2.1 Test Execution Results
- **Command**: `dotnet test` from `HPBim/tests/HPBim.Core.Tests`
- **Result**: **PASS** (268 passed, 0 failed, 0 skipped, duration 699ms). All domain models, geometry, TCVN rules, and foundation QC checkers in `HPBim.Core` are fully functional and pass unit tests.
- **Command**: `dotnet test` from `HPBim` root (building `HPBim.slnx`)
- **Result**: **FAIL** (CS0535 compilation error).

### 2.2 Compilation Defect in `HPBim.Client.Tests`
- **Location**: `HPBim\tests\HPBim.Client.Tests\Fakes\FakeRevitPipeClient.cs:14`
- **Error**: `error CS0535: 'FakeRevitPipeClient' does not implement interface member 'IRevitPipeClient.CancelAsync(CancellationToken)'`
- **Root Cause**: `IRevitPipeClient` was recently updated with `Task<bool> CancelAsync(CancellationToken cancellationToken = default);` (defined in `HPBim\src\HPBim.Client\Connection\IRevitPipeClient.cs:52`), but the test fake `FakeRevitPipeClient` was not updated to implement this method.
- **Required Fix for Implementer**: Add the stub `public Task<bool> CancelAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);` to `FakeRevitPipeClient.cs`.

---

## 3. Deep Dive 1: Artifact & Schema Validation

### 3.1 Observation & Code Evidence
In `HPBim.Core.Serialization.BimModelSerializer`:
```csharp
// HPBim.Core\Serialization\BimModelSerializer.cs:49-50
var model = JsonSerializer.Deserialize<BimModel>(json, DefaultOptions);
return model ?? throw new JsonException("Deserialization returned null BimModel.");
```
In `HPBim.Core.Models.BimModel`:
```csharp
// HPBim.Core\Models\BimModel.cs:8-20
public sealed record BimModel
{
    public string ProjectName { get; init; } = string.Empty;
    public IReadOnlyList<LevelDef> Levels { get; init; } = Array.Empty<LevelDef>();
    public IReadOnlyList<AxisGrid> Grids { get; init; } = Array.Empty<AxisGrid>();
    public IReadOnlyList<ColumnDef> Columns { get; init; } = Array.Empty<ColumnDef>();
    public IReadOnlyList<WallDef> Walls { get; init; } = Array.Empty<WallDef>();
    public IReadOnlyList<BeamDef> Beams { get; init; } = Array.Empty<BeamDef>();
    public IReadOnlyList<SlabDef> Slabs { get; init; } = Array.Empty<SlabDef>();
    public IReadOnlyList<RoomDef> Rooms { get; init; } = Array.Empty<RoomDef>();
    public IReadOnlyList<MepFixtureDef> Fixtures { get; init; } = Array.Empty<MepFixtureDef>();
    public IReadOnlyList<PipeRunDef> Pipes { get; init; } = Array.Empty<PipeRunDef>();
    public IReadOnlyList<RebarDef> Rebars { get; init; } = Array.Empty<RebarDef>();
```
In `HPBim.Cli.Commands.ValidateCommand`:
```csharp
// HPBim.Cli\Commands\ValidateCommand.cs:37-43, 60-63
var model = await BimModelSerializer.DeserializeFromFileAsync(inputPath, ct).ConfigureAwait(false);
var qcEngine = new QcEngine();
var report = qcEngine.Validate(model);
RenderValidationSummary(report);
return report.IsValid ? 0 : 1;
...
if (report.IsValid)
{
    _formatter.WriteSuccess("TRẠNG THÁI: SẠCH 100%. Mô hình sẵn sàng dispatch tới Revit.");
}
```

### 3.2 Vulnerability Analysis: Silent Pass of Raw CAD JSON & Empty Files
1. **Unchecked Deserialization**:
   - `BimModel` initializes every list to `Array.Empty<T>()` and `ProjectName` to `string.Empty`.
   - `System.Text.Json` by default ignores unrecognized properties.
   - If an input file contains raw CAD JSON (such as `scratch/piles_cad_vs_revit_inspection.json`, which has root object `{ "cad": { "blockName": "Rec-TT", ... } }`), or an empty JSON object `{}`:
     - `JsonSerializer.Deserialize<BimModel>(json)` returns a valid, non-null `BimModel` instance where all collections are empty.
2. **QcEngine Bypass**:
   - In `QcEngine.Validate(model)` (`HPBim.Core\Qc\QcEngine.cs:32-62`):
     - `ValidateTcvnCovers(model.Rebars, errors)`: `model.Rebars` is empty $\rightarrow$ 0 errors added.
     - `_clashDetector.DetectAll(model)`: collections are empty $\rightarrow$ 0 clashes detected.
     - `_clearanceChecker.CheckAll(model)`: collections are empty $\rightarrow$ 0 violations found.
     - `report.Errors.Count == 0`, `report.Clashes.Count == 0` $\rightarrow$ `report.IsValid == true`!
3. **Misleading Success Output**:
   - `ValidateCommand` outputs: `[PASS] TRẠNG THÁI: SẠCH 100%. Mô hình sẵn sàng dispatch tới Revit.` with exit code `0`.
   - `GenerateCommand` (`HPBim.Cli\Commands\GenerateCommand.cs:42-49`) accepts this model, dispatches to Revit, executes 8 stages creating 0 elements, and prints: `DỰNG HÌNH HOÀN TẤT: Mô hình BIM 3D bản địa đã được tạo thành công trong Revit 2026.` with exit code `0`.
4. **Missing Schema Contracts**:
   - No `$schema` URI or `schemaVersion` check.
   - No `format` or `artifactType` discriminant (e.g. `artifactType: "HPBim.Model.v1"`).
   - No minimum required element validation (e.g. at least 1 Level, at least 1 Grid or physical element).
   - No referential integrity check: e.g. a `ColumnDef` referencing `baseLevel: "Level_Unknown"` does not fail schema validation prior to Revit dispatch.

---

## 4. Deep Dive 2: Single Source of Truth Execution Plan (Preview vs Apply Equivalence)

### 4.1 Fragmented Architecture: The Two Divergent Execution Worlds
The codebase is currently split into two independent, conflicting paradigms:

| Dimension | World A: 8-Stage Pipeline (`HPBim.Client`) | World B: Ad-Hoc CLI Commands (`HPBim.Cli`) |
| :--- | :--- | :--- |
| **Location** | `HPBim.Client\Pipeline\BimPipelineEngine.cs`, `HPBim.Client\ScriptGenerators\*` | `HPBim.Cli\Commands\Generate*Command.cs` (>20 command files) |
| **Input Source** | `BimModel` (deserialized from `NhaPho_5x18m.json`) | Scratch JSON files (`scratch\piles_true_bim_model.json`, `scratch\columns_story1_model.json`, etc.) parsed with manual `JsonDocument` queries |
| **Script Generation** | `CompositeBimGenerator` / `Stage1LevelGenerator`... | Hand-crafted inline Roslyn C# script strings inside each CLI command class |
| **Level Resolution** | Dynamic lookup with fallback to first level or duplication | Hardcoded string matching (`"Tầng Móng"`, `"Đỉnh Đài Móng"`, `"Tầng 1 (±0.000)"`) with `.First()` fallback |
| **Transaction Policy** | Composite runs in 1 TransactionGroup; Sequential runs in 8 separate calls | Each command sets its own `TransactionModes.Auto` or `None` |

### 4.2 Preview (Dry-Run) vs Apply (Commit) Divergence in `BimPipelineEngine`
In `HPBim.Client\Pipeline\BimPipelineEngine.cs:51-57`:
```csharp
if (options.DryRun && options.AtomicDryRun)
{
    return await ExecuteAtomicDryRunAsync(model, options, sw, cancellationToken).ConfigureAwait(false);
}

return await ExecuteSequentialStagesAsync(model, options, sw, cancellationToken).ConfigureAwait(false);
```
1. **Behavioral Divergence**:
   - In Dry-Run (when `AtomicDryRun == true`, default in `GenerateCommand`): It compiles and dispatches a single monolithic Roslyn script generated by `CompositeBimGenerator.GetScriptCode()`.
   - In Apply (Commit): It performs 8 sequential RPC calls (`Stage1LevelGenerator`, `Stage2GridGenerator`, ..., `Stage8RebarGenerator`).
2. **Logic & API Discrepancies**:
   - `Stage1LevelGenerator.cs:98-102` resolves `ViewFamily.FloorPlan` and invokes `ViewPlan.Create(doc, planType.Id, level.Id)` for every created level.
   - `CompositeBimGenerator.Stages1To3.cs:5-29` (`GetStage1LevelsScript`) **completely omits** `ViewPlan.Create`!
   - Result: Dry-Run tests an execution path that never creates views. If view generation fails during Commit (due to view template conflicts, duplicated view names, or missing `ViewFamilyType`), Apply fails despite Dry-Run having passed!
3. **Flawed Sequential Dry-Run**:
   - If `AtomicDryRun` is false and `DryRun` is true, `ExecuteSequentialStagesAsync` runs Stage 1 with `dryRun: true` (which rolls back), then runs Stage 2 with `dryRun: true`.
   - Because Stage 1 rolled back, levels created in Stage 1 do NOT exist when Stage 3 executes!
   - Stage 3 then silently skips all columns and beams (`if (!levelsByName.TryGetValue(lvlName, out var lvl)) continue;`).

### 4.3 Proposed Unification: The Single Execution Plan IR (`BimExecutionPlan`)
To guarantee Preview/Apply equivalence, the client and CLI must resolve all parameters into an immutable Intermediate Representation (IR) before touching Revit:
- **Phase 1: Planning / Resolution (Offline / Query)**:
  - Validate input artifact against JSON Schema.
  - Query Revit environment via `revit.context` to resolve existing Level IDs, FamilySymbol IDs, and ViewFamilyTypes.
  - Build `BimExecutionPlan`: an ordered list of atomic `PlanOperation` records (Levels $\rightarrow$ Grids $\rightarrow$ Foundations $\rightarrow$ Columns $\rightarrow$ Framing $\rightarrow$ Walls $\rightarrow$ Openings $\rightarrow$ Rooms $\rightarrow$ MEP).
  - Every operation has pre-resolved geometric coordinates, target family/type identifiers, and composite source keys.
- **Phase 2: Execution (Revit Bridge)**:
  - A single parameter-driven runner executes the `BimExecutionPlan`.
  - Preview executes with `DryRun = true` (rolling back the `TransactionGroup`).
  - Apply executes the EXACT same script and payload with `DryRun = false` (committing the `TransactionGroup`).
  - Zero divergent code paths between Preview and Apply.

---

## 5. Deep Dive 3: Per-Element Granularity & Implicit Fallbacks

### 5.1 Lack of Per-Element Status Reporting
Currently, all scripts return high-level summary counts:
- `Stage1LevelGenerator.cs:105`: `return new { stage = 1, totalLevels = levelItems.Count, createdLevels = createdCount };`
- `Stage3StructureGenerator.cs:151`: `return new { stage = 3, columns = createdCols, beams = createdBeams, slabs = createdSlabs };`
- `GenerateGradeBeamsCommand.cs:236`: `return new { createdCount = createdIds.Count, targetLevel = levelMong.Name, ... };`
There is **no per-element status**:
- No identification of which specific element succeeded or failed.
- No classification into `Created`, `Updated`, `NoOp`, `Skipped`, `Failed`.
- The caller receives no mapping of input element ID $\rightarrow$ Revit `UniqueId`.

### 5.2 Dangerous Implicit Fallbacks Catalogue
The codebase contains pervasive heuristic guessing and silent fallbacks that directly violate the Golden Rule (ĐIỀU 0):

#### 1. Arbitrary Level Fallback
- `HPBim.Cli\Commands\GeneratePilesCommand.cs:162-163`:
  ```csharp
  var level = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
      .FirstOrDefault(l => l.Name == "Tầng Móng")
      ?? new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().First();
  ```
- `HPBim.Cli\Commands\GenerateGradeBeamsCommand.cs:194-195`:
  ```csharp
  var levelMong = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
      .FirstOrDefault(l => l.Name == "Tầng Móng")
      ?? new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().First();
  ```
  *Risk*: If `"Tầng Móng"` is missing or misspelled, the code picks whatever level happens to be first in the document (e.g. "Tầng Mái" or "Level 1"), placing 196 foundation piles at the roof!

#### 2. Arbitrary Family / Type Fallback
- `HPBim.Cli\Commands\GeneratePilesCommand.cs:169-174`:
  ```csharp
  var symbol = new FilteredElementCollector(doc)...
      .FirstOrDefault(s => s.Family.Name == "1_STR_Coc_Vuong" && s.Name == "P_300x300")
      ?? new FilteredElementCollector(doc)...
          .FirstOrDefault(s => s.Name.Contains("300x300") || s.Name.Contains("300"));
  ```
- `HPBim.Cli\Commands\GenerateColumnsCommand.cs:179-181`:
  ```csharp
  FamilySymbol baseSymbol = symbols.FirstOrDefault(s => s.FamilyName.Contains("1_STR_Column_ConcreteRectangular_DungTam"))
                         ?? symbols.FirstOrDefault(s => s.FamilyName.Contains("Column_ConcreteRectangular"))
                         ?? symbols.First();
  ```
- `HPBim.Client\TypeResolver\DynamicTypeScriptHelper.cs:44, 76, 101, 122`:
  ```csharp
  var baseSymbol = symbols.FirstOrDefault() ?? throw new InvalidOperationException("No structural column symbol found.");
  ```
  *Risk*: If the exact symbol is not found, it picks the first symbol in the category. In a multi-discipline project, this could duplicate a circular steel column or precast hollow column instead of a rectangular concrete column.

#### 3. Arbitrary Plumbing Fixture Fallback
- `HPBim.Client\ScriptGenerators\CompositeBimGenerator.Stages7To8.cs:13, 20`:
  ```csharp
  var defaultFixtureSym = fixtureSymbols.FirstOrDefault();
  ...
  var inst = doc.Create.NewFamilyInstance(pt, defaultFixtureSym, lvl, StructuralType.NonStructural);
  ```
  *Risk*: The first plumbing fixture in the project is used for every fixture in the model! Toilets, sinks, bathtubs, floor drains all become the same element type.

#### 4. Arbitrary Rebar Host Fallback & Silent Exception Swallowing
- `HPBim.Client\ScriptGenerators\CompositeBimGenerator.Stages7To8.cs:103, 144-147`:
  ```csharp
  host ??= hostCandidates.FirstOrDefault();
  ...
  try {
      var rebar = Rebar.CreateFromCurves(...);
  }
  catch (Exception) {
      // Fallback gracefully if host lacks valid geometry cover or boundaries.
  }
  ```
  *Risk*: If `hostId` is not found, the rebar is hosted in an arbitrary column or beam. If Revit throws an error during rebar creation, it is caught in an empty `catch` block and silently discarded!

#### 5. Silent Geometry & Curve Skips
- `HPBim.Client\ScriptGenerators\CompositeBimGenerator.Stages1To3.cs:62, 83, 89`:
  - `if (!existingLevels.TryGetValue(baseLvlName, out var baseLvl)) continue;` (silent skip).
  - `if (p0.DistanceTo(p1) < 0.001) continue;` (silent skip).
- `HPBim.Client\ScriptGenerators\CompositeBimGenerator.Stages4To6.cs:67, 82`:
  - `if (!wallsById.TryGetValue(hostWallId, out var hostWall)) continue;` (silent skip).
  - `double param = Math.Clamp(distFt / locCurve.Curve.Length, 0.05, 0.95);` (arbitrary clamping of opening position).

---

## 6. Deep Dive 4: Rollback Verification & Bridge Status Honesty

### 6.1 Transaction & Rollback Lifecycle in `ScriptRunner`
In `HPRebar.McpBridge\Service\ScriptRunner.cs:74-111`:
```csharp
if (mode != TransactionModes.None)
{
    group = new TransactionGroup(doc, "MCP: " + label);
    group.Start();
}

if (mode == TransactionModes.Auto)
{
    transaction = new Transaction(doc, "MCP script");
    AutoDismissFailurePreprocessor.ApplyTo(transaction);
    transaction.Start();
}

value = script.RunAsync(globals, linked.Token).GetAwaiter().GetResult().ReturnValue;
...
if (transaction is not null && transaction.Commit() != TransactionStatus.Committed)
    throw new InvalidOperationException("Revit rejected the change (a failure was posted during commit).");

if (group is not null)
{
    if (request.DryRun)
    {
        group.RollBack();
        rolledBack = true;
    }
    else
    {
        group.Assimilate();
    }
}
```

### 6.2 Blind Spots and Integrity Gaps

#### 1. Silent Deletion of Revit Warnings
In `HPRebar.McpBridge\Service\AutoDismissFailurePreprocessor.cs:12-25`:
```csharp
public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
{
    foreach (var failure in failuresAccessor.GetFailureMessages())
    {
        if (failure.GetSeverity() == FailureSeverity.Warning)
        {
            failuresAccessor.DeleteWarning(failure);
            continue;
        }

        return FailureProcessingResult.ProceedWithRollBack;
    }

    return FailureProcessingResult.Continue;
}
```
- **Finding**: Every single warning generated by Revit during element creation (e.g. "Elements overlap", "Room is not in a properly enclosed region", "Line is slightly off axis", "Wall cut by opening") is **permanently deleted and discarded**.
- **Impact**: Neither the script, nor `ExecuteResult`, nor the client ever sees these warnings. The AI receives a clean result even if Revit produced 50 geometric warnings.
- **Requirement R1 Directive**: "thu thập warning từ Revit" (collect warnings from Revit). `AutoDismissFailurePreprocessor` must collect `failure.GetDescriptionText()` into a warning list and pass it back in `ExecuteResult.Warnings`.

#### 2. `DocumentChangeCounter` False Positive Counts on Dry-Run
In `HPRebar.McpBridge\Service\DocumentChangeCounter.cs:28-33`:
```csharp
private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
{
    _added += e.GetAddedElementIds().Count;
    _modified += e.GetModifiedElementIds().Count;
    _deleted += e.GetDeletedElementIds().Count;
}
```
- **Finding**: `DocumentChangeCounter` hooks `Application.DocumentChanged`. In Revit, `DocumentChanged` fires when the inner `Transaction` commits (`transaction.Commit()`), **before** `group.RollBack()` is called.
- As a result, on a dry-run, `ExecuteResult.Changed.Added` reports e.g. `196` elements added, alongside `RolledBack = true`.
- If a client or test checks `Changed.Added > 0` to decide if elements persisted, or if `group.RollBack()` fails silently, the caller cannot distinguish whether elements actually remained in the document.

#### 3. Transport Timeout & `OutcomeUnknown` Ambiguity
In `HPBim.Client\Connection\RevitPipeClient.cs:98, 144-150`:
```csharp
var timeout = TimeSpan.FromSeconds(request.TimeoutSeconds > 0 ? request.TimeoutSeconds : _options.DefaultExecuteTimeoutSeconds);
...
using var timeoutCts = new CancellationTokenSource(timeout);
```
- **Finding**: The client-side pipe timeout is identical to `request.TimeoutSeconds`.
- If Revit API thread is delayed by a modal dialog, view regeneration, or background work:
  - The client CTS cancels at $T=30$s and throws `RevitPipeException(isTimeout: true)`.
  - The named pipe connection may be torn down or disconnected.
  - Meanwhile, Revit may continue executing on its thread and commit the transaction at $T=32$s!
- The client records a failure, but the elements were actually created in the RVT document.
- On a retry, the script runs again and creates duplicate elements!
- **Requirement R1/R2 Directive**: Transport timeout must be separated from execution budget ($T_{\text{transport}} = T_{\text{exec}} + \Delta t_{\text{margin}}$), and any timeout must be categorized as `OutcomeUnknown`, followed by an atomic reconciliation against Revit Extensible Storage before re-attempting.

---

## 7. Deep Dive 5: Measured QA/QC Metrics & QcEngine Literals

### 7.1 Hardcoded PASS Literals in `FoundationQcEngine.cs`
In `HPBim.Core\Qc\FoundationQcEngine.cs:250-264`:
```csharp
var steps = new List<FoundationStepResult>
{
    AuditGridsAndLevels(grids, levels, expectedXGrids, expectedYGrids, expectedLevels),
    AuditCounts(piles.Count, footings.Count, beams.Count),
    AuditPositions(piles, footings, groundTruthPiles, groundTruthFootings),
    AuditDimensions(footings, beams),
    AuditElevations(piles, footings, beams),
    AuditStructuralRelationships(piles, footings, beams, gridNodes),
    AuditCadDetails(footings),
    new FoundationStepResult(8, "Sổ Cái & Hồ Sơ Nghiệm Thu", true, "Sẵn sàng ghi nhận vào Sổ cái và xuất Biên bản.", Array.Empty<string>())
};
```
- **Violation**: Step 8 is **hardcoded to `true`**! No ledger verification, no file existence check, no cryptographic receipt verification occurs. It is an unmeasured pass literal.

### 7.2 Unmeasured Metrics Reporting "100% Pass" on Null Inputs
In `HPBim.Core\Qc\FoundationQcEngine.cs:69-106`:
```csharp
public FoundationStepResult AuditPositions(
    IReadOnlyList<PileAuditItem> piles,
    IReadOnlyList<FootingAuditItem> footings,
    IReadOnlyDictionary<long, Point2D>? groundTruthPiles = null,
    IReadOnlyDictionary<string, Point2D>? groundTruthFootings = null)
{
    var errors = new List<string>();
    if (groundTruthPiles != null) { ... }
    if (groundTruthFootings != null) { ... }

    bool passed = errors.Count == 0;
    string summary = passed
        ? "100% tọa độ tim cọc và trọng tâm đài móng khớp CAD, sai số <= 2.0mm."
        : $"Phát hiện {errors.Count} cấu kiện lệch vị trí quá 2.0mm.";

    return new FoundationStepResult(3, "Vị Trí & Tọa Độ Mặt Bằng", passed, summary, errors);
}
```
- **Violation**: When `groundTruthPiles` and `groundTruthFootings` are `null` (which happens whenever CAD ground truth is not provided), **zero checks are run**, `errors.Count == 0`, and the method reports:
  `"100% tọa độ tim cọc và trọng tâm đài móng khớp CAD, sai số <= 2.0mm."` with `Passed = true`!
- The same flaw exists in `AuditGridsAndLevels` (`FoundationQcEngine.cs:272, 290`): when `expX == null` and `expectedLevels == null`, no spans are checked, yet it returns:
  `"Hệ trục (27/27) và Cao trình (10/10) hoàn toàn chuẩn xác, ghim 100%."` with `Passed = true`!
- **Requirement R1 Directive**: "Nếu thiếu nguồn dữ liệu đối chiếu hoặc thiếu thông số đo, phải kết luận Inconclusive, cấm tự động nghiệm thu." (If baseline comparison data or measurements are missing, the result must be `Inconclusive`, never an automatic PASS).

### 7.3 Count-Only Verification in `RevitModelVerifier.cs`
In `HPBim.Client\Verification\RevitModelVerifier.cs:48-75, 116-136`:
- `GetVerificationQueryScript()` runs 11 `FilteredElementCollector` queries calling `.GetElementCount()`.
- `CompareCounts` compares document totals against JSON totals.
- **Flaw**:
  - If a project already had 10 columns, and `GenerateCommand` failed to create any columns, `GetElementCount()` is 10 $\rightarrow$ `IsMatch = true`!
  - If elements were placed at incorrect coordinates, wrong levels, or with wrong cross-sections, `RevitModelVerifier` reports:
    `"ĐỐI SOÁT HOÀN HẢO: 100% số lượng cấu kiện trong Revit khớp tuyệt đối với schema thiết kế."`
  - It does not measure a single vertex, curve, elevation, or bounding box!

---

## 8. Summary Matrix of Findings & Required Transformations

| Category | Current As-Is State | Defect / Vulnerability | Required Target State (R1 / P0) |
| :--- | :--- | :--- | :--- |
| **Artifact Schema** | `BimModelSerializer` deserializes unchecked JSON; `BimModel` has all default properties. | Raw CAD JSON or `{}` silently deserializes to empty model; passes `ValidateCommand` with exit code 0. | `BimModelSchemaValidator` enforces schema version (`$schema`, `schemaVersion`), required collections, and referential integrity. |
| **Execution Architecture** | Dual architecture: 8-stage pipeline in Client vs >20 bespoke CLI commands with inline Roslyn scripts. | Extreme code fragmentation; inconsistent level resolution, duplicated math, different transaction policies. | Unify into a single `BimExecutionPlan` (IR); retire bespoke CLI generator scripts in favor of a shared planner. |
| **Preview vs Apply** | DryRun runs monolithic `CompositeBimGenerator`; Apply runs 8 sequential stage calls. | DryRun omits `ViewPlan.Create`; sequential dry-run breaks cross-stage references; logic diverges. | Single Source of Truth Execution Plan IR: identical script and payload executed for both, toggling only `DryRun` flag. |
| **Element Granularity** | Aggregate return objects (`{ createdCount = 10 }`). | No status per element; caller cannot know which elements succeeded or failed; no ID mapping. | Granular reporting: list of `ElementExecutionResult` returning `Created`, `Updated`, `NoOp`, `Skipped`, `Failed` with Revit UniqueId. |
| **Implicit Fallbacks** | `levels.FirstOrDefault(...) ?? levels.First()`; `symbols.FirstOrDefault(...) ?? symbols.First()`; first plumbing fixture; clamp positions. | Blatant violation of ĐIỀU 0: heuristic guessing, putting foundations on roof, wrong symbols, silently skipping items. | Zero fallbacks: strict resolution. If level or symbol is not found, fail fast with explicit code and record in RFI / Skipped list. |
| **Revit Warnings** | `AutoDismissFailurePreprocessor` executes `failuresAccessor.DeleteWarning(failure)`. | All geometric/modeling warnings from Revit are destroyed and lost without being reported. | Collect all warnings into `ExecuteResult.Warnings` list and return to caller for QA/QC analysis. |
| **Rollback Honesty** | `ScriptRunner` executes `group.RollBack()` but does not verify post-rollback state. | `DocumentChangeCounter` reports additions that occurred before rollback; timeout causes unknown outcome. | Verify rollback confirmation from Revit; classify transport timeouts as `OutcomeUnknown`; implement receipt journal. |
| **QA/QC Measurement** | `FoundationQcEngine` has hardcoded `Step 8 = true`; null ground truth outputs `Passed = true`; count-based verifier. | False sense of security; reports 100% PASS without measuring data; passes bogus models. | Forbid hardcoded PASS; return `Inconclusive` when ground truth is missing; perform measured geometric verification. |

---

## 9. Recommendations for Milestone Implementation

1. **Phase 1: Fix Test Baseline & Implement Strict Schema Validation**:
   - Implement `CancelAsync` in `FakeRevitPipeClient.cs` to restore 100% green compilation across all tests.
   - Create `BimModelSchemaValidator` in `HPBim.Core.Serialization`:
     - Validate JSON schema, required non-empty collections (levels $\ge 1$, grids/elements $\ge 1$), and referential integrity (element level exists in `Levels`, wall host exists in `Walls`).
     - Reject raw CAD JSON with explicit error code `ERR_INVALID_SCHEMA`.
2. **Phase 2: Define Single Source of Truth `BimExecutionPlan` (IR)**:
   - Create `BimExecutionPlan` and `PlanOperation` models in `HPBim.Core`.
   - Implement `BimPlanCompiler` to translate `BimModel` into `BimExecutionPlan`.
   - Update `BimPipelineEngine` to execute the IR identically in both Dry-Run and Commit modes.
3. **Phase 3: Enforce Per-Element Granularity & Eliminate All Fallbacks**:
   - Update Revit script template to return `List<ElementExecutionRecord>` with `Status`, `SourceKey`, `ElementId`, `UniqueId`, and `FailureReason`.
   - Remove all `.First()` and `.FirstOrDefault()` fallbacks; throw or mark `Failed` when symbols/levels are missing.
4. **Phase 4: Capture Revit Warnings & Post-Rollback Verification**:
   - Modify `AutoDismissFailurePreprocessor` to collect warning strings into a shared context.
   - Update `ExecuteResult` contract to include `IReadOnlyList<string> Warnings`.
   - Add post-rollback confirmation in `ScriptRunner`.
5. **Phase 5: Refactor QA/QC Engine to Measured Geometry & Inconclusive States**:
   - In `FoundationQcEngine`, change missing ground truth evaluation to return `passed: false, summary: "INCONCLUSIVE: Thiếu dữ liệu đối soát CAD"`.
   - Remove hardcoded Step 8 literal `true`.
   - Refactor `RevitModelVerifier` to query element bounding boxes and coordinates rather than raw document counts.
