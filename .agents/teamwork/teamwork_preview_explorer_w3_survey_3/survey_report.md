# HPBim Wave 3 Remediation — Survey Report: R5 & Test Suites / Governance

**Survey Explorer:** Explorer 3 (Survey Explorer)  
**Date:** 2026-10-09  
**Working Directory:** `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_w3_survey_3`  
**Target Codebase:** `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim`  
**Status:** Read-Only Investigation Complete

---

## Executive Summary

This survey report provides an in-depth, read-only architectural investigation of:
1. **R5: Standardized Geometric Measurement & Acceptance Verification** in the `HPBim` deliverable.
2. **Test Suites and Repository Governance** across `HPBim` and related solution projects.

We traced the complete execution and verification pipeline, identifying the exact root causes of four critical verification flaws:
- Incomplete querying of elements by mapped `TargetUniqueId` (falling back to mark/name scanning).
- Missing pile cutoff elevation pre-check in `HasRequiredMeasurements`, causing piles with missing elevations to be falsely verified.
- Absence of profile outline verification for Footings and Slabs.
- The `AuditGeneric` auto-verify flaw, which unconditionally stamps unmeasured categories as `Verified`.

We also audited the complete test suite (965 tests in `HPBim.slnx`, 100% passing) and verified Clean Code constraints against repository rules `HP_CLEAN_CODE_CORE.md`.

---

## 1. R5: Standardized Geometric Measurement & Acceptance Verification

### 1.1 Architecture & Class Map

The geometric measurement and verification subsystem in `HPBim` is cleanly separated across domain contracts (`HPBim.Core`) and host communication services (`HPBim.Client`):

| File Path | Role & Responsibilities | Line Count |
|---|---|---|
| `src/HPBim.Core/Verification/MeasuredGeometricEngine.cs` | Orchestrator of geometric audits. Maps categories to audit handlers; checks required measurements before auditing; handles missing items. | 189 lines |
| `src/HPBim.Core/Verification/MeasuredGeometricEngine.Structural.cs` | Implements structural element audits: `AuditColumn`, `AuditLinearStructure` (Beam/GradeBeam), `AuditPile`, `AuditWall`. Implements `HasRequired*Measurements` for structural elements. | 248 lines |
| `src/HPBim.Core/Verification/MeasuredGeometricEngine.ProfileGrid.cs` | Implements profile and datum element audits: `AuditProfileStructure` (Footing/Slab), `AuditLevel`, `AuditGrid`. Implements `HasRequired*Measurements` for profiles and datums. | 189 lines |
| `src/HPBim.Core/Verification/ActualElementGeometry.cs` | Strongly-typed DTO containing empirical measurements extracted from Revit (Centroid, CurveStart, CurveEnd, Top/Bottom Elevation, Width, Height, Thickness, Rotation). | 35 lines |
| `src/HPBim.Core/Verification/ElementGeometricAudit.cs` | Strongly-typed audit result per element (planned vs actual, deltas, tolerance flags, defect reasons, and status). | 54 lines |
| `src/HPBim.Core/Verification/GeometricAuditReport.cs` | Aggregated report of the entire plan audit (overall pass/fail, counts of Verified, Defect, Inconclusive, Missing). | 57 lines |
| `src/HPBim.Core/Verification/GeometricVerificationStatus.cs` | Status enum: `Verified = 1`, `Defect = 2`, `Inconclusive = 3`, `Missing = 4`. | 10 lines |
| `src/HPBim.Client/Verification/IMeasuredGeometricVerifier.cs` | Service interface for measured geometric verification over Revit Named Pipe. | 15 lines |
| `src/HPBim.Client/Verification/MeasuredGeometricVerifier.cs` | Dispatches `RevitGeometryQueryScriptTemplate` via `IRevitPipeClient` and feeds parsed actuals into `MeasuredGeometricEngine`. | 71 lines |
| `src/HPBim.Client/Verification/MeasuredGeometryJsonParser.cs` | Deserializes Revit JSON output into `IReadOnlyDictionary<string, ActualElementGeometry>`. | 79 lines |
| `src/HPBim.Client/Verification/RevitGeometryQueryScriptTemplate.cs` | Generates Roslyn C# script executed inside Revit process to inspect geometry of active document elements. | 128 lines |
| `src/HPBim.Cli/Commands/VerifyCommand.cs` | CLI command executing both schema count verification and measured geometric verification. | 144 lines |

---

### 1.2 Element Querying by Mapped TargetUniqueId

#### As-Is Trace
Elements are identified in `ElementPlanItem` with:
- `SourceKey`: `CompositeSourceKey` (`[DocIdentifier]#[Category]:[PrimaryHandle]`)
- `TargetUniqueId`: string containing the Revit element UniqueId (mapped via ledger or created receipt).

However, during geometric verification, there are three distinct breakdown points where `TargetUniqueId` is ignored:

1. **Revit Query Script Defect (`RevitGeometryQueryScriptTemplate.cs:46-54`)**:
   ```csharp
   Element elem = null;
   if (category == "Column") elem = allColumns.FirstOrDefault(c => c.Name == mark || (c.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() == mark));
   else if (category == "Beam" || category == "GradeBeam") elem = allBeams.FirstOrDefault(b => b.Name == mark || (b.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() == mark));
   else if (category == "Wall") elem = allWalls.FirstOrDefault(w => w.Name == mark);
   else if (category == "Level") elem = allLevels.FirstOrDefault(l => l.Name == mark);
   else if (category == "Grid") elem = allGrids.FirstOrDefault(g => g.Name == mark);
   else if (category == "Pile" || category == "Footing") elem = allPiles.FirstOrDefault(p => p.Name == mark || (p.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() == mark));
   else if (category == "Slab" || category == "Floor") elem = allFloors.FirstOrDefault(fl => fl.Name == mark || (fl.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() == mark));
   ```
   **Problem**: The script queries via LINQ `FirstOrDefault` by `mark` or `Name`. It completely ignores `item.Str("targetUniqueId")`! If two elements share the same mark (e.g. multiple piles or columns with mark `"C1"` or `"P1"`), or if `mark` is empty, it returns the first element or null. Direct O(1) lookup via `doc.GetElement(targetUniqueId)` is never executed.

2. **Engine Lookup Defect (`MeasuredGeometricEngine.cs:98-114`, `FindActual`)**:
   ```csharp
   private static ActualElementGeometry? FindActual(ElementPlanItem item, IReadOnlyDictionary<string, ActualElementGeometry> actuals)
   {
       if (item.SourceKey != null)
       {
           string formattedKey = item.SourceKey.ToString();
           if (actuals.TryGetValue(formattedKey, out var act)) return act;

           string plainKey = $"{item.SourceKey.DocIdentifier}#{item.SourceKey.Category}:{item.SourceKey.PrimaryHandle}";
           if (actuals.TryGetValue(plainKey, out act)) return act;

           if (!string.IsNullOrEmpty(item.SourceKey.PrimaryHandle) && actuals.TryGetValue(item.SourceKey.PrimaryHandle, out act)) return act;
       }

       if (!string.IsNullOrEmpty(item.Mark) && actuals.TryGetValue(item.Mark, out var actByMark)) return actByMark;

       return null;
   }
   ```
   **Problem**: Even if `item.TargetUniqueId` is populated and `actuals` dictionary contains `act.UniqueId` (as populated by `AuditPlan` at line 45: `if (!string.IsNullOrEmpty(act.UniqueId)) dict[act.UniqueId!] = act;`), `FindActual` **never** checks `item.TargetUniqueId`!

3. **CLI Invocation Defect (`VerifyCommand.cs:69`)**:
   `var plan = ExecutionPlanBuilder.Build(model, targetDoc: null, options: new ExecutionOptions());`
   `VerifyCommand` constructs a brand new plan without consulting `BimCoordinationLedgerService`. Thus, `plan.Items` always have `TargetUniqueId = null`.

#### Remediation Solution
1. In `RevitGeometryQueryScriptTemplate.cs`:
   Read `string targetUniqueId = item.Str("targetUniqueId", string.Empty);`.
   If non-empty, query immediately:
   ```csharp
   if (!string.IsNullOrEmpty(targetUniqueId))
   {
       elem = doc.GetElement(targetUniqueId);
   }
   if (elem == null)
   {
       // Fallback to category collector search by mark
   }
   ```
2. In `MeasuredGeometricEngine.cs` (`FindActual`):
   Prioritize `TargetUniqueId` lookup:
   ```csharp
   if (!string.IsNullOrEmpty(item.TargetUniqueId) && actuals.TryGetValue(item.TargetUniqueId, out var actByUid))
   {
       return actByUid;
   }
   ```
3. In `VerifyCommand.cs`:
   Inject/resolve `BimCoordinationLedgerService` and call `PopulateTargetUniqueIds(plan, ledgerService)` before dispatching to `VerifyPlanGeometryAsync`.

---

### 1.3 Verification of Pile Elements & Cutoff Elevation

#### Root Cause Analysis
In `MeasuredGeometricEngine.Structural.cs`:
1. `HasRequiredPileMeasurements` (lines 184-194):
   ```csharp
   private static bool HasRequiredPileMeasurements(ActualElementGeometry actual, out string missingReason)
   {
       if (!actual.Centroid.HasValue)
       {
           missingReason = "Rule 0: Pile thiếu thuộc tính Centroid từ Revit API.";
           return false;
       }

       missingReason = string.Empty;
       return true;
   }
   ```
   **Flaw**: Only `Centroid` is checked. `TopElevationMm` (pile cutoff elevation) is **never required**.
2. `AuditPile` (lines 119-124):
   ```csharp
   if (actual.TopElevationMm.HasValue)
   {
       deltaElev = Math.Abs(actual.TopElevationMm.Value - ptSpec.OffsetZMm);
       if (deltaElev > _tolerances.ElevationMm)
           discrepancies.Add($"Pile cutoff elevation error ({deltaElev:F2}mm) exceeds tolerance ({_tolerances.ElevationMm:F2}mm).");
   }
   ```
   **Flaw**: If `actual.TopElevationMm` is `null`, this check is silently bypassed! `deltaElev` remains `0.0`, no discrepancy is added, and `Status` evaluates to `GeometricVerificationStatus.Verified`.
3. `RevitGeometryQueryScriptTemplate.cs` (lines 60-65):
   For pile family instances with `LocationPoint lp`, the script populates `record["centroid"]` and `record["rotationDegrees"]`, but does **not** populate `record["topElevationMm"]`.

#### Remediation Solution
1. Update `HasRequiredPileMeasurements` in `MeasuredGeometricEngine.Structural.cs`:
   ```csharp
   private static bool HasRequiredPileMeasurements(ActualElementGeometry actual, out string missingReason)
   {
       if (!actual.Centroid.HasValue)
       {
           missingReason = "Rule 0: Pile thiếu thuộc tính Centroid từ Revit API.";
           return false;
       }

       if (!actual.TopElevationMm.HasValue)
       {
           missingReason = "Rule 0: Pile thiếu thuộc tính Cutoff Elevation (TopElevationMm) từ Revit API.";
           return false;
       }

       missingReason = string.Empty;
       return true;
   }
   ```
2. When `actual.TopElevationMm` is missing, `HasRequiredMeasurements` returns `false`, causing `AuditItem` to invoke `CreateInconclusiveAudit(item, actual, missingReason)` returning status `Inconclusive`.
3. In `RevitGeometryQueryScriptTemplate.cs`, extract pile cutoff elevation from bounding box `Max.Z` or insertion point `lp.Point.Z`:
   ```csharp
   else if (elem is FamilyInstance fi && (category == "Pile" || category == "Footing"))
   {
       var bb = elem.get_BoundingBox(null);
       if (bb != null) record["topElevationMm"] = Mm(bb.Max.Z);
       else if (elem.Location is LocationPoint lpPile) record["topElevationMm"] = Mm(lpPile.Point.Z);
   }
   ```

---

### 1.4 Verification of Profile/Slab Elements (Outline & Thickness)

#### Root Cause Analysis
In `MeasuredGeometricEngine.ProfileGrid.cs` (lines 11-52), `AuditProfileStructure`:
```csharp
private ElementGeometricAudit AuditProfileStructure(ElementPlanItem item, ActualElementGeometry actual)
{
    var discrepancies = new List<string>();
    double deltaElev = 0.0;
    double deltaThick = 0.0;

    if (item.Geometry is ProfileGeometrySpec profSpec)
    {
        if (actual.TopElevationMm.HasValue)
        {
            deltaElev = Math.Abs(actual.TopElevationMm.Value - profSpec.ElevationMm);
            if (deltaElev > _tolerances.ElevationMm)
                discrepancies.Add($"Profile top elevation error ({deltaElev:F2}mm) exceeds tolerance ({_tolerances.ElevationMm:F2}mm).");
        }

        if (profSpec.ThicknessMm > 0 && actual.ThicknessMm.HasValue)
        {
            deltaThick = Math.Abs(actual.ThicknessMm.Value - profSpec.ThicknessMm);
            if (deltaThick > _tolerances.SectionDimensionMm)
                discrepancies.Add($"Profile thickness error ({deltaThick:F2}mm) exceeds tolerance ({_tolerances.SectionDimensionMm:F2}mm).");
        }
    }
    // ...
}
```
**Defects Identified**:
1. **Zero Outline Verification**: `profSpec.OuterLoop` defines the 2D polygon vertices of the slab or footing. However, `AuditProfileStructure` does not check any 2D position, centroid, perimeter, or boundary extents against the actual Revit floor geometry.
2. **Missing Plan Extents in Actual DTO**: `ActualElementGeometry` has no outline vertices or bounding extents for floors.
3. **Revit Query Script Incompleteness**: `RevitGeometryQueryScriptTemplate.cs` computes `topElevationMm` and `thicknessMm` for `Floor`, but does not export plan centroid (`(bb.Min + bb.Max)/2`) or bounding box extents (`widthMm = Mm(bb.Max.X - bb.Min.X)`, `heightMm = Mm(bb.Max.Y - bb.Min.Y)`).
4. **Thickness Check Bypass**: In `HasRequiredProfileMeasurements` (lines 143-163), if `prof.ThicknessMm <= 0`, thickness is not required, and outline is never required.

#### Remediation Solution
1. In `RevitGeometryQueryScriptTemplate.cs`:
   For `Floor floorObj`: compute plan centroid and bounding box dimensions:
   ```csharp
   if (bb != null)
   {
       record["centroid"] = new { x = Mm((bb.Min.X + bb.Max.X) / 2.0), y = Mm((bb.Min.Y + bb.Max.Y) / 2.0), z = Mm(bb.Max.Z) };
       record["widthMm"] = Mm(bb.Max.X - bb.Min.X);
       record["heightMm"] = Mm(bb.Max.Y - bb.Min.Y);
   }
   ```
2. In `MeasuredGeometricEngine.ProfileGrid.cs`:
   Compute expected bounding box / centroid from `profSpec.OuterLoop`:
   - Compare `actual.Centroid` against `profSpec` polygon centroid within `_tolerances.PlanPositionMm`.
   - Compare `actual.WidthMm` and `actual.HeightMm` against `profSpec` bounding extents within `_tolerances.SectionDimensionMm`.
3. In `HasRequiredProfileMeasurements`:
   Require `actual.TopElevationMm`, `actual.Centroid` (or outline extents), and `actual.ThicknessMm` (when `profSpec.ThicknessMm > 0`).

---

### 1.5 Category-Level Verification & Generic Auto-Verify Flaw

#### Root Cause Analysis
In `MeasuredGeometricEngine.cs`:
- Line 82:
  ```csharp
  _ => AuditGeneric(item, actual)
  ```
- Lines 86-96:
  ```csharp
  private static ElementGeometricAudit AuditGeneric(ElementPlanItem item, ActualElementGeometry actual) => new()
  {
      Planned = item,
      Actual = actual,
      SourceKey = item.SourceKey,
      Category = item.Category,
      Mark = item.Mark,
      Status = GeometricVerificationStatus.Verified, // <--- DIRECT VIOLATION OF RULE 0
      TargetUniqueId = actual.UniqueId,
      TargetElementId = actual.ElementId
  };
  ```
- Lines 179-187:
  ```csharp
  _ => NoMissingMeasurements(out missingReason)
  ```
  Returns `true` unconditionally.

**Impact**:
Any category outside Column, Beam, GradeBeam, Wall, Pile, Footing, Slab, Level, and Grid (e.g. `Generic`, `Opening`, `Room`, `MepFixture`, `PipeRun`, `Rebar`) bypasses all empirical measurements and is stamped `Verified = 100%`. This directly violates Rule 0: "Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng" and "Mọi đánh giá QA/QC phải dựa trên dữ liệu đo đạc thực nghiệm. Nếu thiếu nguồn dữ liệu đối chiếu hoặc thiếu thông số đo, phải kết luận Inconclusive, cấm tự động nghiệm thu."

#### Remediation Solution
1. In `AuditGeneric`:
   Replace `Status = GeometricVerificationStatus.Verified` with `Status = GeometricVerificationStatus.Inconclusive`:
   ```csharp
   private static ElementGeometricAudit AuditGeneric(ElementPlanItem item, ActualElementGeometry actual) => new()
   {
       Planned = item,
       Actual = actual,
       SourceKey = item.SourceKey,
       Category = item.Category,
       Mark = item.Mark,
       Status = GeometricVerificationStatus.Inconclusive,
       TargetUniqueId = actual.UniqueId,
       TargetElementId = actual.ElementId,
       DefectReason = $"Rule 0: Category '{item.Category}' lacks empirical measurement rules; cannot auto-verify without actual physical measurements.",
       Discrepancies = new[] { $"Unmeasured category [{item.Category}] marked Inconclusive under Rule 0." }
   };
   ```
2. In `HasRequiredMeasurements`:
   For unhandled categories:
   ```csharp
   _ => HasRequiredGenericMeasurements(category, actual, out missingReason)
   ```
   Where `HasRequiredGenericMeasurements` returns `false` with `missingReason = $"Rule 0: Category '{category}' has no standardized measurement rules implemented.";`.

---

## 2. Test Suites and Repository Governance

### 2.1 HPBim Test Projects Inventory

`HPBim.slnx` contains three test projects:

| Project | Target Framework | Runner / Framework | Test Count | Status | Notes |
|---|---|---|---|---|---|
| `tests/HPBim.Core.Tests` | `net10.0` | `Microsoft.Testing.Platform` + `xunit.v3` (3.1.0) | **465** | 100% PASS | Pure logic tests, domain models, QC rules, geometry math, tolerances |
| `tests/HPBim.Client.Tests` | `net10.0` | `Microsoft.Testing.Platform` + `xunit.v3` (3.1.0) | **458** | 100% PASS | Pipeline engine, intent journal, Named Pipe transport, CLI commands |
| `tests/HPBim.E2E.Tests` | `net10.0` | `Microsoft.Testing.Platform` + `xunit.v3` (3.1.0) | **42** | 100% PASS | 4-tier combinatorial workloads using production classes & pipe fakes |
| **Total HPBim Tests** | | | **965** | **100% PASS** | Execution time: ~5.3 seconds |

### 2.2 Coverage of Wave 3 Requirements (R1..R5)

| Requirement | Current Test Coverage in HPBim | Missing Coverage Needed |
|---|---|---|
| **R1. Multi-Writer Journal Concurrency & File Lock** | `IntentJournalTests`, `IntentJournalAdversarialTests`, `IntentJournalDeepStressAdversarialTests`, `IntentJournalTier5AdversarialTests` | Test 2 concurrent instances writing 1,000 unique intents simultaneously, followed by cold-restart verification of 1,000/1,000 intact records. |
| **R2. Reconcile Persist-Before-Terminal & Keyset Coverage** | `AutoReconcileIdempotencyTests`, `AdversarialReconcileIdempotencyTests`, `ExtensibleStorageReceiptReaderTests` | Test simulating ledger disk write failure during reconcile; verify journal remains `OutcomeUnknown` and is NOT marked `Committed`. |
| **R3. Persistent Document Identity vs Dynamic DocumentVersion GUID** | `ExtensibleStorageReceiptReaderTests`, `PreFlightGateTests` | Test verifying that dynamic change in `VersionGuid` after apply does not reject valid receipt for that operation. |
| **R4. Strict NoOp UniqueId, Exact Skip Policy, Rollback Truthfulness** | `BimPipelineEngineRecordReconciliationTests`, `CardinalityAndTruncationTests`, `DryRunRollbackHonestyTests`, `CompositeSourceKeyJsonConverterTests` | Test verifying NoOp record without `TargetUniqueId` is rejected; test verifying Skipped record with non-allowlisted reason causes batch rollback; test verifying ProvenanceHandles roundtrip JSON preservation. |
| **R5. Standardized Geometric Measurement & Verification** | `MeasuredGeometricEngineTests`, `MeasuredGeometricEngineRequiredMeasurementsTests`, `MeasuredGeometricToleranceTests`, `MeasuredGeometricVerifierAdversarialCoverageTests`, `MeasuredGeometricVerifierTests` | Test verifying Pile missing cutoff elevation returns `Inconclusive`; test verifying Footing/Slab outline & thickness verification; test verifying Generic category returns `Inconclusive`. |

---

### 2.3 Repository-Wide Test Counts & Build Commands

Build & Test commands:
```powershell
# Build entire HPBim solution
dotnet build HPBim/HPBim.slnx -c Release

# Run HPBim tests (965 tests)
dotnet test HPBim/HPBim.slnx

# Run McpShared tests (908 tests)
dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests
dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests

# Run HPRebar tests (1,067 tests)
dotnet test HPRebar/HPRebar.Core.Tests
dotnet test HPRebar/HPRebar.Mcp.Server.Tests
```

Total verified test counts across repository:
- `HPBim`: 965 tests
- `HPRebar`: 1,067 tests
- `McpShared`: 908 tests
- Other MCP hosts (`HPAutoCad`, `HPNavis`, `HPEtabs`, `HPCivil3d`, `HPSap2000`, `HPPowerBi`, `HPExcel`, `HPRobot`, `HPTekla`): ~839+ tests
- **Grand Total across repository:** **3,779+ tests** (zero regressions).

---

### 2.4 Clean Code Governance Audit

| Clean Code Rule | Scope | Audit Finding | Compliance |
|---|---|---|---|
| **C2: File lines <= 300** | `HPBim.Core`, `HPBim.Client` | 100% compliant. Maximum file size in Core: 248 lines (`MeasuredGeometricEngine.Structural.cs`). Maximum in Client: 251 lines (`BimPipelineEngine.PlanExecution.cs`). | **PASS** |
| | `HPBim.Cli/Commands` | 2 legacy CAD extraction files exceed 300 lines: `Story1CadBeamDetector.cs` (303 lines), `Story1SpanAuditService.cs` (573 lines). (Non-production command scratch tools). | Note / Baseline |
| **M3: Method lines <= 50** | `HPBim.Core/Verification`, `HPBim.Client/Verification` | 100% compliant across all verification engines (`AuditColumn` [27 lines], `AuditLinearStructure` [32 lines], `AuditPile` [38 lines], `AuditWall` [35 lines], `AuditProfileStructure` [42 lines]). Only multi-line script templates in `RevitGeometryQueryScriptTemplate.cs` have longer concatenation strings. | **PASS** |
| **Q-B1: Zero commented-out code** | `HPBim.Core`, `HPBim.Client`, `HPBim.Cli` | 0 commented-out code lines found across all projects. | **PASS** |
| **Q-B2: Zero unannotated empty catch** | `HPBim.Core`, `HPBim.Client` | 0 unannotated empty catch blocks. All catches log or have detailed rationale comments. | **PASS** |
| | `HPBim.Cli/Commands` | 4 legacy exploratory command files contain unannotated `catch { }`: `ExportDaKiengDwgCommand.cs:122`, `InspectBeamParametersCommand.cs`, `InspectCadStory2FramingCommand.cs`, `LinkDaKiengCadCommand.cs`. | Note / To Remediate |
| **netstandard2.0 purity** | `HPBim.Core` | 100% pure `netstandard2.0`. Zero references to `Autodesk.Revit.*` or `Autodesk.*`. Compiles cleanly without Revit SDK. | **PASS** |

---

## 3. Conclusions and Remediation Roadmap

To fully remediate R5 in accordance with the Wave 3 requirements:
1. **TargetUniqueId Lookup**:
   - Update `RevitGeometryQueryScriptTemplate.cs` to query elements by `doc.GetElement(targetUniqueId)` when present.
   - Update `MeasuredGeometricEngine.cs`'s `FindActual` to prioritize `item.TargetUniqueId`.
   - Update `VerifyCommand.cs` to hydrate `TargetUniqueId` from `BimCoordinationLedgerService`.
2. **Pile Cutoff Elevation**:
   - Update `HasRequiredPileMeasurements` in `MeasuredGeometricEngine.Structural.cs` to enforce `actual.TopElevationMm.HasValue`.
   - Extract pile cutoff elevation in `RevitGeometryQueryScriptTemplate.cs`.
3. **Profile / Slab Outline Verification**:
   - Extract bounding extents and centroid for floors in `RevitGeometryQueryScriptTemplate.cs`.
   - Check actual centroid and bounding box dimensions against expected `OuterLoop` extents in `AuditProfileStructure`.
   - Require outline and thickness measurements in `HasRequiredProfileMeasurements`.
4. **Eliminate Generic Auto-Verify**:
   - Modify `AuditGeneric` in `MeasuredGeometricEngine.cs` to return `Status = GeometricVerificationStatus.Inconclusive` under Rule 0.
   - Modify `HasRequiredMeasurements` to fail pre-check for unmeasured categories.
5. **New Unit Tests**:
   - Add unit tests validating that piles without cutoff elevation return `Inconclusive`.
   - Add unit tests validating slab outline deviations are caught as `Defect`.
   - Add unit tests validating Generic category returns `Inconclusive`.
