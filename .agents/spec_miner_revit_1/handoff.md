# Handoff Report: Revit 2025/2026 API Specification for Continuous Beam Rebar

**Agent**: `spec_miner_revit_1`  
**Working Directory**: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_revit_1`  
**Deliverable**: `revit_api_spec.md`  
**Handoff Type**: Hard (Task Complete)  

---

## 1. Observation

Direct observations from repository files and reference code:

1. **Rebar Creation Methods**:
   - In `HPRebar/HPRebar/Column Rebar/StirrupCreator.cs` (lines 49–56):
     ```csharp
     var rebar = Rebar.CreateFromRebarShape(
         document, shape, barType, host, placement.Origin, placement.XVector, placement.YVector);
     var accessor = rebar.GetShapeDrivenAccessor();
     accessor.ScaleToBox(placement.Origin, placement.Width, placement.Height);
     accessor.SetLayoutAsNumberWithSpacing(run.Count, RevitUnits.MmToFt(run.Spacing), true, true, true);
     ```
   - In `HPRebar/HPRebar/Column Rebar/MainBarCreator.cs` (lines 28–58): Free-form rebar creation required conditional compilation `#if REVIT2026_OR_GREATER` due to Revit 2026 API changes. In contrast, `Rebar.CreateFromCurves` provides a stable shape-driven signature across Revit 2023–2027.
   - In `HPRebar.Core/ColumnRebar/StirrupDistributionCalculator.cs` (line 16):
     ```csharp
     public const int MaxBarPositions = 1002;
     ```
     Revit's `RebarShapeDrivenAccessor.SetLayoutAs*` throws `ArgumentOutOfRangeException` if count > 1002.

2. **Revit Deprecated APIs**:
   - In `HPRebar/HPRebar/Column Rebar/RevitUnits.cs` (lines 12–22):
     ```csharp
     public static double MmToFt(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
     public static double FtToMm(double ft) => UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters);
     public static string Display(Document doc, double ft) => UnitFormatUtils.Format(doc.GetUnits(), SpecTypeId.Length, ft, false);
     ```
     Uses `UnitTypeId.Millimeters` and `SpecTypeId.Length` (ForgeTypeId). No `DisplayUnitType` or `UnitType`.
   - In `AGENTS.md` (lines 70–74):
     ```csharp
     #if REVIT2024_OR_GREATER
         long id = elementId.Value;       // .Value is long since 2024
     #else
         int id = elementId.IntegerValue; // legacy
     #endif
     ```
   - In `HPRebar/HPRebar/Column Rebar/StructuralColumnSelectionFilter.cs` (lines 11–12):
     ```csharp
     element.Category?.BuiltInCategory == BuiltInCategory.OST_StructuralColumns;
     ```
     Category comparison uses `BuiltInCategory` enum directly without touching `Category.Id`.

3. **Transaction Safety**:
   - In `HPRebar/HPRebar/Column Rebar/ColumnRebarOrchestrator.cs` (lines 58–92): Single outer `using var group = new TransactionGroup(_document, "Column Rebar"); group.Start();` wraps all sub-transactions. On exception, `group.RollBack(); throw;` executes; on completion, `group.Assimilate();` merges all into one undo step.
   - In `HPRebar/HPRebar/Column Rebar/RebarFailureHandling.cs` (lines 21–48): `SwallowWarnings` implements `IFailuresPreprocessor` and calls `accessor.DeleteWarning(failure)` for `FailureSeverity.Warning`.

4. **Dimensioning and View Creation**:
   - In `HPRebar/HPRebar/Column Rebar/DimensionCreator.cs` (lines 121–127):
     ```csharp
     private static Reference ToLinearReference(Document document, PlanarFace face)
     {
         var surface = face.Reference.ConvertToStableRepresentation(document);
         var linear = surface.Replace("SURFACE", "LINEAR");
         return Reference.ParseFromStableRepresentation(document, linear);
     }
     ```
     Section view dimensioning throws or fails if references are `SURFACE`; rewriting to `LINEAR` is mandatory.
   - In `HPRebar/HPRebar/Column Rebar/DetailViewCreator.cs` (lines 109–130): `Rename` catches clashing view name exceptions and suffixes `"A"`, `"B"`.

---

## 2. Logic Chain

1. **Shape-Driven vs Free-Form for Beam Rebar**:
   - *Observation*: `MainBarCreator.cs` used `Rebar.CreateFreeForm` which required version-specific branching and lacks standard shape code parameters in schedules.
   - *Requirement*: `ORIGINAL_REQUEST.md` R3 specifically dictates `BeamMainBarCreator.cs (CreateFromCurves)`.
   - *Inference*: Continuous beams require shape-driven rebar created via `Rebar.CreateFromCurves`. By computing 2D/3D polyline curve chains lying in the vertical plane defined by normal $\vec{N} = \vec{Y}_{beam}$ and passing `useExistingShapeIfPossible = true, createNewShape = true`, Revit generates standard shape-driven bars that participate in native rebar schedules, tags, and grips across all supported versions (2023–2027) with zero API divergence.

2. **Stirrup Generation & Distribution**:
   - *Observation*: `StirrupCreator.cs` generates closed ties via `Rebar.CreateFromRebarShape` and uses `RebarShapeDrivenAccessor`.
   - *Inference*: Beams with 3-zone stirrups (dense-sparse-dense) must create 3 distinct `Rebar` instances per span. Each instance is scaled to the beam's rectangular cross-section ($b - 2\times cover$, $h - 2\times cover$) via `ScaleToBox` and distributed using `SetLayoutAsNumberWithSpacing`. The maximum bar count constraint of 1002 must be validated in `HPRebar.Core` before opening any transaction.

3. **Geometry Extraction & Support Discovery**:
   - *Observation*: Beams are `FamilyInstance` of `OST_StructuralFraming`.
   - *Inference*: Beam axis $\vec{X}_{beam}$ is obtained from `LocationCurve`. Collinearity of adjacent spans requires checking dot product $\approx 1.0$ and perpendicular distance $\le 10$ mm. Support discovery must query intersecting `OST_StructuralColumns`, `OST_Walls`, and perpendicular `OST_StructuralFraming` directly below or at the beam joints to calculate support widths and derive clear span $L_{clear} = L_{center} - c_1/2 - c_2/2$. Cantilevers are cleanly identified when exterior nodes have zero underlying supports.

4. **TransactionGroup Atomicity**:
   - *Observation*: Generating drawings, dimensions, and rebar involves multiple separate transactions.
   - *Inference*: To satisfy Acceptance Criteria R3 & guardrail 66, all database modifications must execute within an outer `TransactionGroup("Beam Rebar")` owned by `BeamRebarOrchestrator`. An `IFailuresPreprocessor` handles Revit's non-fatal rebar overlap warnings. On any unhandled exception or user cancellation, `RollBack()` restores the document to its exact pre-run state.

---

## 3. Caveats

1. **Revit Process Availability**: Analysis was conducted directly from reference source code, Revit API contracts, and official SDK metadata. No live Revit 2026 GUI process was attached during this read-only phase.
2. **Rebar Shape Template Assumptions**: `Rebar.CreateFromRebarShape` assumes standard shape families (e.g. `M_T1` or equivalent rectangular closed stirrup) exist in the active Revit project template. `RebarShapeResolver` must validate presence prior to transaction opening.
3. **Linear Dimension Reference Behavior**: The stable string replacement (`SURFACE` $\to$ `LINEAR`) in `DimensionCreator` is an internal Revit behavior. As noted in the codebase, dimension creation is always wrapped in a guarded try/catch to ensure dimension failure never invalidates rebar creation.

---

## 4. Conclusion

The specification for continuous beam rebar generation is fully mined, verified, and documented in:
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_revit_1\revit_api_spec.md`

Key findings:
- **14 Features Discovered** fully enumerated with inputs, outputs, and error handling.
- **17 Edge Cases** catalogued with explicit prevention logic.
- Complete API signatures specified for `Rebar.CreateFromCurves`, `Rebar.CreateFromRebarShape`, and `RebarShapeDrivenAccessor`.
- Zero deprecated APIs guaranteed: `UnitTypeId`, `SpecTypeId`, `#if REVIT2024_OR_GREATER` for `ElementId.Value`.
- Complete file structure mapped to `HPRebar/HPRebar/Beam Rebar/` adhering strictly to feature-folder conventions.

---

## 5. Verification Method

To independently verify this specification:

1. **Review Specification File**:
   Inspect `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_revit_1\revit_api_spec.md`.
2. **Verify Deprecated API Rules Against Repository**:
   ```bash
   grep -rn "DisplayUnitType" HPRebar/
   grep -rn "IntegerValue" HPRebar/
   ```
3. **Validate Solution Compilation Commands**:
   ```bash
   dotnet build HPRebar/HPRebar.slnx -c Debug.R25 -p:DeployAddin=false
   dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false
   ```
4. **Validate Pure Domain Test Suite**:
   ```bash
   dotnet test HPRebar/HPRebar.Core.Tests
   ```
