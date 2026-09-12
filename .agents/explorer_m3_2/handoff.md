# Handoff Report: Beam Rebar Creators & Shape Generation (Milestone M3 Part 2)

**Document**: `handoff.md`  
**Agent**: `explorer_m3_2`  
**Date**: 2026-09-07  
**Working Directory**: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m3_2`  
**Recipient**: Orchestrator (`parent`, conversation id: `e303874c-1ef4-4fd0-9596-71bbccff874a`)  
**Type**: Hard Handoff (Task Complete)  

---

## 1. Observation

1. **Revit API Rebar Instantiation Requirements**:
   - In `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_revit_1\revit_api_spec.md` (lines 72–86), `Rebar.CreateFromCurves` requires:
     ```csharp
     public static Rebar CreateFromCurves(
         Document doc, RebarStyle style, RebarBarType barType,
         RebarHookType startHook, RebarHookType endHook, Element host,
         XYZ norm, IList<Curve> curves,
         RebarHookOrientation startHookOrient, RebarHookOrientation endHookOrient,
         bool useExistingShapeIfPossible, bool createNewShape)
     ```
     With constraint (lines 96–103): `norm` must be perpendicular to all curve segments and unit length. For vertical bends along the beam axis, $\vec{N} = \vec{Y}_{beam} = \vec{Z} \times \vec{X}_{beam}$. For transverse cross-ties, $\vec{N} = \vec{X}_{beam}$.
   - In `spec_miner_revit_1\revit_api_spec.md` (line 186): `Rebar.CreateFreeForm` is deprecated in Revit 2026 and removed in Revit 2027; shape-driven `Rebar.CreateFromCurves` must be used.
   - For stirrups, `Rebar.CreateFromRebarShape` (lines 111–124) requires orthogonal unit vectors `xVec` and `yVec`, followed by `ScaleToBox` and `SetLayoutAsNumberWithSpacing` (max count $\le 1002$).

2. **Column Rebar Golden Reference Patterns**:
   - In `HPRebar/HPRebar/Column Rebar/RebarCreationService.cs` (lines 76–135), creation is staged in distinct inner transactions (`"Create Stirrup Bars"`, `"Create Main Bars"`) with `RebarFailureHandling.Apply(transaction)` suppressing non-fatal warnings (e.g. rebar outside host).
   - In `HPRebar/HPRebar/Column Rebar/StirrupCreator.cs` (lines 49–57), `Rebar.CreateFromRebarShape` is scaled via `ScaleToBox` and arrayed via `SetLayoutAsNumberWithSpacing(run.Count, RevitUnits.MmToFt(run.Spacing), true, true, true)`.
   - In `HPRebar/HPRebar/Column Rebar/PointMapper.cs` (lines 45–50), local coordinates in mm are converted to world coordinates in feet:
     ```csharp
     public XYZ ToXyz(Point3 point) =>
         _origin
         + RevitUnits.MmToFt(point.X) * _east
         + RevitUnits.MmToFt(point.Y) * _north
         + RevitUnits.MmToFt(point.Z) * XYZ.BasisZ;
     ```

3. **Core Domain Calculators & Models**:
   - In `HPRebar/HPRebar.Core/BeamRebar/Models/BarPolyline.cs` (lines 30–35), `BarPolyline` encapsulates the complete 3D centerline geometry via `Polyline3 Polyline` in millimetres.
   - In `HPRebar/HPRebar.Core/BeamRebar/Calculators/BeamMainBarCalculator.cs` (lines 50–183 & 187–394), top and bottom continuous bars already have their 90° downward/upward exterior anchorage hooks embedded in the 3D polyline vertices, as well as 50% staggered lap splices and step transitions.
   - In `HPRebar/HPRebar.Core/BeamRebar/Calculators/BeamAdditionalBarCalculator.cs` (lines 22–344 & 360–420), top support bars (extending $L/3$ in Layer 1 and $L/4$ in Layer 2) and bottom midspan bars (starting $L/7$ from support faces) are computed with vertical offsets ($\Delta Z = 50$ mm).
   - In `HPRebar/HPRebar.Core/BeamRebar/Calculators/BeamSideBarCalculator.cs` (lines 51–123 & 128–199), longitudinal skin bars ($h \ge 700$ mm, spacing $\le 300$ mm) and transverse cross-ties are computed.
   - In `HPRebar/HPRebar.Core/BeamRebar/Calculators/BeamSpecialBarCalculator.cs` (lines 144–198 & 206–261), concentrated hanging stirrups and 45° diagonal bent ties flanking secondary beam joints are computed.

4. **Directory State**:
   - `HPRebar/HPRebar/Beam Rebar/` does not yet exist. Milestone M3 is currently in progress.

---

## 2. Logic Chain

1. From Observation 1 & 3, `HPRebar.Core/BeamRebar` provides complete 3D polylines for all longitudinal bars (main, additional, side, and diagonal bent bars) with hooks, splices, and layers fully resolved in local coordinates $(X, Y, Z)$ in millimetres.
2. From Observation 1, Revit's `Rebar.CreateFromCurves` accepts planar curve segments and a unit normal vector. For all longitudinal bars in the vertical plane, the plane is spanned by $\vec{X}_{beam}$ and $\vec{Z} = (0,0,1)$, meaning the unit normal vector is identically $\vec{Y}_{beam} = \vec{Z} \times \vec{X}_{beam}$. For transverse cross-ties and stirrups, the plane is the transverse Y-Z plane, so the unit normal vector is $\vec{X}_{beam}$.
3. Because 90° hooks and cutoffs are explicitly calculated into the polyline vertices by `BeamMainBarCalculator` and `BeamAdditionalBarCalculator`, passing `startHook: null, endHook: null` to `Rebar.CreateFromCurves` ensures exact geometry without relying on document-dependent hook definitions, while allowing Revit to automatically recognize standard shapes (e.g. `M_02`, `M_04`) or synthesize a parametric shape.
4. From Observation 1 & 2, perimeter stirrups must be instantiated via `Rebar.CreateFromRebarShape` with `ScaleToBox` and `SetLayoutAsNumberWithSpacing` using vectors $\vec{xVec} = \vec{Y}_{beam}$ and $\vec{yVec} = \vec{Z}$. This guarantees parametric stirrup behavior and eliminates manual curve loop construction for standard closed rectangular ties.
5. From Observation 2, orchestrating creation across 5 separate transactions (`"Create Stirrups"`, `"Create Main Bars"`, `"Create Additional Bars"`, `"Create Side Bars"`, `"Create Special Bars"`) with `RebarFailureHandling.Apply(transaction)` protects against Revit modal warning popups and ensures clean transaction rollback boundaries under the outer `TransactionGroup("Beam Rebar")`.
6. Therefore, the implementation plan in `creators_plan.md` decomposes the rebar creation subsystem into 7 primary components (`RebarCreationService`, 5 Creator classes, and `RebarShapeResolver` / `RebarTypeCatalog`) and supporting models, ensuring clean code-behind, testability, and multi-version Revit 2023–2027 compatibility.

---

## 3. Caveats

- **Runtime Revit Execution**: As noted in `AGENTS.md`, full in-process Revit execution has not been verified on this machine yet. All code designs rely on authoritative Revit SDK documentation, compiler contracts, and the golden reference of `Column Rebar`.
- **Secondary Beam Intersection Data**: `BeamSpecialBarCreator` depends on `SecondaryBeamIntersection` records populated by `BeamStackReader` / `BeamSupportFinder`. If a project has no secondary beams framing into the continuous beam, special bar generation is cleanly skipped.
- **RebarShape Names in Custom Templates**: While `RebarShapeResolver` includes fallback lists (`["M_T1", "T1", "01", "M_01"]` for stirrups and `["M_T10", "T10"]` for cross-ties), templates with completely non-standard shape names will trigger pre-flight validation failure in `CanCreate()`.

---

## 4. Conclusion

The architectural design and implementation specifications for Milestone M3 Part 2 (Beam Rebar Creators & Shape Generation) are complete. The detailed design document has been written to:
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m3_2\creators_plan.md`

The plan provides:
1. Complete C# designs and method signatures for `RebarCreationService.cs`, `BeamStirrupCreator.cs`, `BeamMainBarCreator.cs`, `BeamAdditionalBarCreator.cs`, `BeamSideBarCreator.cs`, `BeamSpecialBarCreator.cs`, `RebarShapeResolver.cs`, and `RebarTypeCatalog.cs`.
2. Exact vector mathematics for local-to-world transformations (`PointMapper`), normal vectors ($\vec{Y}_{beam}$ for longitudinal bars, $\vec{X}_{beam}$ for stirrups/cross-ties), and box scaling.
3. Strict adherence to Revit 2025/2026 API standards, deprecation avoidance (zero `DisplayUnitType`, zero `CreateFreeForm`), and repository feature-folder rules.

---

## 5. Verification Method

1. **Inspect Plan File**:
   - View `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m3_2\creators_plan.md` to confirm all 7 components and supporting models are fully specified.
2. **Domain Unit Tests Verification**:
   - Run:
     ```bash
     dotnet test HPRebar/HPRebar.Core.Tests
     ```
     Verifies that all 102+ underlying mathematical calculators (stirrups, main bars, additional bars, side bars, special bars) pass with 100% success.
3. **Multi-Version Compilation Check (upon implementation)**:
   - Run:
     ```bash
     dotnet build HPRebar/HPRebar.slnx -c Debug.R25 -p:DeployAddin=false
     dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false
     ```
     Must compile with 0 errors and 0 warnings.
4. **Deprecation Invalidation Test**:
   - Search for forbidden patterns:
     ```bash
     grep -rn "CreateFreeForm" HPRebar/HPRebar/Beam\ Rebar/
     grep -rn "DisplayUnitType" HPRebar/HPRebar/Beam\ Rebar/
     ```
     Must return 0 matches.
