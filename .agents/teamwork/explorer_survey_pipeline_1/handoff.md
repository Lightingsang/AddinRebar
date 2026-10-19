# Handoff Report: R1 (P0 — Truthful Results & Preview/Apply Equivalence) Codebase Survey

**Agent**: `explorer_survey_pipeline_1`  
**Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_pipeline_1`  
**Date**: 2026-10-07  
**Recipient**: Orchestrator (`44938268-52fa-4afa-af1c-06dc2c4f2512`)  
**Handoff Type**: Hard Handoff (Investigation Complete)  

---

## 1. Observation

### Observation 1: Test Baseline & Compilation Error
- **Command**: `dotnet test G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.Core.Tests`
  - Output: `Passed! total: 268, failed: 0, succeeded: 268, skipped: 0, duration: 699ms`.
- **Command**: `dotnet test G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx`
  - Output: `error CS0535: 'FakeRevitPipeClient' does not implement interface member 'IRevitPipeClient.CancelAsync(CancellationToken)' [G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.Client.Tests\HPBim.Client.Tests.csproj]`
  - Line: `HPBim\tests\HPBim.Client.Tests\Fakes\FakeRevitPipeClient.cs:14`.

### Observation 2: Artifact & Schema Deserialization Bypasses
- In `HPBim.Core\Serialization\BimModelSerializer.cs:49`:
  ```csharp
  var model = JsonSerializer.Deserialize<BimModel>(json, DefaultOptions);
  return model ?? throw new JsonException("Deserialization returned null BimModel.");
  ```
- In `HPBim.Core\Models\BimModel.cs:8-20`:
  All properties have default initializers (`ProjectName = string.Empty; Levels = Array.Empty<LevelDef>(); Grids = Array.Empty<AxisGrid>();`, etc.).
- In `HPBim.Core\Qc\QcEngine.cs:32-62`:
  When collections are empty, `ValidateTcvnCovers`, `_clashDetector.DetectAll`, and `_clearanceChecker.CheckAll` add 0 violations.
- In `HPBim.Cli\Commands\ValidateCommand.cs:60-63`:
  When `report.IsValid == true` (which occurs on any empty `BimModel`), it prints `[PASS] TRẠNG THÁI: SẠCH 100%. Mô hình sẵn sàng dispatch tới Revit.` and returns `0`.
- In `scratch\piles_cad_vs_revit_inspection.json:1-4`:
  A raw CAD inspection file with root object `{ "cad": { "blockName": "Rec-TT", ... } }` deserializes into an empty `BimModel` and produces a false 100% PASS without rejection.

### Observation 3: Divergence Between Preview (Dry-Run) and Apply (Commit)
- In `HPBim.Client\Pipeline\BimPipelineEngine.cs:51-57`:
  - Dry-Run calls `ExecuteAtomicDryRunAsync` which uses `_compositeGenerator.Generate(model)` (single monolithic script).
  - Apply calls `ExecuteSequentialStagesAsync` which iterates over `_generators` (8 separate RPC calls).
- In `HPBim.Client\ScriptGenerators\Stage1LevelGenerator.cs:98-102`:
  `ViewPlan.Create(doc, planType.Id, level.Id)` is invoked for every level.
- In `HPBim.Client\ScriptGenerators\CompositeBimGenerator.Stages1To3.cs:5-29`:
  `ViewPlan.Create` is **completely absent**. Dry-run never tests view creation.
- In `HPBim.Cli\Commands`: Over 20 CLI commands (`GenerateFoundationCommand`, `GeneratePilesCommand`, `GenerateGradeBeamsCommand`, `GenerateColumnsCommand`, etc.) have separate, duplicated inline Roslyn scripts completely independent of `BimPipelineEngine`.

### Observation 4: Coarse Granularity & Dangerous Implicit Fallbacks
- Script return structures report only counts:
  - `Stage1LevelGenerator.cs:105`: `return new { stage = 1, totalLevels = levelItems.Count, createdLevels = createdCount };`
  - `Stage3StructureGenerator.cs:151`: `return new { stage = 3, columns = createdCols, beams = createdBeams, slabs = createdSlabs };`
- Pervasive implicit fallbacks:
  - `GeneratePilesCommand.cs:162-163`: `.FirstOrDefault(l => l.Name == "Tầng Móng") ?? ...OfClass(typeof(Level)).Cast<Level>().First();`
  - `GenerateGradeBeamsCommand.cs:194-195`: `.FirstOrDefault(l => l.Name == "Tầng Móng") ?? ...OfClass(typeof(Level)).Cast<Level>().First();`
  - `GenerateColumnsCommand.cs:179-181`: `symbols.FirstOrDefault(...) ?? symbols.First();`
  - `DynamicTypeScriptHelper.cs:44, 76, 101, 122`: `wallTypes.FirstOrDefault() ?? throw ...; symbols.FirstOrDefault() ?? throw ...;`
  - `CompositeBimGenerator.Stages7To8.cs:13, 20`: `var defaultFixtureSym = fixtureSymbols.FirstOrDefault();` used for all fixtures.
  - `CompositeBimGenerator.Stages7To8.cs:103`: `host ??= hostCandidates.FirstOrDefault();` (arbitrary rebar host).
  - `CompositeBimGenerator.Stages7To8.cs:144-147`: `catch (Exception) { /* Fallback gracefully */ }` silently swallowing exceptions.
  - `CompositeBimGenerator.Stages1To3.cs:62, 83`: `if (!existingLevels.TryGetValue(...)) continue;` silently skips elements.

### Observation 5: Warning Discard & Rollback Honesty Blind Spots
- In `HPRebar.McpBridge\Service\AutoDismissFailurePreprocessor.cs:16-20`:
  ```csharp
  if (failure.GetSeverity() == FailureSeverity.Warning)
  {
      failuresAccessor.DeleteWarning(failure);
      continue;
  }
  ```
  All Revit warnings are permanently deleted and never collected in `ExecuteResult`.
- In `HPRebar.McpBridge\Service\DocumentChangeCounter.cs:28-33`:
  `DocumentChanged` increments added element counts during inner transaction commit, before `group.RollBack()` is called. Dry-run results thus report `Changed.Added > 0`.
- In `HPBim.Client\Connection\RevitPipeClient.cs:98, 144-150`:
  Client timeout equals execution timeout. If Revit API thread is delayed, client throws `RevitPipeException(isTimeout: true)` with `OutcomeUnknown`, lacking reconnection and reconciliation.

### Observation 6: Unmeasured Metrics & Hardcoded Pass Literals
- In `HPBim.Core\Qc\FoundationQcEngine.cs:259`:
  `new FoundationStepResult(8, "Sổ Cái & Hồ Sơ Nghiệm Thu", true, "Sẵn sàng ghi nhận vào Sổ cái và xuất Biên bản.", Array.Empty<string>())` (Step 8 is hardcoded to `true`).
- In `HPBim.Core\Qc\FoundationQcEngine.cs:100-103`:
  When `groundTruthPiles == null` and `groundTruthFootings == null`, `AuditPositions` outputs `Passed = true` and claims: `"100% tọa độ tim cọc và trọng tâm đài móng khớp CAD, sai số <= 2.0mm."`
- In `HPBim.Client\Verification\RevitModelVerifier.cs:48-75`:
  Model verification relies strictly on document-wide `GetElementCount()`, checking total counts without measuring coordinates, geometry, or element parameters.

---

## 2. Logic Chain

1. **Schema Validation Integrity**:
   - Because `BimModel` uses records with empty collection defaults (Observation 2) and `JsonSerializer` discards unknown properties, any JSON (even raw CAD or `{}`) parses into a non-null `BimModel`.
   - Because `QcEngine` loops over collections and performs 0 checks when collections are empty, empty models produce 0 errors and `report.IsValid == true`.
   - Therefore, `ValidateCommand` and `GenerateCommand` report 100% success for invalid or foreign JSON files, directly violating the R1 truthfulness mandate.
2. **Preview/Apply Equivalence**:
   - Because `BimPipelineEngine` executes `CompositeBimGenerator` for dry-run and sequential stage generators for commit (Observation 3), the two execution paths execute different Roslyn scripts.
   - Because `Stage1LevelGenerator` creates views while `CompositeBimGenerator` does not, dry-run can pass while apply fails due to view creation errors.
   - Therefore, a Single Source of Truth Execution Plan IR (`BimExecutionPlan`) is required so that Preview and Apply execute the exact same instructions.
3. **Per-Element Traceability & Fallbacks**:
   - Because script return values only contain aggregate counts (Observation 4), failures and silent skips cannot be attributed to specific elements.
   - Because multiple commands use `.First()` when a requested level or symbol is not found, elements are placed at arbitrary heights or with arbitrary types without notifying the user.
   - Therefore, per-element status (`Created`, `Updated`, `NoOp`, `Skipped`, `Failed`) and zero-fallback strict resolution must be enforced.
4. **Status Honesty & Warnings**:
   - Because `AutoDismissFailurePreprocessor` deletes warnings (Observation 5), Revit geometric clashes and modeling warnings are completely masked.
   - Therefore, warnings must be captured and returned in `ExecuteResult.Warnings`.
5. **Measured QC vs Counts**:
   - Because `FoundationQcEngine` has hardcoded `Step 8 = true` and reports PASS when ground-truth dictionaries are null (Observation 6), the audit engine provides false positives.
   - Therefore, missing comparison data must return `Inconclusive`, and verification must measure geometry rather than element counts.

---

## 3. Caveats

- **Read-Only Scope**: In strict accordance with the explorer role, no source code files were modified. The test compilation error in `FakeRevitPipeClient.cs` was identified and documented, not fixed.
- **Live Revit Environment**: Tests touching the live Revit Named Pipe (`\\.\pipe\hprebar-mcp-r2026`) were inspected via code analysis and unit test harnesses; active live execution against a running Revit instance was not conducted during this survey turn.
- **Other Host MCPs**: The survey focused on `HPBim` and the Revit bridge in `HPRebar`/`McpShared`. Other host bridges (AutoCAD, Navisworks, ETABS, etc.) share `McpShared` contracts but were not in scope for this milestone.

---

## 4. Conclusion

The current codebase contains critical vulnerabilities that allow raw CAD JSON and malformed models to pass silently, allows Preview and Apply to diverge, conceals Revit warnings, and relies on unmeasured QA/QC pass literals. 

To satisfy Milestone R1 (P0), the implementation roadmap must execute:
1. **Fix compilation baseline**: Add `CancelAsync` to `FakeRevitPipeClient.cs`.
2. **Artifact & Schema Validator**: Implement `BimModelSchemaValidator` with strict schema versioning, mandatory collections, and referential integrity.
3. **Single Execution Plan IR**: Create `BimExecutionPlan` to unify Preview and Apply into a single deterministic code path.
4. **Per-Element Granularity**: Report `ElementExecutionRecord` for every element, removing all heuristic `.First()` fallbacks.
5. **Revit Warning Capture**: Capture warnings in `AutoDismissFailurePreprocessor` and propagate them via `ExecuteResult`.
6. **Empirical QA/QC**: Replace hardcoded PASS literals and null-bypass passes with `Inconclusive` results, and upgrade `RevitModelVerifier` to geometric measurement.

---

## 5. Verification Method

To independently verify the observations:
1. **Verify Baseline Test Run**:
   - Run: `dotnet test G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.Core.Tests`
   - Expectation: 268 passed, 0 failed.
2. **Verify Client Test Compilation Defect**:
   - Run: `dotnet test G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.Client.Tests`
   - Expectation: Fails with `CS0535` in `FakeRevitPipeClient.cs:14`.
3. **Verify Silent Schema Pass**:
   - Inspect `HPBim\src\HPBim.Core\Serialization\BimModelSerializer.cs:49` and `HPBim\src\HPBim.Core\Qc\QcEngine.cs:32-62`.
   - Observe that `JsonSerializer.Deserialize<BimModel>("{}")` returns a valid model and `QcEngine.Validate` returns `IsValid == true`.
4. **Verify Implicit Fallbacks**:
   - Inspect `HPBim\src\HPBim.Cli\Commands\GeneratePilesCommand.cs:162-163` and `DynamicTypeScriptHelper.cs:44, 76`.
   - Confirm presence of `?? ...First()` fallbacks.
5. **Verify Hardcoded Step 8 & Warning Deletion**:
   - Inspect `HPBim\src\HPBim.Core\Qc\FoundationQcEngine.cs:259` (literal `true`).
   - Inspect `HPRebar\HPRebar.McpBridge\Service\AutoDismissFailurePreprocessor.cs:18` (`DeleteWarning`).
